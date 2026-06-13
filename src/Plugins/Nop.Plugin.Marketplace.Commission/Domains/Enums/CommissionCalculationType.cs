namespace Nop.Plugin.Marketplace.Commission.Domains.Enums
{
    public enum CommissionCalculationType
    {
        Percentage = 10,          // 5%
        FixedAmount = 20,         // $2.00
        PercentagePlusFixed = 30, // 2.9% + $0.30
        PerUnit = 40              // $1.00 per quantity sold
    }
}