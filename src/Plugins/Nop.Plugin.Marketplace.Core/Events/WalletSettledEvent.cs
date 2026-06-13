namespace Nop.Plugin.Marketplace.Core.Events
{
    public class WalletSettledEvent
    {
        public int EscrowTransactionId { get; set; }
        public string IdempotencyKey { get; set; }
    }
}