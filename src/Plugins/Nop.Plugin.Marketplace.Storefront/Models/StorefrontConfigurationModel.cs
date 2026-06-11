using System.ComponentModel.DataAnnotations;
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Marketplace.Storefront.Models
{
    public record StorefrontConfigurationModel : BaseNopModel
    {
        public int Id { get; set; }

        [NopResourceDisplayName("Store Name")]
        public string StoreName { get; set; }

        [NopResourceDisplayName("URL Slug")]
        public string UrlSlug { get; set; }

        [NopResourceDisplayName("Primary Brand Color")]
        public string PrimaryColorHex { get; set; }

        [UIHint("Picture")]
        [NopResourceDisplayName("Store Logo")]
        public int LogoPictureId { get; set; }

        [UIHint("Picture")]
        [NopResourceDisplayName("Store Banner")]
        public int BannerPictureId { get; set; }

        [NopResourceDisplayName("Is Store Online?")]
        public bool IsActive { get; set; }
    }
}