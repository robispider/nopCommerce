using System;
using Nop.Core;

namespace Nop.Plugin.Marketplace.Wallet.Domains
{
    public partial class WithdrawalRequest : BaseEntity
    {
        public int VendorId { get; set; }
        public decimal Amount { get; set; }
        public int StatusId { get; set; }
        public string AdminNotes { get; set; }
        public DateTime CreatedOnUtc { get; set; }
        public DateTime UpdatedOnUtc { get; set; }
    }
}