# DELIVERABLE 5: Plugin Implementation Blueprint

---

## PLUGIN LAYER ARCHITECTURE

Each bounded context = One plugin module (in some cases, multiple plugins for sub-contexts).

```
nopCommerce Core
│
├─ Nop.Plugin.Marketplace.Core (Foundation)
│  ├─ Shared events, enums, value objects
│  ├─ OutboxMessage infrastructure
│  ├─ IdempotencyKey utilities
│  └─ Base domain entities
│
├─ Nop.Plugin.Marketplace.Business (Business Context)
│  ├─ MarketplaceBusiness aggregate
│  ├─ BusinessDocument entity
│  ├─ KYC workflow
│  └─ Vendor verification
│
├─ Nop.Plugin.Marketplace.Wholesale (Wholesale Context)
│  ├─ SupplierProduct aggregate
│  ├─ Procurement policies
│  └─ B2B catalog
│
├─ Nop.Plugin.Marketplace.Storefront (Storefront Context)
│  ├─ ResellerStorefront aggregate
│  ├─ URL routing
│  ├─ Branding injection
│  └─ StorefrontContext service
│
├─ Nop.Plugin.Marketplace.Inventory (Inventory Context) [NEW PLUGIN]
│  ├─ InventoryBucket aggregate
│  ├─ StockReservation entity
│  ├─ Allocation rules engine
│  └─ Sync conflict handling
│
├─ Nop.Plugin.Marketplace.Order (Order Management Context) [NEW PLUGIN]
│  ├─ MarketplaceOrderGroup aggregate
│  ├─ MarketplaceOrderAllocation entity
│  ├─ Order splitting logic
│  └─ Fulfillment routing
│
├─ Nop.Plugin.Marketplace.Fulfillment (Fulfillment Context)
│  ├─ DropshipFulfillment aggregate
│  ├─ Fulfillment state machine
│  ├─ Ticket acceptance/shipping
│  └─ Tracking management
│
├─ Nop.Plugin.Marketplace.Escrow (Escrow Context)
│  ├─ EscrowTransaction aggregate
│  ├─ 13-state machine
│  ├─ Dispute workflow
│  └─ Settlement orchestration
│
├─ Nop.Plugin.Marketplace.Wallet (Wallet Context)
│  ├─ WalletAccount aggregate
│  ├─ Tri-state balance model
│  ├─ Settlement processing
│  └─ Withdrawal workflow
│
├─ Nop.Plugin.Marketplace.Accounting (Accounting Context)
│  ├─ GlAccount aggregate
│  ├─ JournalEntry aggregate
│  ├─ Double-entry posting
│  └─ Trial balance reconciliation
│
├─ Nop.Plugin.Marketplace.Risk (Risk Context)
│  ├─ VendorReserveRule aggregate
│  ├─ ReserveSchedule entity
│  ├─ ChargebackCase entity
│  ├─ Vendor scoring
│  └─ Reserve release task
│
├─ Nop.Plugin.Marketplace.Commission (Commission Context) [NEW PLUGIN]
│  ├─ CommissionRule aggregate
│  ├─ CommissionSplit entity
│  ├─ Rule engine
│  └─ Tiered calculation
│
├─ Nop.Plugin.Marketplace.Notification (Notification Context) [NEW PLUGIN]
│  ├─ NotificationTemplate aggregate
│  ├─ Multi-channel delivery
│  ├─ Preference management
│  └─ Event listeners
│
└─ Nop.Plugin.Marketplace.ApiIntegration (API Integration Context) [NEW PLUGIN]
   ├─ Webhook infrastructure
   ├─ Rate limiting
   ├─ Retry policies
   └─ External system sync
```

---

## PLUGIN TEMPLATE: INVENTORY CONTEXT

**Example: Nop.Plugin.Marketplace.Inventory**

### Project Structure
```
Nop.Plugin.Marketplace.Inventory/
├─ Areas/Admin/
│  └─ Controllers/
│     └─ InventoryAdminController.cs
├─ Components/
│  └─ InventoryWidgetComponent.cs
├─ Data/
│  ├─ InventoryBuilder.cs (FluentMigrator)
│  ├─ SchemaMigration.cs
│  └─ 20240101_InitialSchema.cs
├─ Domains/
│  ├─ InventoryBucket.cs
│  ├─ StockReservation.cs
│  └─ AllocationRule.cs
├─ Events/
│  ├─ Consumers/
│  │  ├─ OrderPlacedEventConsumer.cs
│  │  ├─ SupplierStockChangedEventConsumer.cs
│  │  └─ OrderCancelledEventConsumer.cs
│  └─ Producers/ [internal, for publishing]
├─ Infrastructure/
│  ├─ NopStartup.cs (DI registration)
│  ├─ InventoryRouteProvider.cs
│  └─ InventoryPermissionProvider.cs
├─ Services/
│  ├─ IInventoryService.cs
│  ├─ InventoryService.cs
│  ├─ IAllocationRuleService.cs
│  └─ AllocationRuleService.cs
├─ Models/
│  ├─ InventoryBucketModel.cs
│  └─ StockReservationModel.cs
├─ Views/Admin/
│  └─ Index.cshtml
├─ Migrations/
│  └─ (FluentMigrator handles)
├─ Tests/ [Optional for POC]
├─ plugin.json
└─ Nop.Plugin.Marketplace.Inventory.csproj
```

### Entity Definitions

**InventoryBucket.cs:**
```csharp
namespace Nop.Plugin.Marketplace.Inventory.Domains
{
    public class InventoryBucket : BaseEntity
    {
        public int ProductId { get; set; }
        public int? SourceVendorId { get; set; } // null = Platform
        public int BucketType { get; set; } // 10=Supplier, 20=Reseller, 30=Platform
        public int AvailableQuantity { get; set; }
        public int ReservedQuantity { get; set; }
        public int BackorderQuantity { get; set; }
        public DateTime UpdatedOnUtc { get; set; }

        // Invariants enforced in domain layer (not DB)
        // AvailableQuantity > 0
        // ReservedQuantity >= 0
        // BackorderQuantity >= 0
    }
}
```

**StockReservation.cs:**
```csharp
namespace Nop.Plugin.Marketplace.Inventory.Domains
{
    public class StockReservation : BaseEntity
    {
        public int InventoryBucketId { get; set; }
        public int OrderItemId { get; set; }
        public int QuantityReserved { get; set; }
        public DateTime ExpiresOnUtc { get; set; }
        public int Status { get; set; } // 10=Active, 20=Released, 30=Expired
        public DateTime CreatedOnUtc { get; set; }
        public DateTime? ReleasedOnUtc { get; set; }
    }
}
```

### Services

**IInventoryService.cs:**
```csharp
namespace Nop.Plugin.Marketplace.Inventory.Services
{
    public interface IInventoryService
    {
        // Reservation
        Task<StockReservation> ReserveStockAsync(int inventoryBucketId, int orderItemId, 
            int quantity, int expiryMinutes = 15);
        Task ReleaseReservationAsync(int reservationId);
        Task ConfirmReservationAsync(int reservationId);
        Task ReleaseExpiredReservationsAsync();

        // Inventory management
        Task<InventoryBucket> GetBucketAsync(int productId, int? vendorId, int bucketType);
        Task UpdateInventoryAsync(int bucketId, int availableDelta, int reservedDelta, 
            int backorderDelta);

        // Allocation
        Task<AllocationResult> AllocateStockAsync(int orderId, int[] itemIds, 
            AllocationPolicy policy);

        // Sync
        Task SyncResellerInventoryAsync(int resellerProductId, int supplierProductId);
        Task HandleInventorySyncConflictAsync(int productId);

        // Reporting
        Task<InventoryReport> GenerateInventoryReportAsync(int vendorId, DateTime date);
    }
}
```

### Event Consumers

**OrderPlacedEventConsumer.cs:**
```csharp
namespace Nop.Plugin.Marketplace.Inventory.Events
{
    public class OrderPlacedEventConsumer : IConsumer<OrderPlacedEvent>
    {
        private readonly IInventoryService _inventoryService;

        public async Task HandleEventAsync(OrderPlacedEvent eventMessage)
        {
            // Get order items
            // For each item: determine allocation strategy
            // Call ReserveStockAsync
            // If any fails: Set order to error state, notify customer
            // Publish StockReservedEvent
        }
    }
}
```

### Database Migration (FluentMigrator)

**InventoryBuilder.cs:**
```csharp
namespace Nop.Plugin.Marketplace.Inventory.Data
{
    public class InventoryBuilder : NopMigrationBase
    {
        public override void Up()
        {
            // InventoryBucket table
            Create.Table("InventoryBucket")
                .WithIdColumn()
                .WithColumn("ProductId").AsInt32().NotNullable()
                .WithColumn("SourceVendorId").AsInt32().Nullable()
                .WithColumn("BucketType").AsInt32().NotNullable()
                .WithColumn("AvailableQuantity").AsInt32().NotNullable()
                .WithColumn("ReservedQuantity").AsInt32().NotNullable()
                .WithColumn("BackorderQuantity").AsInt32().NotNullable()
                .WithColumn("UpdatedOnUtc").AsDateTime2().NotNullable();

            // Indexes
            Create.Index("IX_ProductId_BucketType")
                .OnTable("InventoryBucket")
                .OnColumn("ProductId").Ascending()
                .OnColumn("BucketType").Ascending();

            // StockReservation table
            Create.Table("StockReservation")
                .WithIdColumn()
                .WithColumn("InventoryBucketId").AsInt32().NotNullable()
                .WithColumn("OrderItemId").AsInt32().NotNullable()
                .WithColumn("QuantityReserved").AsInt32().NotNullable()
                .WithColumn("ExpiresOnUtc").AsDateTime2().NotNullable()
                .WithColumn("Status").AsInt32().NotNullable()
                .WithColumn("CreatedOnUtc").AsDateTime2().NotNullable()
                .WithColumn("ReleasedOnUtc").AsDateTime2().Nullable();

            Create.Index("IX_ExpiresOnUtc")
                .OnTable("StockReservation")
                .OnColumn("ExpiresOnUtc").Ascending();
        }
    }
}
```

### DI Registration

**NopStartup.cs:**
```csharp
namespace Nop.Plugin.Marketplace.Inventory.Infrastructure
{
    public class NopStartup : INopStartup
    {
        public int Order => 10; // Execute after core plugins

        public void Configure(IServiceCollection services)
        {
            // Register repositories
            services.AddScoped<IRepository<InventoryBucket>>();
            services.AddScoped<IRepository<StockReservation>>();

            // Register services
            services.AddScoped<IInventoryService, InventoryService>();
            services.AddScoped<IAllocationRuleService, AllocationRuleService>();

            // Register event consumers
            services.AddScoped<OrderPlacedEventConsumer>();
            services.AddScoped<OrderCancelledEventConsumer>();
            services.AddScoped<SupplierStockChangedEventConsumer>();

            // Register scheduled task
            services.AddScoped<ReserveExpiryTask>();
        }
    }
}
```

### Admin Controller

**InventoryAdminController.cs:**
```csharp
namespace Nop.Plugin.Marketplace.Inventory.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("admin/marketplace-inventory/")]
    [Authorize(Policy = MarketplacePermissionProvider.ManageInventory)]
    public class InventoryAdminController : BaseAdminController
    {
        private readonly IInventoryService _inventoryService;

        [HttpGet("list")]
        public async Task<IActionResult> List()
        {
            // Display inventory buckets, reservations, conflicts
        }

        [HttpPost("resolve-conflict")]
        public async Task<IActionResult> ResolveConflict(int productId)
        {
            // Handle inventory sync conflicts
        }
    }
}
```

### Settings & Configuration

**MarketplaceInventorySettings.cs:**
```csharp
namespace Nop.Plugin.Marketplace.Inventory.Settings
{
    public class MarketplaceInventorySettings : ISettings
    {
        public int ReservationExpiryMinutes { get; set; } = 15;
        public bool AllowOversell { get; set; } = true;
        public bool AllowBackorder { get; set; } = true;
        public int MaxBackorderDays { get; set; } = 45;
        public bool AutoSyncResellerInventory { get; set; } = true;
        public bool HardSyncOnConflict { get; set; } = false; // Soft sync by default
    }
}
```

### Plugin Configuration

**plugin.json:**
```json
{
  "Group": "Marketplace",
  "FriendlyName": "Marketplace Inventory Management",
  "SystemName": "Marketplace.Inventory",
  "Version": "1.00",
  "SupportedVersions": ["4.90"],
  "Author": "YourCompany",
  "DisplayOrder": 6,
  "FileName": "Nop.Plugin.Marketplace.Inventory.dll",
  "Description": "Multi-source inventory management with reservations, allocation rules, and sync conflict handling."
}
```

---

## CRITICAL DEPENDENCIES (Execution Order)

```
1. Marketplace.Core (foundation)
   └─ Publish: All core events, OutboxMessage infra

2. Marketplace.Business (onboarding)
   └─ Dependency: Core
   └─ Publish: BusinessApprovedEvent

3. Marketplace.Wholesale (product registration)
   └─ Dependency: Core, Business
   └─ Publish: SupplierStockChangedEvent

4. Marketplace.Inventory (stock tracking)
   └─ Dependency: Core, Wholesale
   └─ Consume: SupplierStockChangedEvent, OrderPlacedEvent

5. Marketplace.Order (order orchestration)
   └─ Dependency: Core, Inventory, Wholesale
   └─ Consume: OrderPlacedEvent
   └─ Publish: MarketplaceOrderGroupCreatedEvent, DropshipTicketCreatedEvent

6. Marketplace.Fulfillment (supplier tickets)
   └─ Dependency: Core, Order
   └─ Consume: DropshipTicketCreatedEvent

7. Marketplace.Escrow (financial hold)
   └─ Dependency: Core, Order, Fulfillment
   └─ Consume: OrderPaidEvent, DeliveryConfirmedEvent
   └─ Publish: SettlementReadyEvent

8. Marketplace.Commission (calc splits)
   └─ Dependency: Core, Escrow
   └─ Consume: OrderPaidEvent, SettlementReadyEvent

9. Marketplace.Wallet (balance mgmt)
   └─ Dependency: Core, Escrow, Commission
   └─ Consume: SettlementReadyEvent
   └─ Publish: WalletSettledEvent

10. Marketplace.Risk (chargeback protection)
    └─ Dependency: Core, Wallet
    └─ Consume: WalletSettledEvent

11. Marketplace.Accounting (GL posting)
    └─ Dependency: Core, Escrow, Wallet, Risk
    └─ Consume: All financial events

12. Marketplace.Storefront (branding)
    └─ Dependency: Core, Business
    └─ Consume: BusinessApprovedEvent

13. Marketplace.Notification (alerts) [OPTIONAL]
    └─ Dependency: Core, all contexts

14. Marketplace.ApiIntegration (webhooks) [OPTIONAL]
    └─ Dependency: Core, all contexts
```

---

## MISSING PLUGIN DEFINITIONS

Need architecture for:

1. **Nop.Plugin.Marketplace.Returns** - Return/exchange workflow
2. **Nop.Plugin.Marketplace.Disputes** - Dispute evidence & resolution
3. **Nop.Plugin.Marketplace.TaxCompliance** - VAT/GST calculation
4. **Nop.Plugin.Marketplace.VendorPerformance** - Metrics & scoring
5. **Nop.Plugin.Marketplace.Analytics** - Dashboards & reporting

