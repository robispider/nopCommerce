using System.Threading.Tasks;
using Nop.Plugin.Marketplace.Order.Domains;

namespace Nop.Plugin.Marketplace.Order.Services
{
    public interface IOrderAllocationService
    {
        Task<MarketplaceOrderGroup> SplitNativeOrderAsync(Nop.Core.Domain.Orders.Order nativeOrder);
    }
}