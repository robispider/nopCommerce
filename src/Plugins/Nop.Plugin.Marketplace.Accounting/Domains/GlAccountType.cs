namespace Nop.Plugin.Marketplace.Accounting.Domains
{
    public enum GlAccountType
    {
        Asset = 10,       // Money you have (e.g., Bank Account, Stripe Clearing)
        Liability = 20,   // Money you owe (e.g., Vendor Payables, Escrow Holding)
        Equity = 30,      // Platform Net Worth
        Revenue = 40,     // Platform Earnings (e.g., Commission, Gateway Fees)
        Expense = 50      // Platform Costs (e.g., Server Costs, Refund Losses)
    }
}