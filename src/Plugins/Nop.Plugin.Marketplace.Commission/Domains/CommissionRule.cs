using System;
using Nop.Core;

namespace Nop.Plugin.Marketplace.Commission.Domains
{
    public partial class CommissionRule : BaseEntity
    {
        public string Name { get; set; }

        public int PriorityId { get; set; } // 10=SKU, 20=Vendor, 30=Category, 40=Tier, 100=Global
        public bool IsModifier { get; set; } // If true, adds/subtracts from the Base Rule (e.g. -1% for Gold Tier)

        public int CalculationTypeId { get; set; } // Maps to CommissionCalculationType

        public int? TargetVendorId { get; set; }
        public int? TargetProductId { get; set; }
        public int? TargetCategoryId { get; set; }

        // Fee Settings
        public decimal Percentage { get; set; }
        public decimal FixedAmount { get; set; }

        // Elite Caps
        public decimal? MinimumFeeAmount { get; set; }
        public decimal? MaximumFeeAmount { get; set; }

        // Lifecycle
        public DateTime? EffectiveFromUtc { get; set; }
        public DateTime? EffectiveToUtc { get; set; }
        public bool IsActive { get; set; }

        public DateTime CreatedOnUtc { get; set; }
    }
}