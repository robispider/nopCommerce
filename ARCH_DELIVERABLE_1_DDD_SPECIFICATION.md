# DELIVERABLE 1: Domain-Driven Design Specification

---

## ⚡ CURRENT IMPLEMENTATION STATUS

**Last Assessed:** January 2025  
**Completion:** 70% (7/14 contexts with code, 7 missing)

| Context | Status | Evidence | Priority |
|---------|--------|----------|----------|
| Business | ✅ 85% | MarketplaceBusiness, BusinessDocument entities exist | Critical |
| Wholesale | ✅ 75% | SupplierProduct, pricing rules | Critical |
| Dropship | ✅ 80% | DropshipFulfillment, 3-state machine | High |
| Escrow | ✅ 95% | 13-state machine, two-phase settlement handshake | Critical |
| Wallet | ✅ 90% | Tri-state balance, IdempotencyKey enforced, serializable TX | Critical |
| Accounting | ✅ 75% | JournalEntry, GL double-entry validation | High |
| Risk | ✅ 60% | Consumers exist, reserve logic partial | Medium |
| **Inventory** | ❌ 0% | MISSING: InventoryBucket, StockReservation | **BLOCKING** |
| **Order** | ❌ 0% | MISSING: MarketplaceOrderGroup, allocation | **BLOCKING** |
| **Commission** | ⚠️ 40% | CommissionService exists but hardcoded | Medium |
| **Notification** | ❌ 0% | MISSING | Low |
| **API Integration** | ❌ 0% | MISSING | Low |
| **Disputes** | ❌ 0% | MISSING ChargebackCase GL posting | Medium |
| **Tax/Compliance** | ❌ 0% | MISSING | Low |

**Critical Gaps:**
- Inventory allocation system completely missing (blocks order placement)
- Order splitting not implemented (multi-vendor orders unsupported)
- Commission tiering hardcoded (no vendor-specific rules)

---

## BOUNDED CONTEXTS

### 1. Business Context
**Purpose:** Vendor onboarding, KYC verification, compliance.

**Responsibilities:**
- Vendor registration → MarketplaceBusiness entity
- Document upload & validation
- Verification workflow (Pending → Approved → Suspended/Rejected)
- Role assignment (Supplier, Reseller, Both)

**Data Ownership:**
- MarketplaceBusiness (legal entity)
- BusinessDocument (KYC artifacts)
- VendorRole (Supplier/Reseller/Both)

**Public Interfaces:**
- `IBusinessService.SubmitKycAsync(vendorId, documents)`
- `IBusinessService.ApproveBusinessAsync(businessId)`
- `IBusinessService.GetBusinessByVendorIdAsync(vendorId)`

**Consumed Events:**
- None (entry point)

**Published Events:**
- `BusinessApprovedEvent`
- `BusinessSuspendedEvent`
- `BusinessRejectedEvent`

---

### 2. Wholesale Context
**Purpose:** B2B product catalog, pricing, sourcing rules.

**Responsibilities:**
- Supplier product registration with wholesale settings
- MOQ (Minimum Order Quantity) enforcement
- Lead time management
- Procurement policy configuration
- Preorder & dropship enablement

**Data Ownership:**
- SupplierProduct (B2B rules per product)
- ProcurementPolicy (payment terms)
- LeadTimeConfig

**Public Interfaces:**
- `ISupplierProductService.RegisterProductAsync(productId, wholesalePrice, moq)`
- `ISupplierProductService.UpdateProcurementPoliciesAsync(productId, allowedPolicies)`
- `ISupplierProductService.GetB2BCatalogAsync(excludeVendorId, pageIndex)`

**Consumed Events:**
- ProductCreatedEvent (native)
- ProductUpdatedEvent (native)

**Published Events:**
- `SupplierProductRegisteredEvent`
- `ProcurementPolicyChangedEvent`
- `SupplierStockChangedEvent`

---

### 3. Storefront Context
**Purpose:** Dynamic URL routing, reseller branding, white-label experience.

**Responsibilities:**
- Storefront creation & management
- URL slug → VendorId resolution
- Custom domain mapping (future)
- Branding injection (logo, colors, theme)
- Storefront visibility/SEO

**Data Ownership:**
- ResellerStorefront (routing & branding)
- StorefrontSetting (color, fonts, etc.)

**Public Interfaces:**
- `IStorefrontService.CreateStorefrontAsync(vendorId, slug, branding)`
- `IStorefrontContext.GetCurrentStorefrontAsync()` (request-scoped)
- `IStorefrontService.GetBySlugAsync(slug)`

**Consumed Events:**
- BusinessApprovedEvent (enable storefront)

**Published Events:**
- `StorefrontCreatedEvent`
- `StorefrontBrandingChangedEvent`
- `StorefrontActivatedEvent`

---

### 4. Inventory Context
**Purpose:** Stock management, reservations, multi-sourcing allocation.

**Responsibilities:**
- Inventory ownership model (Supplier stock, Reseller imported, Platform common)
- Stock reservation (order → fulfillment)
- Backorder & oversell rules
- Real-time synchronization between supplier & reseller
- Conflict resolution (oversold items)

**Data Ownership:**
- InventoryBucket (stock ledger per source)
- StockReservation (order-item → stock link)
- InventoryAllocation (rules engine)

**Public Interfaces:**
- `IInventoryService.ReserveStockAsync(orderId, items, allocationRules)`
- `IInventoryService.ReleaseReservationAsync(reservationId)`
- `IInventoryService.SyncSupplierStockAsync(supplierVendorId)`
- `IInventoryService.CanFulfillAsync(items, policies)`

**Consumed Events:**
- OrderPlacedEvent
- SupplierStockChangedEvent
- FulfillmentCancelledEvent

**Published Events:**
- `StockReservedEvent`
- `StockReleasedEvent`
- `BackorderCreatedEvent`
- `OversellDetectedEvent`
- `StockConflictResolvedEvent`

---

### 5. Order Management Context
**Purpose:** Unified order orchestration across multiple vendors.

**Responsibilities:**
- MarketplaceOrderGroup creation (B2C order wrapper)
- Order splitting by vendor & fulfillment method
- Order state transitions
- Order-to-fulfillment mapping

**Data Ownership:**
- MarketplaceOrderGroup (meta order)
- MarketplaceOrderAllocation (vendor split)
- OrderFulfillmentMethod (dropship/pickup/standard)

**Public Interfaces:**
- `IOrderManagementService.CreateMarketplaceOrderGroupAsync(nativeOrderId)`
- `IOrderManagementService.SplitOrderAsync(orderGroupId)`
- `IOrderManagementService.GetOrderBreakdownAsync(orderGroupId)`

**Consumed Events:**
- OrderPlacedEvent (native) → Initiates splitting
- ShipmentCreatedEvent (native)
- ShipmentDeliveredEvent (native)

**Published Events:**
- `MarketplaceOrderGroupCreatedEvent`
- `OrderSplitCompletedEvent`
- `DropshipTicketCreatedEvent`
- `LocalPickupTicketCreatedEvent`

---

### 6. Fulfillment Context
**Purpose:** Supplier fulfillment workflows (dropship, pickup, preorder).

**Responsibilities:**
- Dropship ticket lifecycle (pending → accepted → shipped → delivered)
- Preorder tracking
- Local pickup coordination
- Tracking number management
- Delivery confirmation

**Data Ownership:**
- DropshipFulfillment (supplier ticket)
- FulfillmentStateHistory (audit)
- PreorderTracking

**Public Interfaces:**
- `IFulfillmentService.GetSupplierTicketsAsync(supplierVendorId)`
- `IFulfillmentService.AcceptTicketAsync(ticketId)`
- `IFulfillmentService.ShipTicketAsync(ticketId, tracking)`
- `IFulfillmentService.ConfirmDeliveryAsync(ticketId)`

**Consumed Events:**
- DropshipTicketCreatedEvent
- OrderCancelledEvent

**Published Events:**
- `TicketAcceptedEvent`
- `TicketShippedEvent`
- `DeliveryConfirmedEvent`
- `TicketCancelledEvent`

---

### 7. Escrow Context
**Purpose:** Financial hold management, dispute resolution, settlement orchestration.

**Responsibilities:**
- Escrow state machine (13 states)
- Dispute workflow
- Grace period enforcement
- Settlement trigger
- Refund initiation
- Auto-release scheduling

**Data Ownership:**
- EscrowTransaction (order hold)
- EscrowStateHistory (audit)
- DisputeCase (raised issues)

**Public Interfaces:**
- `IEscrowService.CreateEscrowAsync(orderId, amount)`
- `IEscrowService.TransitionStateAsync(escrowId, newState, reason)`
- `IEscrowService.RaiseDisputeAsync(escrowId, reason, evidence)`
- `IEscrowService.ResolveDisputeAsync(disputeId, decision)`

**Consumed Events:**
- OrderPaidEvent → Fund escrow
- DeliveryConfirmedEvent → Delivered state
- RefundRequestedEvent → Initiate refund

**Published Events:**
- `EscrowFundedEvent`
- `EscrowReleasedEvent`
- `EscrowDisputedEvent`
- `EscrowRefundedEvent`
- `SettlementReadyEvent` (triggers Wallet)

---

### 8. Wallet Context
**Purpose:** Vendor balance management, settlement processing, withdrawal handling.

**Responsibilities:**
- Multi-tiered balance (Available, Pending, Reserve)
- Settlement credit distribution
- Withdrawal request workflow
- Balance updates with ACID guarantees
- Ledger tracking

**Data Ownership:**
- WalletAccount (vendor balance state)
- WalletLedger (immutable transaction log)
- WithdrawalRequest (cash-out workflow)

**Public Interfaces:**
- `IWalletService.ProcessSettlementAsync(settlementEvent)`
- `IWalletService.RequestWithdrawalAsync(vendorId, amount)`
- `IWalletService.ApproveWithdrawalAsync(requestId)`
- `IWalletService.GetBalanceAsync(vendorId)`

**Consumed Events:**
- SettlementReadyEvent
- ReserveReleasedEvent
- ChargebackDeductedEvent

**Published Events:**
- `WalletCreditedEvent`
- `WithdrawalRequestedEvent`
- `WithdrawalApprovedEvent`
- `WithdrawalFailedEvent`

---

### 9. Accounting Context
**Purpose:** Financial reconciliation, GL posting, audit trail.

**Responsibilities:**
- Double-entry bookkeeping
- GL account management
- Journal entry creation & posting
- Trial balance maintenance
- Vendor payable tracking
- Commission tracking

**Data Ownership:**
- GlAccount (chart of accounts)
- JournalEntry (transaction header)
- JournalEntryLine (debit/credit pairs)
- VendorPayableAccount

**Public Interfaces:**
- `IAccountingService.GetAccountAsync(accountCode)`
- `IAccountingService.PostTransactionAsync(journalEntry, lines)`
- `IAccountingService.GetTrialBalanceAsync(date)`
- `IAccountingService.GenerateVendorReportAsync(vendorId, dateRange)`

**Consumed Events:**
- OrderPaidEvent
- SettlementReadyEvent
- ReserveHoldCreatedEvent
- ChargebackDeductedEvent

**Published Events:**
- `TransactionPostedEvent` (for audit)

---

### 10. Risk Context
**Purpose:** Financial risk mitigation (reserves, chargebacks, fraud).

**Responsibilities:**
- Vendor reserve rule management
- Reserve hold scheduling
- Chargeback tracking
- Fraud flagging
- Dynamic hold adjustment
- Vendor risk scoring

**Data Ownership:**
- VendorReserveRule (policy)
- ReserveSchedule (individual hold)
- ChargebackCase (dispute record)
- VendorRiskScore

**Public Interfaces:**
- `IRiskService.CalculateReserveHoldAsync(vendorId, amount)`
- `IChargebackService.RecordChargebackAsync(orderId, amount, reason)`
- `IRiskService.GetVendorRiskScoreAsync(vendorId)`
- `IRiskService.GetReserveScheduleAsync(vendorId)`

**Consumed Events:**
- WalletCreditedEvent → Calculate hold
- DisputeResolvedEvent → Adjust reserve
- ChargebackNotificationEvent (external) → Record chargeback

**Published Events:**
- `ReserveHoldCreatedEvent`
- `ReserveReleasedEvent`
- `ChargebackDeductedEvent`
- `VendorRiskAdjustedEvent`

---

### 11. Search & Discovery Context
**Purpose:** Product search, faceting, catalog browsing.

**Responsibilities:**
- Elasticsearch/OpenSearch indexing
- B2B vs B2C search separation
- Storefront-scoped search
- Faceted navigation (price, category, supplier)
- Real-time index updates

**Data Ownership:**
- SearchIndex (denormalized product data)
- FacetConfiguration

**Public Interfaces:**
- `ISearchService.SearchProductsAsync(query, filters, storefront)`
- `ISearchService.IndexProductAsync(productId)`
- `ISearchService.RebuildIndexAsync()`

**Consumed Events:**
- ProductClonedEvent → Index reseller product
- SupplierStockChangedEvent → Update availability
- ProductDeletedEvent → Remove from index

**Published Events:**
- `SearchIndexUpdatedEvent`

---

### 12. Commission Context (NEW CONTEXT - Currently Missing)
**Purpose:** Commission calculation, split distribution, vendor earnings tracking.

**Responsibilities:**
- Commission rule engine (tiered, percentage, fixed)
- Split calculation (supplier/reseller/platform)
- Commission tracking & reporting
- Dynamic commission adjustments
- Promotional overrides

**Data Ownership:**
- CommissionRule (rate card)
- CommissionSplit (per-order calculation)
- CommissionAdjustment (promotional)

**Public Interfaces:**
- `ICommissionService.CalculateSplitAsync(orderId)`
- `ICommissionService.GetCommissionRuleAsync(vendorId)`
- `ICommissionService.ApplyPromotionalAdjustmentAsync(orderId, adjustment)`

**Consumed Events:**
- OrderPaidEvent → Calculate split
- SettlementReadyEvent → Validate split

**Published Events:**
- `CommissionCalculatedEvent`
- `CommissionAdjustedEvent`

---

### 13. Notification Context (NEW CONTEXT - Currently Missing)
**Purpose:** Multi-channel notifications for actors.

**Responsibilities:**
- Email notifications (orders, settlement, disputes)
- SMS for critical alerts
- In-app notifications
- Webhook notifications (external systems)
- Notification preferences per vendor

**Data Ownership:**
- NotificationTemplate
- VendorNotificationPreference
- NotificationLog (audit)

**Public Interfaces:**
- `INotificationService.SendNotificationAsync(recipientId, templateId, data)`
- `INotificationService.SetPreferencesAsync(vendorId, preferences)`
- `IWebhookService.RegisterWebhookAsync(vendorId, event, url)`

**Consumed Events:**
- All business events
- Published via webhook to external systems

---

### 14. API Integration Context (NEW CONTEXT - Currently Missing)
**Purpose:** Third-party integrations (ERP, tax, shipping).

**Responsibilities:**
- Webhook handler framework
- Rate limiting per API
- Retry policy & backoff
- API key management
- Integration logging

**Data Ownership:**
- ApiIntegrationConfig
- WebhookSubscription
- IntegrationLog

**Public Interfaces:**
- `IApiIntegrationService.RegisterIntegrationAsync(config)`
- `IWebhookService.ProcessWebhookAsync(eventType, payload)`

**Consumed Events:**
- All events (for external system notification)

---

## AGGREGATE ROOTS & INVARIANTS

### MarketplaceBusiness
```
Root: MarketplaceBusiness
├── Invariants:
│   ├── VendorId must exist in native Vendor table
│   ├── VerificationStatus must be one of: Pending, Approved, Suspended, Rejected
│   ├── Role must be: Supplier, Reseller, or Both
│   ├── Legal name cannot be empty
│   └── At least one KYC document required before approval
├── Lifecycle:
│   Pending (registration) 
│   → Under Review (docs uploaded)
│   → Approved (can operate)
│   → Suspended (compliance issue)
│   → Rejected (KYC failed)
└── Children:
    └── BusinessDocument[]
```

### Storefront
```
Root: ResellerStorefront
├── Invariants:
│   ├── VendorId must have Reseller or Both role
│   ├── UrlSlug must be unique, lowercase, alphanumeric + hyphens
│   ├── UrlSlug must not conflict with system routes
│   ├── StoreName cannot be empty
│   ├── IsActive=false if parent Business not Approved
│   └── PrimaryColorHex must be valid hex or null
├── Lifecycle:
│   Draft (not public)
│   → Active (public, indexed)
│   → Inactive (archived)
└── Children:
    ├── StorefrontBranding
    └── StorefrontSetting[]
```

### SupplierProduct
```
Root: SupplierProduct
├── Invariants:
│   ├── ProductId must exist in native Product table
│   ├── VendorId must have Supplier or Both role
│   ├── WholesalePrice > 0
│   ├── MinimumOrderQuantity >= 1
│   ├── LeadTimeDays >= 0
│   ├── AllowedProcurementPolicies must include at least one flag
│   └── If IsDropshipEnabled=true, then AllowedProcurementPolicies must include FullEscrow or ResellerPrepay
├── Lifecycle:
│   Draft
│   → Active (available for reseller import)
│   → Inactive (no new imports)
└── No children (read-only from domain perspective)
```

### ResellerProductClone
```
Root: ResellerProduct (via ResellerProductMapping)
├── Invariants:
│   ├── ResellerCoreProductId (native Product) must have VendorId=Reseller
│   ├── SupplierCoreProductId (native Product) must have SupplierProduct
│   ├── MarginPercentage >= 0 and <= 500 (sanity check)
│   ├── SelectedProcurementPolicyId must be in Supplier's AllowedPolicurementPolicies
│   ├── Can only be created if Parent Supplier Product exists
│   └── Cannot clone from own vendor
├── Lifecycle:
│   Active (imported, can sell)
│   → Inactive (stop selling)
└── Relationship:
    ResellerProductMapping → (SupplierProduct, ResellProduct)
```

### MarketplaceOrderGroup
```
Root: MarketplaceOrderGroup
├── Invariants:
│   ├── NativeOrderId (1:1 mapping to native Order)
│   ├── TotalAmount = Sum of allocations
│   ├── Status transitions are immutable (no backward transitions except from certain error states)
│   └── Must have at least one allocation
├── Lifecycle:
│   Created (order placed)
│   → Allocated (split by vendor)
│   → Fulfilling (tickets created)
│   → Partial Fulfilled (some items shipped)
│   → Fulfilled (all shipped)
│   → Completed (all delivered, settled)
│   → [Error states: Cancelled, Failed]
└── Children:
    ├── MarketplaceOrderAllocation[]
    ├── EscrowTransaction
    └── FulfillmentTicket[]
```

### MarketplaceOrderAllocation
```
Root: MarketplaceOrderAllocation
├── Invariants:
│   ├── VendorId must exist
│   ├── AllocatedAmount > 0
│   ├── AllocationMethod must be: Dropship, LocalPickup, StandardShipping
│   ├── FulfillmentOwner (Supplier or Reseller)
│   └── Cannot change allocation method after fulfillment starts
├── Lifecycle:
│   Pending
│   → Confirmed (inventory reserved)
│   → Fulfilling (ticket created)
│   → Shipped (tracking confirmed)
│   → Delivered
└── Relationship:
    Belongs to MarketplaceOrderGroup
```

### EscrowTransaction
```
Root: EscrowTransaction
├── Invariants:
│   ├── One per MarketplaceOrderGroup
│   ├── Amount must match MarketplaceOrderGroup.TotalAmount
│   ├── CurrentState must be valid EscrowState
│   ├── State transitions follow legal paths only
│   ├── Cannot transition to Settled without all fulfillments Delivered
│   └── IdempotencyKey must be unique per settlement
├── Lifecycle (13 states):
│   Created
│   → Funded (order paid)
│   → Processing (supplier accepted)
│   → Shipped (tracking confirmed)
│   → Delivered (courier confirmed)
│   → GracePeriod (72h wait for disputes)
│   → SettlementPending (ready for wallet release)
│   → Settled (wallet confirmed)
│   
│   [Dispute path]:
│   Disputed (raised by consumer or admin)
│   → Refunded (funds returned)
│   
│   [Cancel path]:
│   Cancelled (before payment or on request)
└── Children:
    └── EscrowStateHistory[]
```

### DropshipFulfillment
```
Root: DropshipFulfillment
├── Invariants:
│   ├── SupplierVendorId must have Supplier role
│   ├── ResellerVendorId must have Reseller role (or null if direct)
│   ├── LockedWholesalePrice is immutable (set at order time)
│   ├── LockedRetailPrice is immutable (set at order time)
│   ├── Status transitions follow: Pending → Accepted → Shipped → Delivered
│   └── TrackingNumber required before Shipped state
├── Lifecycle:
│   AwaitingResellerDeposit (if ResellerPrepay policy)
│   → [Reseller funds]
│   → Pending (waiting for supplier)
│   → Accepted (supplier acknowledged)
│   → Shipped (tracking provided)
│   → Delivered (courier confirmed)
└── Relationship:
    Belongs to MarketplaceOrderAllocation
```

### WalletAccount
```
Root: WalletAccount
├── Invariants:
│   ├── One per VendorId
│   ├── AvailableBalance >= 0
│   ├── PendingBalance >= 0
│   ├── ReserveBalance >= 0
│   ├── TotalBalance = Available + Pending + Reserve
│   ├── ConcurrencyVersion auto-increments (optimistic locking)
│   └── Cannot withdraw more than AvailableBalance
├── Lifecycle:
│   Created (when vendor approved)
│   → Activated (first settlement)
│   → Active (steady state)
└── Children:
    ├── WalletLedger[]
    └── WithdrawalRequest[]
```

### ReserveSchedule
```
Root: ReserveSchedule
├── Invariants:
│   ├── VendorId must exist
│   ├── EscrowTransactionId must reference valid escrow
│   ├── HeldAmount > 0
│   ├── ReleaseOnUtc > CreatedOnUtc
│   ├── IsReleased can only transition false → true (not backward)
│   └── Cannot release before ReleaseOnUtc
├── Lifecycle:
│   Active (money held)
│   → Released (funds returned to AvailableBalance)
└── Relationship:
    Multiple schedules per VendorId (staggered releases)
```

### JournalEntry
```
Root: JournalEntry
├── Invariants:
│   ├── Must have 2+ lines
│   ├── Sum(Debits) == Sum(Credits) [GOLDEN RULE]
│   ├── Sum > 0
│   ├── IdempotencyKey must be unique
│   ├── Reference must link to source (order, settlement, etc.)
│   └── Cannot modify after posted
├── Lifecycle:
│   Draft
│   → Posted (immutable)
└── Children:
    └── JournalEntryLine[] (2+)
```

### VendorReserveRule
```
Root: VendorReserveRule
├── Invariants:
│   ├── VendorId=0 is global default
│   ├── HoldPercentage must be 0-100
│   ├── HoldDays must be > 0
│   ├── One rule per vendor (upsert)
└── Lifecycle:
    Active (enforced on settlement)
    → Inactive (archived)
```

---

## DOMAIN EVENTS CATALOG

| Event | Publisher | Consumers | Payload |
|-------|-----------|-----------|---------|
| **BusinessApprovedEvent** | Business | Storefront, Wallet, Risk | {businessId, vendorId, approvedAt} |
| **BusinessSuspendedEvent** | Business | Storefront, Wallet | {businessId, vendorId, reason} |
| **StorefrontCreatedEvent** | Storefront | Search | {storefrontId, slug, vendorId} |
| **ProductClonedEvent** | Wholesale | Search, Accounting | {supplierProductId, resellerProductId, margin, idempotencyKey} |
| **SupplierStockChangedEvent** | Wholesale | Inventory, Search, Fulfillment | {productId, vendorId, newQty, oldQty} |
| **MarketplaceOrderGroupCreatedEvent** | Order Mgmt | Escrow, Accounting | {orderGroupId, nativeOrderId, totalAmount} |
| **OrderSplitCompletedEvent** | Order Mgmt | Fulfillment, Accounting | {orderGroupId, allocations[]} |
| **DropshipTicketCreatedEvent** | Fulfillment | Risk, Notification | {ticketId, supplierVendorId, items, deadline} |
| **TicketAcceptedEvent** | Fulfillment | Inventory, Risk | {ticketId, acceptedAt} |
| **TicketShippedEvent** | Fulfillment | Escrow, Risk, Notification | {ticketId, trackingNumber, shippedAt} |
| **DeliveryConfirmedEvent** | Fulfillment | Escrow, Risk | {ticketId, deliveredAt} |
| **EscrowFundedEvent** | Escrow | Risk, Accounting | {escrowId, amount, fundedAt, idempotencyKey} |
| **EscrowReleasedEvent** | Escrow | Wallet, Accounting, Risk | {escrowId, amount, releasedAt} |
| **EscrowDisputedEvent** | Escrow | Risk, Notification | {escrowId, reason, raisedBy, raisedAt} |
| **EscrowRefundedEvent** | Escrow | Wallet, Accounting, Risk, Notification | {escrowId, refundAmount, reason, refundedAt} |
| **SettlementReadyEvent** | Escrow | Wallet, Accounting, Commission | {escrowId, escrowTransactionId, supplierAmount, resellerAmount, platformFee, idempotencyKey} |
| **WalletCreditedEvent** | Wallet | Risk, Accounting, Notification | {walletAccountId, vendorId, amount, referenceId, idempotencyKey} |
| **WithdrawalRequestedEvent** | Wallet | Risk, Notification | {withdrawalId, vendorId, amount, requestedAt} |
| **WithdrawalApprovedEvent** | Wallet | Accounting, Notification | {withdrawalId, approvedAt, paymentMethod} |
| **ReserveHoldCreatedEvent** | Risk | Wallet, Accounting | {scheduleId, vendorId, amount, releaseDate} |
| **ReserveReleasedEvent** | Risk | Wallet, Accounting | {scheduleId, releaseAmount, releasedAt} |
| **ChargebackDeductedEvent** | Risk | Wallet, Accounting, Notification | {chargebackId, vendorId, amount, deductedAt} |
| **CommissionCalculatedEvent** | Commission | Accounting | {orderId, supplierAmount, resellerAmount, platformFee, idempotencyKey} |

---

## COMMANDS (Intention-based)

### Business Context
- `ApproveBusiness` → BusinessApprovedEvent
- `SuspendBusiness` → BusinessSuspendedEvent
- `RejectBusiness` → BusinessRejectedEvent
- `UploadKycDocument` → DocumentUploadedEvent

### Wholesale Context
- `RegisterSupplierProduct` → SupplierProductRegisteredEvent
- `UpdateWholesalePrice` → ProcurementPolicyChangedEvent
- `EnableDropshipping` → DropshipEnabledEvent

### Storefront Context
- `CreateStorefront` → StorefrontCreatedEvent
- `UpdateStorefrontBranding` → StorefrontBrandingChangedEvent
- `ActivateStorefront` → StorefrontActivatedEvent

### Order Management Context
- `PlaceMarketplaceOrder` → MarketplaceOrderGroupCreatedEvent
- `SplitOrder` → OrderSplitCompletedEvent
- `CancelOrder` → OrderCancelledEvent

### Fulfillment Context
- `AcceptFulfillmentTicket` → TicketAcceptedEvent
- `ShipFulfillmentTicket` → TicketShippedEvent
- `ConfirmDelivery` → DeliveryConfirmedEvent

### Escrow Context
- `FundEscrow` → EscrowFundedEvent
- `ReleaseEscrow` → EscrowReleasedEvent
- `RaiseDispute` → EscrowDisputedEvent
- `ResolveDispute` → EscrowResolvedEvent
- `RefundEscrow` → EscrowRefundedEvent
- `InitiateSettlement` → SettlementReadyEvent

### Wallet Context
- `ProcessSettlement` → WalletCreditedEvent
- `RequestWithdrawal` → WithdrawalRequestedEvent
- `ApproveWithdrawal` → WithdrawalApprovedEvent
- `ApplyChargeback` → WalletChargedEvent

### Risk Context
- `CalculateReserveHold` → ReserveHoldCreatedEvent
- `ReleaseReserve` → ReserveReleasedEvent
- `RecordChargeback` → ChargebackDeductedEvent

---

## VALUE OBJECTS (Immutable)

### Money
```
Money(amount: decimal, currency: string)
├── Invariants: amount >= 0, currency != null
└── Methods: Add(), Subtract(), Compare()
```

### CommissionSplit
```
CommissionSplit(supplierAmount, resellerAmount, platformFee)
├── Invariants: all >= 0, sum == total
└── Fields: SupplierCut%, ResellerCut%, PlatformCut%
```

### FulfillmentMethod
```
Enum: Dropship, LocalPickup, StandardShipping
├── Each with: Cost, EstimatedDelivery, Carrier
└── Immutable configuration
```

### ProcurementPolicy
```
Flag Enum: FullEscrow, ResellerPrepay, RollingReserve, CreditLimit
├── Immutable combinations
└── Validation: At least one must be enabled
```

### EscrowState
```
Enum: 13 distinct states
├── Created, Funded, Processing, Shipped, Delivered, GracePeriod, 
│   SettlementPending, Settled, Disputed, Refunded, Cancelled
└── State transition matrix (legal paths only)
```

---

## MISSING CONTEXTS IDENTIFIED

1. **Commission Context** - Currently hardcoded in CommissionService
   - Needs: Rule engine, tiered rates, promotional overrides
   - Risk: Cannot audit commission calculations per order

2. **Notification Context** - Not explicitly modeled
   - Needs: Multi-channel (email, SMS, webhook), preferences, audit log
   - Risk: Missed notifications (no retry/queue)

3. **API Integration Context** - Not modeled
   - Needs: Webhook framework, rate limiting, retry policies
   - Risk: Third-party integration failures not handled

4. **Dispute Resolution Context** - Minimal modeling
   - Needs: Evidence attachment, timeline, admin commentary
   - Risk: Cannot track resolution process

5. **Tax & Compliance Context** - Not present
   - Needs: VAT/GST calculation, tax exemption tracking, reporting
   - Risk: Non-compliant in international markets

6. **Vendor Performance Context** - Not present
   - Needs: Quality scoring, chargeback rates, on-time delivery metrics
   - Risk: Cannot identify problematic vendors

7. **Search & Discovery Context** - Exists but underdeveloped
   - Needs: Better faceting, personalization, trending
   - Risk: Poor discoverability affects vendor sales

---

## CRITICAL MISSING WORKFLOWS

1. **Oversell Resolution** - What happens when stock runs out mid-fulfillment?
2. **Backorder Management** - How are backorders tracked and fulfilled?
3. **Inventory Sync Conflicts** - If supplier & reseller stock diverge, who wins?
4. **Return/Exchange** - Not modeled; affects accounting & inventory
5. **Partial Refunds** - How are reserve holds adjusted?
6. **Multi-Supplier Orders** - When multiple suppliers can fulfill same item?
7. **Preorder to Fulfillment** - How does lead time affect order state?
8. **Vendor Deactivation** - What happens to in-flight orders?
9. **Commission Disputes** - How does vendor dispute commission calculation?
10. **Audit Log Completeness** - What events are immutable & auditable?

