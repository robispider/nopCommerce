using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nop.Core.Caching;
using Nop.Data;
using Nop.Plugin.Marketplace.Commission.Domains;
using Nop.Plugin.Marketplace.Commission.Domains.Enums;
using Nop.Plugin.Marketplace.Commission.Settings; // Inject settings
using Nop.Plugin.Marketplace.Order.Domains;
using Nop.Plugin.Marketplace.Dropship.Domains;
using Nop.Plugin.Marketplace.Core.Domains;
using Nop.Services.Catalog;
using Nop.Services.Orders;

namespace Nop.Plugin.Marketplace.Commission.Services
{
    public class CommissionEvaluatorService : ICommissionEvaluatorService
    {
        private readonly IRepository<CommissionRule> _ruleRepository;
        private readonly IRepository<CommissionSplit> _splitRepository;
        private readonly IRepository<MarketplaceOrderAllocation> _allocationRepository;
        private readonly IRepository<DropshipFulfillment> _dropshipRepository;

        private readonly IOrderService _orderService;
        private readonly IProductService _productService;
        private readonly ICategoryService _categoryService;
        private readonly IStaticCacheManager _staticCacheManager;
        private readonly ILocker _locker; // Add distributed locking
        private readonly MarketplaceCommissionSettings _commissionSettings; // Add dynamic settings

        public CommissionEvaluatorService(
            IRepository<CommissionRule> ruleRepository,
            IRepository<CommissionSplit> _splitRepository,
            IRepository<MarketplaceOrderAllocation> _allocationRepository,
            IRepository<DropshipFulfillment> _dropshipRepository,
            IOrderService _orderService,
            IProductService _productService,
            ICategoryService _categoryService,
            IStaticCacheManager _staticCacheManager,
            ILocker locker,
            MarketplaceCommissionSettings commissionSettings)
        {
            this._ruleRepository = ruleRepository;
            this._splitRepository = _splitRepository;
            this._allocationRepository = _allocationRepository;
            this._dropshipRepository = _dropshipRepository;
            this._orderService = _orderService;
            this._productService = _productService;
            this._categoryService = _categoryService;
            this._staticCacheManager = _staticCacheManager;
            this._locker = locker;
            this._commissionSettings = commissionSettings;
        }

        public async Task<CommissionSplitResult> CalculateSplitsAsync(int nativeOrderId)
        {
            var existingSplits = await _splitRepository.GetAllAsync(q => q.Where(s => s.NativeOrderId == nativeOrderId));
            if (existingSplits.Any())
            {
                return MapToResult(nativeOrderId, existingSplits.ToList());
            }

            CommissionSplitResult finalResult = null;
            string lockKey = $"marketplace_commission_lock_{nativeOrderId}";

            // ALIBABA-GRADE: Use ILocker to prevent concurrent calculation attempts
            await _locker.PerformActionWithLockAsync(lockKey, TimeSpan.FromSeconds(15), async () =>
            {
                // Re-verify inside lock to prevent dirty reads
                existingSplits = await _splitRepository.GetAllAsync(q => q.Where(s => s.NativeOrderId == nativeOrderId));
                if (existingSplits.Any())
                {
                    finalResult = MapToResult(nativeOrderId, existingSplits.ToList());
                    return;
                }

                var nativeOrder = await _orderService.GetOrderByIdAsync(nativeOrderId);
                if (nativeOrder == null)
                    throw new Exception("Order not found");

                var allocations = await _allocationRepository.GetAllAsync(q => q.Where(a => a.MarketplaceOrderGroupId == nativeOrderId));
                if (!allocations.Any())
                {
                    finalResult = new CommissionSplitResult();
                    return;
                }

                var orderItems = await _orderService.GetOrderItemsAsync(nativeOrderId);
                var productIds = orderItems.Select(oi => oi.ProductId).Distinct().ToArray();

                var products = await _productService.GetProductsByIdsAsync(productIds);
                var productDict = products.ToDictionary(p => p.Id);

                var productCategoryDict = new Dictionary<int, List<int>>();
                foreach (var p in products)
                {
                    var categories = await _categoryService.GetProductCategoriesByProductIdAsync(p.Id);
                    productCategoryDict[p.Id] = categories.Select(c => c.CategoryId).ToList();
                }

                var dropshipTickets = await _dropshipRepository.GetAllAsync(q => q.Where(d => d.OrderId == nativeOrderId));
                var dropshipDict = dropshipTickets.ToDictionary(d => d.OrderItemId);

                var activeRules = await GetActiveRulesCachedAsync();

                // ALIBABA-GRADE: Calculate dynamic gateway fee using injected settings
                decimal gatewayPercentage = _commissionSettings.GatewayFeePercentage / 100m;
                decimal totalGatewayFee = Math.Round((nativeOrder.OrderTotal * gatewayPercentage) + _commissionSettings.GatewayFeeFixed, 2);

                var createdSplits = new List<CommissionSplit>();

                foreach (var allocation in allocations)
                {
                    var orderItem = orderItems.FirstOrDefault(oi => oi.Id == allocation.OrderItemId);
                    if (orderItem == null)
                        continue;

                    var product = productDict[orderItem.ProductId];
                    var productCategoryIds = productCategoryDict[product.Id];

                    dropshipDict.TryGetValue(orderItem.Id, out var dropshipTicket);

                    int sellingVendorId = dropshipTicket?.ResellerVendorId ?? allocation.VendorId;

                    // 1. Evaluate Base Rule (SKU > Vendor > Category > Default)
                    var baseRule = activeRules
                        .Where(r => !r.IsModifier && IsRuleApplicable(r, sellingVendorId, product.Id, productCategoryIds))
                        .OrderBy(r => r.PriorityId)
                        .ThenByDescending(r => r.CreatedOnUtc)
                        .FirstOrDefault();

                    if (baseRule == null)
                        throw new Exception($"FATAL: No Commission Base Rule found for Item {orderItem.Id}");

                    // 2. Evaluate Modifiers
                    var modifiers = activeRules
                        .Where(r => r.IsModifier && IsRuleApplicable(r, sellingVendorId, product.Id, productCategoryIds))
                        .ToList();

                    // 3. Platform Fee
                    decimal calculatedFee = CalculateFeeAmount(baseRule, modifiers, orderItem.PriceExclTax, orderItem.Quantity);

                    if (baseRule.MaximumFeeAmount.HasValue && calculatedFee > baseRule.MaximumFeeAmount.Value)
                        calculatedFee = baseRule.MaximumFeeAmount.Value;
                    if (baseRule.MinimumFeeAmount.HasValue && calculatedFee < baseRule.MinimumFeeAmount.Value)
                        calculatedFee = baseRule.MinimumFeeAmount.Value;

                    decimal platformFee = Math.Round(calculatedFee, 2);

                    // 4. Proportional Gateway Fee
                    decimal itemGrossAmount = orderItem.PriceInclTax;
                    decimal itemGatewayFee = 0;
                    if (nativeOrder.OrderTotal > 0)
                        itemGatewayFee = Math.Round(totalGatewayFee * (itemGrossAmount / nativeOrder.OrderTotal), 2);

                    decimal taxAmount = orderItem.PriceInclTax - orderItem.PriceExclTax;

                    int supplierId = dropshipTicket?.SupplierVendorId ?? allocation.VendorId;
                    int? resellerId = dropshipTicket?.ResellerVendorId;

                    decimal supplierRevenue = 0;
                    decimal resellerMargin = 0;

                    if (dropshipTicket != null)
                    {
                        // Dropship
                        decimal wholesaleCost = dropshipTicket.LockedWholesalePrice * orderItem.Quantity;
                        supplierRevenue = Math.Round(wholesaleCost, 2);

                        resellerMargin = orderItem.PriceExclTax - itemGatewayFee - platformFee - supplierRevenue;

                        if (resellerMargin < 0)
                            throw new Exception($"Negative Reseller Margin detected for OrderItem {orderItem.Id}. Margin: {resellerMargin}");
                    }
                    else
                    {
                        // Direct Supplier
                        supplierRevenue = orderItem.PriceExclTax - itemGatewayFee - platformFee;
                        resellerMargin = 0;

                        if (supplierRevenue < 0)
                            throw new Exception($"Negative Supplier Revenue detected for Direct OrderItem {orderItem.Id}. Revenue: {supplierRevenue}");
                    }

                    var splitRecord = new CommissionSplit
                    {
                        NativeOrderId = nativeOrderId,
                        OrderItemId = orderItem.Id,
                        AppliedBaseRuleId = baseRule.Id,
                        AppliedModifierRuleIds = string.Join(",", modifiers.Select(m => m.Id)),
                        CustomerPaidAmount = orderItem.PriceExclTax,
                        TaxAmount = taxAmount,
                        GatewayFeeAmount = itemGatewayFee,
                        PlatformFeeAmount = platformFee,
                        SupplierVendorId = supplierId,
                        SupplierWholesaleAmount = supplierRevenue,
                        ResellerVendorId = resellerId,
                        ResellerMarginAmount = resellerMargin,
                        CreatedOnUtc = DateTime.UtcNow
                    };

                    await _splitRepository.InsertAsync(splitRecord);
                    createdSplits.Add(splitRecord);
                }

                finalResult = MapToResult(nativeOrderId, createdSplits);
            });

            return finalResult;
        }

        private CommissionSplitResult MapToResult(int nativeOrderId, List<CommissionSplit> splits)
        {
            if (!splits.Any())
                return new CommissionSplitResult();

            return new CommissionSplitResult
            {
                TotalOrderAmount = splits.Sum(s => s.CustomerPaidAmount + s.TaxAmount),
                GatewayFeeAmount = splits.Sum(s => s.GatewayFeeAmount),
                NetPlatformFeeAmount = splits.Sum(s => s.PlatformFeeAmount),
                SupplierVendorId = splits.FirstOrDefault()?.SupplierVendorId ?? 0,
                SupplierAmount = splits.Sum(s => s.SupplierWholesaleAmount),
                ResellerVendorId = splits.FirstOrDefault()?.ResellerVendorId ?? 0,
                ResellerAmount = splits.Sum(s => s.ResellerMarginAmount)
            };
        }

        private decimal CalculateFeeAmount(CommissionRule baseRule, List<CommissionRule> modifiers, decimal itemTotal, int quantity)
        {
            decimal totalPercentage = baseRule.Percentage + modifiers.Sum(m => m.Percentage);
            decimal totalFixed = baseRule.FixedAmount + modifiers.Sum(m => m.FixedAmount);

            if (totalPercentage < 0)
                totalPercentage = 0;

            return baseRule.CalculationTypeId switch
            {
                (int)CommissionCalculationType.Percentage => itemTotal * (totalPercentage / 100m),
                (int)CommissionCalculationType.FixedAmount => totalFixed,
                (int)CommissionCalculationType.PercentagePlusFixed => (itemTotal * (totalPercentage / 100m)) + totalFixed,
                (int)CommissionCalculationType.PerUnit => totalFixed * quantity,
                _ => 0
            };
        }

        private bool IsRuleApplicable(CommissionRule rule, int vendorId, int productId, List<int> categoryIds)
        {
            if (rule.TargetProductId.HasValue && rule.TargetProductId.Value != productId)
                return false;
            if (rule.TargetVendorId.HasValue && rule.TargetVendorId.Value != vendorId)
                return false;
            if (rule.TargetCategoryId.HasValue && !categoryIds.Contains(rule.TargetCategoryId.Value))
                return false;
            return true;
        }

        private async Task<List<CommissionRule>> GetActiveRulesCachedAsync()
        {
            var cacheKey = _staticCacheManager.PrepareKeyForDefaultCache(new CacheKey("marketplace.commission.rules.active"));

            return await _staticCacheManager.GetAsync(cacheKey, async () =>
            {
                var now = DateTime.UtcNow;
                var rules = await _ruleRepository.GetAllAsync(q => q.Where(r =>
                    r.IsActive &&
                    (!r.EffectiveFromUtc.HasValue || r.EffectiveFromUtc <= now) &&
                    (!r.EffectiveToUtc.HasValue || r.EffectiveToUtc >= now)
                ));
                return rules.ToList();
            });
        }

        public async Task<CommissionSplitResult> GetExistingSplitsAsync(int nativeOrderId)
        {
            var existingSplits = await _splitRepository.GetAllAsync(q => q.Where(s => s.NativeOrderId == nativeOrderId));
            if (!existingSplits.Any())
            {
                throw new Exception($"FATAL: No commission splits found for Order {nativeOrderId}. Escrow cannot be released without an immutable ledger.");
            }
            return MapToResult(nativeOrderId, existingSplits.ToList());
        }
    }
}