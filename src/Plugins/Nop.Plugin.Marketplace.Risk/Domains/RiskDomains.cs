using System;
using Nop.Core;

namespace Nop.Plugin.Marketplace.Risk.Domains
{
    // The Rule: E.g., "Hold 20% for 45 Days"
    public partial class VendorReserveRule : BaseEntity
    {
        public int VendorId { get; set; } // 0 = Global Default Rule
        public decimal HoldPercentage { get; set; }
        public int HoldDays { get; set; }
    }

    // The Execution: The actual money held
    public partial class ReserveSchedule : BaseEntity
    {
        public int VendorId { get; set; }
        public int EscrowTransactionId { get; set; }
        public decimal HeldAmount { get; set; }
        public DateTime ReleaseOnUtc { get; set; }
        public bool IsReleased { get; set; }
        public DateTime CreatedOnUtc { get; set; }
    }

    // The Fraud Case
    public partial class ChargebackCase : BaseEntity
    {
        public int CoreOrderId { get; set; }
        public int VendorId { get; set; }
        public decimal DisputeAmount { get; set; }
        public string Reason { get; set; }
        public DateTime CreatedOnUtc { get; set; }
    }
}