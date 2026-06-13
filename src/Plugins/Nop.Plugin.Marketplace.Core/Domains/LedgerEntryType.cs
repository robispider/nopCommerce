namespace Nop.Plugin.Marketplace.Core.Domains
{
    public enum LedgerEntryType
    {
        Credit = 1, // Adds money to Vendor's Available/Reserve Balance
        Debit = 2   // Removes money from Vendor's Available/Reserve Balance
    }
}