using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Plugin.Marketplace.Core.Domains;


    public enum BusinessVerificationStatus
    {
        Pending = 10,
        Approved = 20,
        Rejected = 30,
        Suspended = 40
    }

    public enum MarketplaceRoleType
    {
        Supplier = 10,
        Reseller = 20,
        Both = 30
    }
