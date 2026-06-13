using System;

namespace Nop.Plugin.Marketplace.Core.Domains
{
    [Flags]
    public enum ProcurementSettlementPolicy
    {
        None = 0,
        FullEscrow = 1,        // Supplier finances it; gets paid when delivered
        ResellerPrepay = 2,    // Reseller must deposit wholesale cost before supplier ships
        RollingReserve = 4,    // Reseller uses their platform reserve/wallet
        CreditLimit = 8        // Reseller uses their trust credit (B2B terms)
    }
}