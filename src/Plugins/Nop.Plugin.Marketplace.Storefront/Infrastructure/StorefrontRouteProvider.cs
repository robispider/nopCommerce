using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Nop.Web.Framework.Mvc.Routing;

namespace Nop.Plugin.Marketplace.Storefront.Infrastructure
{
    /// <summary>
    /// Represents the route provider for dynamic Reseller Storefronts.
    /// Intercepts traffic to /store/{slug} before native catch-all routes process it.
    /// </summary>
    // FIX: Interface is IRouteProvider in modern nopCommerce
    public class StorefrontRouteProvider : IRouteProvider
    {
        /// <summary>
        /// Register routing endpoints for the Storefront plugin.
        /// </summary>
        /// <param name="endpointRouteBuilder">The endpoint route builder provided by ASP.NET Core.</param>
        public void RegisterRoutes(IEndpointRouteBuilder endpointRouteBuilder)
        {
            // Map the custom storefront route pattern
            // Example: https://marketplace.com/store/shoeking -> StorefrontController.Index(slug: "shoeking")
            endpointRouteBuilder.MapControllerRoute(
                name: "Marketplace.Storefront.Frontend",
                pattern: "store/{slug}/{action=Index}/{id?}",
                defaults: new { controller = "Storefront", action = "Index" }
            );

            // Future-proofing: Admin UI route for Resellers to configure their storefront
            endpointRouteBuilder.MapControllerRoute(
                name: "Marketplace.Storefront.Admin",
                pattern: "Admin/Storefront/{action=Configure}/{id?}",
                defaults: new { controller = "StorefrontAdmin", action = "Configure", area = "Admin" }
            );
        }

        /// <summary>
        /// Gets the priority of the route provider.
        /// Must be higher than 0 so it registers before the standard nopCommerce generic/catch-all routes.
        /// </summary>
        public int Priority => 100;
    }
}