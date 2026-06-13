# nopCommerce Marketplace Plugin Architecture - Comprehensive Technical Report

**Report Generated:** January 2025  
**Project:** nopCommerce B2B/B2C Marketplace System  
**Scope:** Analysis of Nop.Plugin.Marketplace.* plugin ecosystem  
**Target Audience:** System architects, integration partners, AI development assistants

---

## Executive Summary

The marketplace system implements a sophisticated multi-vendor B2B/B2C platform built on nopCommerce 4.90. It supports:

- **Multi-tier vendor ecosystems** (Suppliers → Resellers → End Consumers)
- **Dynamic storefront generation** with white-label capabilities
- **Dropship fulfillment** with mixed-cart order routing
- **Financial risk management** with escrow, wallet, and reserve holds
- **Wholesale sourcing** with flexible procurement policies
- **Compliant KYC verification** and vendor onboarding
- **Double-entry accounting** for financial reconciliation

The architecture is **event-driven** using NopCommerce's Publish/Subscribe pattern with **idempotent message processing** via an Outbox Pattern for guaranteed delivery.

---

## Architecture Overview

### Plugin Dependency Graph

```
┌─────────────────────────────────────────────────────────────┐
│              Marketplace.Business                           │
│  (KYC, Vendor Verification, Document Management)           │
└────────────────────┬────────────────────────────────────────┘
                     │
        ┌────────────┴────────────┐
        ▼                         ▼
┌──────────────────┐    ┌──────────────────┐
│ Marketplace.Core │    │ Marketplace.Wholesale
│                  │    │ (B2B Catalog, Pricing
└──────────────────┘    │  MOQ, Lead Times)
        │                └──────────────────┘
        │
┌───────┴──────────┬────────────────┬───────────────────┐
│                  ▼                ▼                   ▼
│        ┌─────────────────┐ ┌──────────────┐ ┌───────────────┐
│        │ Marketplace.    │ │ Marketplace. │ │ Marketplace.  │
│        │ Storefront      │ │ Dropship     │ │ Accounting    │
│        │ (URL Routing,   │ │ (Cart Split, │ │ (GL Accounts, │
│        │ Branding,       │ │ Fulfillment, │ │ Journal       │
│        │ Dynamic Theme)  │ │ Procurement) │ │ Entries)      │
│        └─────────────────┘ │              │ └───────────────┘
│                            └──────────────┘
│                                  │
│        ┌─────────────────────────┼─────────────────────┐
│        ▼                         ▼                     ▼
│   ┌─────────────┐          ┌──────────────┐    ┌───────────┐
│   │ Marketplace.│          │ Marketplace. │    │ Marketplace
│   │ Escrow      │          │ Wallet       │    │ Risk
│   │ (State FSM, │          │ (Balances,   │    │ (Reserve
│   │ Disputes)   │          │ Ledger,      │    │ Holds,
│   │             │          │ Withdrawals) │    │ Fraud)
│   └─────────────┘          └──────────────┘    └───────────┘
└───────────────────────────────────────────────────────────┘
```

**Core Dependencies:**
- All plugins depend on `Nop.Plugin.Marketplace.Core` for shared domain models and events
- Escrow → Wallet → Risk forms the financial settlement pipeline
- Dropship feeds order splits to Accounting & Escrow
- Business plugin gates access to other marketplace features

---

## Plugin Deep Dive

### 1. **Nop.Plugin.Marketplace.Core** (Display Order: 1)
**Purpose:** Foundation layer providing shared data models, event definitions, and outbox pattern infrastructure.

#### Key Components:

**Domain Models:**
- `MarketplaceBusiness` - Legal entity record mapping to native `VendorId`
  - Fields: `LegalName`, `TaxId`, `VerificationStatusId`, `RoleTypeId`
  - Links to KYC document verification

- `ResellerProductMapping` - Links cloned products to originals
  - Maps `ResellerCoreProductId` → `SupplierCoreProductId`
  - Stores `SelectedProcurementPolicyId` and `MarginPercentage`
  - Enables inventory sync from supplier

- `ResellerProcurementSettlementPolicy` [Flags Enum]
  - `FullEscrow` (1) - Money held until delivery
  - `ResellerPrepay` (2) - Reseller funds upfront
  - `RollingReserve` (4) - From vendor's platform wallet
  - `CreditLimit` (8) - B2B trade terms

**Enumerations:**
- `BusinessVerificationStatus`: Pending → Approved → Rejected/Suspended
- `MarketplaceRoleType`: Supplier, Reseller, Both
- `EscrowState` (13 states):
  ```
  Created (10) → Funded (30) → Processing (50) → Shipped (70) 
  → Delivered (90) → GracePeriod (110) → SettlementPending (130) 
  → Settled (150) [Terminal]

  Dispute Path: Disputed (170) → Refunded (190) [Terminal]
  Cancel Path: Cancelled (210) [Terminal]
  ```

**Event Definitions:**
- `SettlementRequestedEvent` - Triggers wallet crediting
- `WalletSettledEvent` - Triggers risk management hold calculation
- `ProductClonedEvent` - Fires when reseller imports supplier product
- `RiskEvents`: ReserveHoldRequestedEvent, ReserveReleasedEvent, ChargebackDeductedEvent
- `SupplierStockChangedEvent` - For inventory sync triggers

**Supporting Entities:**
- `OutboxMessage` - Implements Outbox Pattern for reliable event publishing
  - Prevents duplicate processing via EventType + idempotency keys

- `EscrowStateHistory` - Audit trail of all state transitions with admin notes

- `LedgerEntryType` [Enum]: Credit, Debit

---

### 2. **Nop.Plugin.Marketplace.Business** (Display Order: 2)
**Purpose:** Vendor onboarding, KYC verification, compliance, and marketplace governance.

#### Key Components:

**Domain Models:**
- `BusinessDocument`
  - Stores DocumentType: "TaxId", "BusinessLicense", etc.
  - FileUri points to MinIO object storage
  - Audit: UploadedOnUtc timestamp

- `MarketplaceBusinessSettings` (Configuration)
  - MinIO S3-compatible object storage for KYC docs
  - Configurable KYC bucket name
  - SSL toggle for development environments

**Services:**

1. **IMarketplaceBusinessService**
   ```csharp
   Task SubmitKycAsync(vendorId, legalName, taxId, docStream, docName, mimeType)
   Task ApproveBusinessAsync(marketplaceBusinessId)
   ```

2. **IMarketplaceDocumentService** - Manages document upload/retrieval

3. **IMarketplaceCatalogService** - Controls catalog visibility by verification status

**Permission Model:**
- Custom `MarketplacePermissionProvider` for role-based access
- Restricts marketplace features until KYC approval

**User Workflows:**

**Vendor Onboarding:**
```
1. Vendor clicks "Join Marketplace" (if MultiVendor enabled)
2. Native nopCommerce Vendor record created (VendorId)
3. Vendor fills KYC form:
   - Legal Name, Tax ID
   - Upload business documents (PDF/JPG) to MinIO
4. System creates MarketplaceBusiness record (Pending)
5. Admin reviews documents in MarketplaceKyc admin panel
6. Admin clicks Approve → VerificationStatusId = Approved
7. Vendor can now:
   - List products (if Supplier role)
   - Browse wholesale catalog (if Reseller role)
   - Set up storefront (if Reseller role)
```

**Onboarding Technical Flow:**
- `MarketplaceKycController.Submit()` → Validates vendor
- Uploads doc stream to MinIO via `MarketplaceBusinessSettings`
- Writes `BusinessDocument` record to DB
- Sends notification email to admin
- Admin dashboard shows pending approvals
- `MarketplaceAdminMenuConsumer` adds admin menu item

---

### 3. **Nop.Plugin.Marketplace.Wholesale** (Display Order: 4)
**Purpose:** B2B wholesale sourcing, wholesale pricing rules, and supplier product catalog management.

#### Key Components:

**Domain Model:**
- `SupplierProduct`
  ```csharp
  ProductId          // Native nopCommerce product
  VendorId           // Supplier vendor
  AllowedProcurementPolicies  // Flags: which policies this supplier allows
  WholesalePrice     // B2B cost to reseller
  MinimumOrderQuantity // MOQ enforcement
  IsDropshipEnabled  // Can be ordered as dropship
  IsPreorderEnabled  // Allow pre-orders
  LeadTimeDays       // Manufacturing/sourcing lead time
  ```

**Services:**

1. **ISupplierProductService**
   - `GetByProductIdAsync(productId)` - Fetch B2B rules
   - `InsertSupplierProductAsync()` / `UpdateSupplierProductAsync()`
   - `SearchB2BProductsAsync(excludeVendorId, pageIndex, pageSize)` - For B2B catalog browser

**Workflows:**

**Supplier Setup (Wholesale Enablement):**
```
1. Supplier (approved vendor) creates product in native catalog
2. Supplier navigates to "Wholesale Settings" tab
3. Sets: WholesalePrice, MOQ, IsDropshipEnabled flag
4. Specifies AllowedProcurementPolicies (which payment terms to accept)
5. System creates SupplierProduct record
6. Product now appears in reseller B2B catalog with label "Available for Dropship"
```

**Reseller Sourcing (Light Cloning):**
```
1. Reseller user clicks "Browse B2B Catalog"
2. System queries SupplierProduct records (excludes own vendor)
3. Reseller finds supplier's product, clicks "Import to My Catalog"
4. Modal: "Set your margin %", "Pick procurement policy"
5. Reseller sets MarginPercentage = 25% (will sell at wholesale + 25%)
6. Reseller selects ProcurementPolicy (e.g., ResellerPrepay)
7. System executes "Light Clone":
   - Creates new Product record under reseller's VendorId (native nopCommerce)
   - Copies: Name, Description, Images, Category
   - Sets parent reference to supplier's product ID
   - Creates ResellerProductMapping record
   - Fires ProductClonedEvent (may trigger accounting entry for tracking)
8. Product available in reseller's storefront at inflated price
```

**Controllers:**
- `SupplierWholesaleController` - Supplier manages B2B product settings
- `B2BSourcingController` - Reseller browses & imports products
- `MySourcedCatalogController` - Reseller views imported products

---

### 4. **Nop.Plugin.Marketplace.Storefront** (Display Order: 3)
**Purpose:** Dynamic URL routing, domain mapping, and white-label reseller storefronts.

#### Key Components:

**Domain Model:**
- `ResellerStorefront`
  ```csharp
  VendorId           // Reseller's vendor ID
  UrlSlug            // e.g., "shoeking" → /store/shoeking
  CustomDomain       // e.g., www.shoeking.com (future)
  StoreName          // Display name
  PrimaryColorHex    // #FF0000 for CSS theming
  LogoPictureId      // Reference to Picture table
  BannerPictureId    // Reference to Picture table
  IsActive           // Visibility toggle
  ```

**Services:**

1. **IStorefrontContext** (Scoped per HTTP request)
   ```csharp
   Task<ResellerStorefront> GetCurrentStorefrontAsync()
   ```
   - **Tier 1 Cache:** Memory during request lifecycle
   - **Tier 2 Cache:** Redis/distributed cache
   - **Resolver Logic:**
     - Checks URL path for `/store/{slug}` pattern
     - Extracts slug (e.g., "shoeking")
     - Queries DB: ResellerStorefront where UrlSlug == slug && IsActive
     - Returns null if not found (renders main marketplace)

**Components & Views:**
- `StorefrontBrandingViewComponent` - Injects logo/colors into layout
- `StorefrontIndexModel` - Homepage template
- Dynamic CSS generation using `PrimaryColorHex`

**Technical Architecture:**

**Route Provider:**
- `StorefrontRouteProvider` implements `IRouteProvider`
- Registers wildcard route: `store/{slug}/...`
- Routes /store/shoeking → StorefrontController actions

**Workflow - Reseller Storefront Setup:**
```
1. Reseller (after KYC approval) navigates to "My Storefront"
2. Form: UrlSlug, StoreName, PrimaryColorHex
3. Upload logo & banner images
4. Toggle IsActive checkbox
5. System creates ResellerStorefront record
6. StorefrontCacheEventConsumer clears Redis cache
7. Reseller shares link: "yourdomain.com/store/shoeking"
8. End consumer visits link:
   a. StorefrontContext.GetCurrentStorefrontAsync() resolves "shoeking"
   b. Layout renders with reseller's logo + primary color
   c. Product listing filtered to reseller's VendorId
```

**Consumer Workflow - Shopping at Reseller Storefront:**
```
1. Customer visits yourdomain.com/store/shoeking
2. Page loads → StorefrontContext queries DB for UrlSlug="shoeking"
3. Layout renders with branding (logo, color)
4. Customer browses reseller's products (native product list filtered by VendorId)
5. Adds items to cart (cart items tagged with reseller VendorId)
6. Checkout → Order created with mixed-vendor items
```

---

### 5. **Nop.Plugin.Marketplace.Dropship** (Display Order: 5)
**Purpose:** Mixed-cart order routing, supplier fulfillment requests, and procurement policy enforcement.

#### Key Components:

**Domain Model:**
- `DropshipFulfillment` (Supplier Fulfillment Ticket)
  ```csharp
  OrderId            // Native B2C order
  OrderItemId        // Specific item from order
  ResellerVendorId   // Who listed the product
  SupplierVendorId   // Who manufactures/stocks it
  ProcurementPolicyId // Which payment terms apply

  LockedWholesalePrice  // What supplier gets (fixed at checkout)
  LockedRetailPrice     // What consumer paid (fixed at checkout)

  DropshipStatusId   // 10=Pending, 20=Accepted, 30=Shipped
  TrackingNumber     // Supplier fills in after shipping
  CreatedOnUtc       // When order placed
  ShippedOnUtc       // When supplier shipped
  ```

**Enumerations:**
- `DropshipStatus`
  - Pending (10) - Waiting for supplier to acknowledge
  - Accepted (20) - Supplier accepted order
  - Shipped (30) - Supplier shipped (has tracking)
  - Awaiting ResellerDeposit (15) - Special state if ResellerPrepay policy

**Services:**

1. **IDropshipFulfillmentService**
   ```csharp
   Task InsertFulfillmentAsync(DropshipFulfillment)
   Task<DropshipFulfillment> GetByIdAsync(id)
   Task UpdateFulfillmentAsync(DropshipFulfillment)
   Task<IPagedList<DropshipFulfillment>> SearchSupplierTicketsAsync(supplierVendorId, pageIndex, pageSize)
   ```

**Event Consumers:**

1. **OrderPlacedEventConsumer** (Core Event: `OrderPlacedEvent`)
   - **Trigger:** Native nopCommerce order created (B2C checkout)
   - **Logic:**
     ```
     FOR EACH OrderItem in Order:
       1. Get Product
       2. Find ResellerProductMapping (links to supplier product)
       3. Load SupplierProduct B2B settings
       4. IF product is dropship-enabled:
          - Load ResellerProductMapping → SelectedProcurementPolicyId
          - Create DropshipFulfillment record
          - Determine initial status based on procurement policy:
            - ResellerPrepay → AwaitingResellerDeposit (reseller must fund first)
            - FullEscrow/RollingReserve → Pending
          - Lock in wholesale & retail prices at checkout time
       5. ELSE if direct supplier:
          - Standard procurement (not dropship)
     ```
   - **Price Locking Strategy:**
     - `LockedWholesalePrice = SupplierProduct.WholesalePrice * Quantity`
     - `LockedRetailPrice = OrderItem.PriceExclTax`
     - **Rationale:** Protects reseller margin from supplier price changes post-order
     - **Commission Split:** 
       - Reseller margin = LockedRetailPrice - LockedWholesalePrice
       - Supplier revenue = LockedWholesalePrice

**Controllers:**

1. **SupplierDropshipController**
   - `SearchTickets(pageIndex)` - List all pending fulfillments for supplier
   - `AcceptTicket(fulfillmentId)` - Mark as Accepted
   - `ShipTicket(fulfillmentId, trackingNumber)` - Mark as Shipped + record tracking

**Admin/Supplier UI Workflow - Fulfill Dropship Order:**
```
1. Supplier logs in → Dashboard shows "Pending Fulfillment (5)"
2. Clicks "Fulfillment Queue"
3. Table shows:
   - Order ID, Items, Price Locked As, Status
4. Supplier reviews DropshipFulfillment record:
   - Consumer name, address, item details
   - LockedWholesalePrice (supplier's revenue)
   - LockedRetailPrice (what consumer paid)
5. Supplier clicks "Accept"
   - Status → Accepted
   - If ResellerPrepay: system checks reseller deposited funds with Wallet
6. Supplier picks/packs items
7. Supplier inputs tracking number, clicks "Ship"
   - Status → Shipped
   - ShippedOnUtc recorded
   - TrackingNumber stored
   - OrderItem marked as "Awaiting Delivery"
8. Event triggered? (Consumer/reseller notified of tracking)
```

**Delivery Confirmation Workflow:**
```
1. Courier delivery system triggers "Shipment Delivered" in nopCommerce
2. System fires ShipmentDeliveredEvent
3. Escrow consumer listens → updates EscrowTransaction to "Delivered"
4. Grace period starts (72 hours for disputes)
5. After grace period: automatically transitions to "SettlementPending"
6. Escrow releases funds to wallets (via SettlementRequestedEvent)
```

---

### 6. **Nop.Plugin.Marketplace.Escrow** (Display Order: 5)
**Purpose:** Order state management, hold funds until delivery confirmation, handle disputes, and trigger settlements.

#### Key Components:

**Domain Models:**

1. **EscrowTransaction** (Per-order financial lock)
   ```csharp
   CoreOrderId     // Native order ID
   SupplierVendorId
   ResellerVendorId
   CurrentStateId  // References EscrowState enum
   UpdatedOnUtc    // Last state change
   ```

2. **EscrowStateHistory** (Immutable audit trail)
   ```csharp
   EscrowTransactionId
   OldStateId
   NewStateId
   SystemNote      // "Payment confirmed", "Delivery confirmed"
   AdminUserId     // If admin forced state change
   CreatedOnUtc
   ```

**Services:**

1. **IEscrowService**
   ```csharp
   Task TransitionStateByOrderIdAsync(coreOrderId, newState, systemNote, adminUserId?)
   Task ReleaseFundsAsync(escrowTransactionId, adminUserId?)
   Task DisputeEscrowAsync(escrowTransactionId, reason, adminUserId)
   Task MarkAsSettledAsync(escrowTransactionId)
   ```

2. **CommissionService**
   - `CalculateSplitsAsync(coreOrderId)` → `CommissionSplitResult`
   - Hardcoded logic (can be externalized):
     ```
     Gateway Fee = 1.5% of Total
     Platform Fee = 5% of Total
     Net to Vendors = Total - Platform Fee
     Supplier Cut = 75% of Net
     Reseller Cut = 25% of Net
     (Rounding handled by assigning difference to reseller)
     ```

**Event Consumers:**

1. **OrderPaidEventConsumer**
   - **Trigger:** `OrderPaidEvent` (payment cleared)
   - **Action:** Create EscrowTransaction record, transition to "Funded"

2. **ShipmentDeliveredEventConsumer**
   - **Trigger:** `ShipmentDeliveredEvent` (courier confirmed delivery)
   - **Action:** Transition to "Delivered" → 72-hour grace period → "SettlementPending"

3. **WalletSettledEventConsumer**
   - **Trigger:** `WalletSettledEvent` (wallet confirmed credits)
   - **Action:** Transition to "Settled"

**State Machine Logic:**

```
Successful Order Path:
  Created (10)
    ↓ [OrderPaidEvent]
  Funded (30)
    ↓ [Supplier ships]
  Processing (50)
    ↓ [Supplier uploads tracking]
  Shipped (70)
    ↓ [Courier confirms delivery]
  Delivered (90)
    ↓ [72-hour grace period]
  GracePeriod (110)
    ↓ [Auto-transition or explicit release]
  SettlementPending (130)
    ↓ [Wallet confirms credits]
  Settled (150) ← Terminal: Funds disbursed

Dispute Path:
  [Any pre-settled state]
    ↓ [Admin/Escrow raises dispute]
  Disputed (170)
    ↓ [Admin investigation & resolution]
  Refunded (190) ← Terminal: Money returned to consumer

Cancellation:
  [Pre-shipping]
    ↓ [Order cancelled]
  Cancelled (210) ← Terminal
```

**Admin Workflow - Dispute Resolution:**
```
1. Consumer reports issue (item not as described)
2. Click "Raise Dispute" in order details
3. Reason: "Item defective", attached photo
4. Escrow transitions: Delivered → Disputed
5. Admin dashboard flags disputed escrows
6. Admin reviews evidence (consumer, supplier messages)
7. Admin decision: "Refund to consumer" OR "Approve release to supplier"
   - Refund: transition to Refunded → Wallet debits supplier, credits consumer
   - Release: transition to SettlementPending → Normal settlement flow
```

**Accounting Integration:**

- `OrderPaidAccountingConsumer` listens to `OrderPaidEvent`
  - Records journal entry: Dr. Escrow Liability, Cr. Revenue Recognition

- `SettlementAccountingConsumer` listens to `SettlementRequestedEvent`
  - Records commission split as double-entry:
    - Dr. Supplier Receivable, Cr. Escrow Liability
    - Dr. Reseller Receivable, Cr. Escrow Liability
    - Dr. Platform Income, Cr. Escrow Liability

---

### 7. **Nop.Plugin.Marketplace.Accounting** (Display Order: 1)
**Purpose:** Chart of accounts, double-entry bookkeeping, financial reconciliation.

#### Key Components:

**Domain Models:**

1. **GlAccount** (Chart of Accounts)
   ```csharp
   AccountCode    // "1001" (Cash), "2001" (Escrow Liability)
   Name           // "Cash", "Escrow Liability"
   AccountTypeId  // Asset, Liability, Equity, Revenue, Expense
   IsActive       // Toggle for archival
   ```

2. **JournalEntry** (Transaction header)
   ```csharp
   Reference      // "ESC_123" linking to escrow TX ID
   IdempotencyKey // Prevents duplicate entries
   CreatedOnUtc
   // Contains multiple JournalEntryLines
   ```

3. **JournalEntryLine** (Debit/Credit pair)
   ```csharp
   JournalEntryId
   GlAccountId
   DebitAmount    // ≥0
   CreditAmount   // ≥0
   Description
   ```

**Services:**

1. **IAccountingService**
   ```csharp
   Task<GlAccount> GetAccountByCodeAsync(accountCode)
   Task RecordTransactionAsync(JournalEntry header, IEnumerable<JournalEntryLine> lines)
   ```

**Double-Entry Validation:**
- **Golden Rule:** Sum of Debits MUST equal Sum of Credits
- Throws exception if imbalanced
- Idempotency check prevents duplicate posting (via `IdempotencyKey`)

**Sample Journal Entries:**

**Entry 1: Order Paid (Payment Received)**
```
Reference: ESC_123
IdempotencyKey: ORD_123_PAID

Lines:
  Dr. Escrow Holding Account (2001)  $100.00
  Cr. Cash/Revenue                             $100.00
```

**Entry 2: Settlement Release (Funds Distributed)**
```
Reference: ESC_123_SETTLE
IdempotencyKey: ESC_123_SETTLE_001

Lines:
  Dr. Supplier Payable (2002)    $75.00
  Dr. Reseller Payable (2003)    $20.00
  Dr. Platform Revenue (4001)     $5.00
  Cr. Escrow Holding Account              $100.00
```

**Event Consumers:**

1. **OrderPaidAccountingConsumer**
   - Listens: `OrderPaidEvent`
   - Action: Record cash receipt, escrow liability

2. **SettlementAccountingConsumer**
   - Listens: `SettlementRequestedEvent`
   - Action: Record vendor payables, platform revenue

3. **RiskAccountingConsumers**
   - Listens: `ReserveHoldRequestedEvent`, `ChargebackDeductedEvent`
   - Action: Record reserve liability, fraud loss

**Report Generation (Future):**
- Generate trial balance (verify debits = credits)
- Vendor revenue reports
- Platform fee tracking
- Chargeback loss tracking

---

### 8. **Nop.Plugin.Marketplace.Wallet** (Display Order: 5)
**Purpose:** Vendor balance management, payment settlement, and withdrawal requests.

#### Key Components:

**Domain Models:**

1. **WalletAccount** (Per-vendor account state)
   ```csharp
   VendorId                // Links to Vendor
   AvailableBalance        // Ready to withdraw
   PendingBalance          // Awaiting final settlement
   ReserveBalance          // Held for chargeback protection
   ConcurrencyVersion      // Optimistic locking counter
   ```

2. **WalletLedger** (Immutable transaction log)
   ```csharp
   WalletAccountId
   EntryTypeId             // Credit or Debit
   Amount
   ReferenceType           // "Settlement", "Withdrawal", "Chargeback"
   ReferenceId             // FK to settlement/chargeback record
   IdempotencyKey          // Prevents duplicate credits
   Notes
   CreatedOnUtc
   ```

3. **WithdrawalRequest** (Cash-out request)
   ```csharp
   VendorId
   Amount
   StatusId                // Requested, Pending, Processed, Failed
   RequestedOnUtc
   ProcessedOnUtc
   FailureReason
   ```

4. **WithdrawalStatus** [Enum]
   - Requested (10)
   - Pending (20)
   - Processed (30)
   - Failed (40)

**Services:**

1. **IWalletTransactionService**
   ```csharp
   Task ProcessSettlementRequestAsync(SettlementRequestedEvent)
   Task<int> RequestWithdrawalAsync(vendorId, amount)
   Task ApproveWithdrawalAsync(withdrawalRequestId)
   Task ReleaseReserveAsync(vendorId, amount)
   ```

**Workflow - Settlement Processing:**

```
1. Event: SettlementRequestedEvent published by Escrow
   {
     SupplierVendorId: 5,
     SupplierAmount: $75.00,
     ResellerVendorId: 10,
     ResellerAmount: $20.00,
     PlatformFeeAmount: $5.00,
     IdempotencyKey: "ESC_123_001"
   }

2. WalletTransactionService.ProcessSettlementRequestAsync():
   - Check: IdempotencyKey already exists?
     IF yes → return (prevent double credit)
   - Transaction scope: Serializable isolation level

3. Credit Supplier (5):
   - Load WalletAccount(5)
   - Insert WalletLedger: Credit $75.00
   - Update WalletAccount: AvailableBalance += $75
   - Increment ConcurrencyVersion

4. Credit Reseller (10):
   - Load WalletAccount(10)
   - Insert WalletLedger: Credit $20.00
   - Update WalletAccount: AvailableBalance += $20
   - Increment ConcurrencyVersion

5. Transaction committed
   - Both vendors now have updated balances

6. Publish: WalletSettledEvent
   {
     EscrowTransactionId: 123,
     IdempotencyKey: "ESC_123_001"
   }
   - Consumed by Risk plugin to calculate reserve holds
```

**Concurrency Protection:**
- **Serializable Isolation:** Creates database-level locks
- **ConcurrencyVersion:** Prevents race condition where 2 requests read same balance

**Withdrawal Workflow:**

```
1. Vendor: "Withdraw $500"
   - Check AvailableBalance >= $500?
   - Create WithdrawalRequest: Status = Requested

2. System: RequestWithdrawalAsync()
   - Load WalletAccount (Serializable lock)
   - AvailableBalance -= $500
   - PendingBalance += $500
   - Update WalletAccount
   - Debit ledger: $500

3. Admin: Reviews pending withdrawals
   - Can approve (funds disbursed to bank account)
   - Can reject (funds returned to AvailableBalance)

4. Approve:
   - With external payment gateway:
     - Charge vendor's registered bank account (or process ACH)
   - Update WithdrawalRequest: Status = Processed
   - PendingBalance -= $500
```

**Controllers:**

1. **VendorWalletController** (Vendor-facing)
   - `Dashboard()` - Shows balances
   - `RequestWithdrawal()` - Vendor requests cash-out
   - `WithdrawalHistory()` - View past withdrawals

**Models:**
- `WalletDashboardModel` - AvailableBalance, PendingBalance, ReserveBalance visualization

---

### 9. **Nop.Plugin.Marketplace.Risk** (Display Order: 5)
**Purpose:** Financial risk mitigation via rolling reserves and chargeback management.

#### Key Components:

**Domain Models:**

1. **VendorReserveRule** (Hold policy)
   ```csharp
   VendorId        // 0 = global default
   HoldPercentage  // e.g., 20 (%)
   HoldDays        // e.g., 45 days
   ```
   - Example: "Hold 20% of earnings for 45 days"

2. **ReserveSchedule** (Individual hold record)
   ```csharp
   VendorId
   EscrowTransactionId  // Links to order
   HeldAmount           // e.g., $100 (20% of $500 earnings)
   ReleaseOnUtc         // Timestamp when hold expires
   IsReleased           // Whether released
   CreatedOnUtc
   ```

3. **ChargebackCase** (Fraud/dispute record)
   ```csharp
   CoreOrderId
   VendorId
   DisputeAmount    // Amount deducted from wallet
   Reason           // "Customer dispute", "Fraud"
   CreatedOnUtc
   ```

**Services:**
- Reserve hold calculation & scheduling
- Chargeback tracking
- Fraud case management

**Event Consumers:**

1. **WalletSettledRiskConsumer**
   - **Trigger:** `WalletSettledEvent` (funds credited to vendor wallet)
   - **Logic:**
     ```
     1. Fetch VendorReserveRule for vendor
        IF none → use global default
        IF still none → return (no hold)

     2. Calculate hold:
        HeldAmount = SettledAmount × (HoldPercentage / 100)

     3. Create ReserveSchedule:
        ReleaseOnUtc = NOW + HoldDays
        IsReleased = false

     4. Update WalletAccount:
        AvailableBalance -= HeldAmount
        ReserveBalance += HeldAmount

     5. Debit WalletLedger:
        EntryType = "Hold"
        Amount = HeldAmount
        ReferenceType = "ReserveSchedule"
     ```

2. **Scheduled Task: ReserveReleaseTask**
   - **Trigger:** Every 4 hours (configurable)
   - **Logic:**
     ```
     1. Query ReserveSchedules where:
        - IsReleased = false
        - ReleaseOnUtc <= NOW

     2. For each mature schedule:
        - Set IsReleased = true
        - Create ReserveReleasedEvent
        - Write to OutboxMessage (async event)

     3. ReserveReleasedEvent consumed by Wallet:
        - Find WalletAccount
        - AvailableBalance += ReleaseAmount
        - ReserveBalance -= ReleaseAmount
        - Credit ledger: "Hold Released"
     ```

**Chargeback Handling:**

```
Scenario: Consumer disputes order (chargeback from payment processor)

1. Admin notified of chargeback
2. Admin navigates to "Marketplace → Chargebacks"
3. Clicks "Record Chargeback" for order
   - Reason: e.g., "Fraud - unauthorized"
   - Amount: e.g., $250
4. System:
   - Creates ChargebackCase record
   - Finds corresponding Escrow (settled/released)
   - Publishes ChargebackDeductedEvent
   - Consumed by Wallet:
     - Find vendor's WalletAccount
     - AvailableBalance -= ChargebackAmount
     - Debit ledger: "Chargeback Loss"
   - Creates accounting entry:
     - Dr. Chargeback Loss (expense)
     - Cr. Vendor Payable
5. Vendor balance reduced (may go negative)
6. Platform takes loss OR pursues vendor for payment
```

**Risk Management Dashboard (Admin):**
- Vendor reserve rules CRUD
- Active reserve schedules (upcoming releases)
- Chargeback case tracking
- Risk metrics (chargeback rate by vendor)

---

## Cross-Cutting Patterns & Architecture

### Event-Driven Architecture

**Event Flow Diagram:**
```
OrderPlaced (Native) 
  ↓
[OrderPlacedEventConsumer - Dropship]
  ↓
DropshipFulfillment Created
  ↓
OrderPaid (Native)
  ↓
[OrderPaidEventConsumer - Escrow]
  ↓
EscrowTransaction.State = Funded
Outbox: [OrderPaidEvent → Accounting]
  ↓
ShipmentDelivered (Native)
  ↓
[ShipmentDeliveredEventConsumer - Escrow]
  ↓
EscrowTransaction.State = Delivered
  ↓
[Grace Period 72 hours]
  ↓
EscrowTransaction.State = SettlementPending
Outbox: [SettlementRequestedEvent]
  ↓
[SettlementAccountingConsumer]
  ↓
Journal entries recorded
  ↓
[EscrowReleasedEventConsumer - Wallet]
  ↓
ProcessSettlementRequestAsync()
  ↓
Credit Supplier Wallet + Reseller Wallet
Publish: WalletSettledEvent
  ↓
[WalletSettledRiskConsumer]
  ↓
Create ReserveSchedule (hold for 45 days)
  ↓
[ReserveReleaseTask - Scheduled]
  ↓
Release mature reserves
```

### Outbox Pattern (Reliability)

**Purpose:** Guarantee event delivery even if publisher crashes.

**Implementation:**
- Every significant state change writes to `OutboxMessage` table
- OutboxMessage contains:
  - EventType (fully qualified class name)
  - Payload (JSON serialized)
  - CreatedOnUtc
  - IsProcessed flag

**Consumption:**
```csharp
// Startup.cs
services.AddScoped<OutboxMessageProcessorTask>();
// Runs every 5 minutes
// Deserializes OutboxMessage.Payload
// Publishes via IEventPublisher
// Marks IsProcessed = true
```

**Idempotency:**
- Every settlement event includes `IdempotencyKey`
- Example: `"ESC_123_SETTLE_001"`
- Consumers check: "Have I seen this key before?"
- Prevents double-crediting if event consumed twice

### Database Transaction Safety

**Critical Paths Use Serializable Isolation:**
```csharp
// Wallet balance update
using var scope = new TransactionScope(
    TransactionScopeOption.Required,
    new TransactionOptions { IsolationLevel = IsolationLevel.Serializable },
    TransactionScopeAsyncFlowOption.Enabled);

// DB locks entire WalletAccount row
// No concurrent updates possible
await _accountRepository.UpdateAsync(account);
scope.Complete();
```

**Justification:**
- Prevents balance race conditions
- Cost: Slight performance impact (row-level locks)
- Benefit: Financial integrity

---

## Data Flow Scenarios

### Scenario 1: End-to-End Order (Supplier → Reseller → Consumer)

**Setup Phase (Pre-Order):**
1. **Supplier** creates product, enables wholesale via `SupplierProduct`
   - WholesalePrice = $50, MOQ = 10, IsDropshipEnabled = true
2. **Reseller** imports product (Light Clone)
   - Creates cloned product at $50 + 25% margin = $62.50 retail
   - Creates `ResellerProductMapping(SupplierProductId, ResellerProductId, Margin=25%)`
3. **Reseller** activates storefront
   - Creates `ResellerStorefront` with UrlSlug = "shoeking"

**Order Phase:**
1. **Consumer** visits yourdomain.com/store/shoeking
   - StorefrontContext resolves slug, injects branding
2. **Consumer** adds 2x cloned product to cart = $125 (2 × $62.50)
3. **Consumer** checkout:
   - Order Total = $125
   - Items tagged: ResellerVendorId=Reseller, SupplierVendorId=Supplier
4. **Order Placed Event:**
   - `OrderPlacedEventConsumer` processes order
   - Creates `DropshipFulfillment`:
     - LockedRetailPrice = $125 (what consumer paid)
     - LockedWholesalePrice = $100 (2 × $50)
     - Status = Pending
     - DropshipStatusId = Pending

**Payment Phase:**
1. **Payment clears** (Card processed)
2. **OrderPaidEvent** fired
3. **Escrow creates** `EscrowTransaction`:
   - State = Funded
   - Escrow amount = $125 held
4. **Accounting records:**
   - Dr. Escrow Holding / Cr. Cash
5. **Outbox written** with event

**Fulfillment Phase:**
1. **Supplier receives** fulfillment notification
2. **Supplier logs in**, finds DropshipFulfillment in queue
3. **Supplier ships**, records tracking number
   - DropshipFulfillment.TrackingNumber = "UPS123..."
   - Status = Shipped
4. **Consumer receives** tracking update

**Delivery Phase:**
1. **Courier confirms delivery**
   - ShipmentDeliveredEvent fired in nopCommerce
2. **EscrowTransaction.State** → Delivered
3. **Grace period:** 72 hours for disputes
4. **Auto-transition (if no dispute):**
   - State → SettlementPending
5. **SettlementRequestedEvent published:**
   ```json
   {
     "EscrowTransactionId": 123,
     "SupplierVendorId": 2,
     "SupplierAmount": 75.00,
     "ResellerVendorId": 5,
     "ResellerAmount": 50.00,
     "PlatformFeeAmount": 0,
     "IdempotencyKey": "ESC_123_SETTLE"
   }
   ```

**Settlement Phase:**
1. **Wallet processes settlement:**
   - Credit Supplier wallet: +$75
   - Credit Reseller wallet: +$50
   - Publish: WalletSettledEvent
2. **Accounting records journal entry:**
   - Dr. Supplier Payable $75 / Cr. Escrow
   - Dr. Reseller Payable $50 / Cr. Escrow
3. **Risk consumer processes reserve hold:**
   - Get VendorReserveRule (Reseller): "Hold 20% for 45 days"
   - Calculate: 20% × $50 = $10
   - Move $10 from AvailableBalance → ReserveBalance
   - Create ReserveSchedule: ReleaseOnUtc = NOW + 45 days
4. **Vendor balances:**
   - Supplier: AvailableBalance +$75 (unreserved)
   - Reseller: AvailableBalance +$40, ReserveBalance +$10

**Withdrawal Phase:**
1. **Supplier requests withdrawal:** $75
2. **System processes:**
   - AvailableBalance -= $75 → PendingBalance += $75
   - Create WithdrawalRequest: Status = Requested
3. **Admin approves**
   - Deduct from PendingBalance
   - Submit to payment processor (ACH/Wire)
   - Mark WithdrawalRequest: Status = Processed

---

### Scenario 2: Dispute & Chargeback

**Dispute Raised:**
1. **Consumer reports:** "Item arrived damaged"
2. **Order disputed** before auto-release
   - EscrowTransaction.State → Disputed
   - Funds held pending admin review

**Admin Resolution:**
1. **Admin reviews** photos, supplier response
2. **Admin decision:** "Refund to consumer"
3. **System:**
   - Update EscrowTransaction.State → Refunded
   - Create reversal `SettlementRequestedEvent` with negative amounts
   - Wallet debits supplier, credits consumer refund

**Chargeback (Later):**
1. **Consumer disputes credit card transaction** with bank
2. **Bank initiates chargeback** → $125 reversal
3. **Admin records chargeback:**
   - Create ChargebackCase: Amount = $125, Reason = "Fraud"
4. **System:**
   - Find vendor (Supplier): Deduct from balance
   - Or if order already settled: Create liability
   - Journal entry: Dr. Chargeback Loss / Cr. Vendor Payable

---

### Scenario 3: Reseller Prepay Policy

**Setup:**
- Supplier requires: "ResellerPrepay" policy
- Reseller imports product but selects ResellerPrepay

**Order:**
1. Consumer buys at reseller, checkout completes
2. OrderPlacedEventConsumer creates DropshipFulfillment:
   - **Status = AwaitingResellerDeposit** (not Pending)
   - Does NOT auto-notify supplier yet
3. System sends notification to Reseller:
   - "You have pending fulfillment requiring prepayment"
   - Wholesale cost: $100

**Reseller Action:**
1. Reseller logs into portal
2. Finds pending fulfillment
3. Clicks "Deposit & Approve"
4. System checks: Does reseller have $100 in AvailableBalance?
   - IF no → "Insufficient funds"
   - IF yes → Debit wallet:
     - AvailableBalance -= $100
     - PendingWholesalePayment += $100 (hold)
     - Create WalletLedger: Debit $100, Reference="PrepayDeposit"
5. Update DropshipFulfillment.Status → Pending
6. Notify supplier: "Fulfillment approved, reseller prepaid"

**Supplier Fulfillment:**
- Process as normal (ship, track, deliver)
- Upon delivery & settlement:
  - Escrow releases $75 (supplier revenue)
  - Reseller's prepaid $100 applied:
    - $75 goes to supplier
    - $25 credited back to reseller AvailableBalance

---

## User Roles & Permissions

### Customer (B2C Shopper)
- Browse main storefront & reseller storefronts
- Add to cart, checkout
- View order status
- Raise disputes/returns

### Supplier Vendor
- Create products in native catalog
- Configure wholesale settings (SupplierProduct)
- View B2B sourcing requests from resellers
- Access Dropship fulfillment queue
  - Accept/reject orders
  - Ship with tracking
- View settlement & wallet balance
- Request withdrawals

### Reseller Vendor
- Complete KYC verification
- Browse B2B catalog
- Import products (Light Clone)
- Create storefront (Marketplace.Storefront)
- View customer orders from storefront
- Access procurement settings per imported product
- View wallet balance & historical settlements
- Manage withdrawals

### Admin
- Approve/reject vendor KYC applications
- Monitor all orders, disputes, chargebacks
- Force state transitions (escalation)
- Configure marketplace policies:
  - Reserve rules (hold % & days)
  - Commission split %
  - Procurement policy defaults
- View financial reports (GL, trial balance)
- Manage chargebacks & risk cases
- View audit logs (EscrowStateHistory)

---

## Database Schema Summary

### Core Tables (Marketplace.Core)

| Table | Purpose |
|-------|---------|
| `MarketplaceBusiness` | Vendor verification & roles |
| `ResellerProductMapping` | Cloned product tracking |
| `OutboxMessage` | Event sourcing/reliability |
| `EscrowState` | Enumeration (immutable) |

### Business Tables (Marketplace.Business)

| Table | Purpose |
|-------|---------|
| `BusinessDocument` | KYC documents |

### Wholesale Tables (Marketplace.Wholesale)

| Table | Purpose |
|-------|---------|
| `SupplierProduct` | B2B product settings |

### Storefront Tables (Marketplace.Storefront)

| Table | Purpose |
|-------|---------|
| `ResellerStorefront` | Reseller branding & routing |

### Dropship Tables (Marketplace.Dropship)

| Table | Purpose |
|-------|---------|
| `DropshipFulfillment` | Supplier tickets |

### Escrow Tables (Marketplace.Escrow)

| Table | Purpose |
|-------|---------|
| `EscrowTransaction` | Order hold & state |
| `EscrowStateHistory` | Audit trail |

### Accounting Tables (Marketplace.Accounting)

| Table | Purpose |
|-------|---------|
| `GlAccount` | Chart of accounts |
| `JournalEntry` | Transaction header |
| `JournalEntryLine` | Debit/credit pairs |

### Wallet Tables (Marketplace.Wallet)

| Table | Purpose |
|-------|---------|
| `WalletAccount` | Vendor balance state |
| `WalletLedger` | Transaction history |
| `WithdrawalRequest` | Cash-out requests |

### Risk Tables (Marketplace.Risk)

| Table | Purpose |
|-------|---------|
| `VendorReserveRule` | Hold policy settings |
| `ReserveSchedule` | Individual hold record |
| `ChargebackCase` | Fraud tracking |

---

## Integration Points & APIs

### Native nopCommerce Integration

1. **Vendor Management**
   - Uses native `Vendor` entity
   - Extends via `MarketplaceBusiness` (1:1 mapping via VendorId)

2. **Product Management**
   - Uses native `Product` entity
   - Light Clone: New Product records, same catalog structure
   - Extends via `SupplierProduct` (B2B rules)
   - Extends via `ResellerProductMapping` (clone tracking)

3. **Order Processing**
   - Uses native `Order` and `OrderItem`
   - Listens to native events: OrderPlacedEvent, OrderPaidEvent, ShipmentDeliveredEvent

4. **Payment Processing**
   - Native payment gateway integration
   - Marketplace holds funds in Escrow (logical hold, not payment gateway freeze)

5. **Customer Roles**
   - Native customer role system
   - Optional: Extend to "Supplier", "Reseller" roles

### REST API Considerations (Future)

Could expose:
- `/api/marketplace/storefront/{slug}` - Get storefront details
- `/api/marketplace/products/wholesale` - B2B catalog
- `/api/marketplace/orders/{orderId}/fulfillment` - Dropship status
- `/api/marketplace/wallet/balance` - Vendor balance
- `/api/marketplace/escrow/{escrowId}/disputes` - Dispute management

---

## Performance & Scalability Considerations

### Caching Strategy

1. **StorefrontContext (Request-scoped cache)**
   - Tier 1: In-memory for request lifecycle
   - Tier 2: Redis for frequently accessed storefronts

2. **Product Wholesale Rules**
   - Cache SupplierProduct lookups by ProductId
   - Invalidate on update

3. **Vendor Reserve Rules**
   - Cache by VendorId
   - Global default cached separately

### Database Optimization

1. **Indexes:**
   - `EscrowTransaction.CoreOrderId` (frequent lookups)
   - `DropshipFulfillment.SupplierVendorId` (supplier queue)
   - `WalletLedger.WalletAccountId` (ledger queries)
   - `ResellerProductMapping.ResellerCoreProductId` (clone lookups)

2. **Partitioning (Enterprise):**
   - Outbox by month (archive old processed messages)
   - EscrowStateHistory by year
   - WalletLedger by vendor (sharding)

### Asynchronous Processing

- Event consumption happens async (background service)
- OutboxMessageProcessorTask runs every 5 minutes
- Doesn't block checkout/order flow

---

## Security Considerations

### Data Isolation

1. **Vendor Data Visibility**
   - Supplier sees only their fulfillment queue
   - Reseller sees only their storefront orders
   - Admin sees all data

2. **KYC Document Security**
   - Stored in MinIO (S3-compatible) with TLS
   - Only accessible to admin & KYC approver
   - Encrypted at rest (MinIO configuration)

3. **Financial Data**
   - Wallet/Ledger accessible only to vendor owner & admin
   - Commission calculations immutable (audit trail)

### Input Validation

1. **Procurement Policy Selection**
   - Must be in SupplierProduct.AllowedProcurementPolicies flags
   - Prevents unauthorized policy selection

2. **Reserve Hold Percentages**
   - Validated range: 0-100%
   - Hold days > 0

3. **Amount Validations**
   - No negative withdrawals
   - Withdrawal ≤ AvailableBalance

### Audit Trail

- EscrowStateHistory logs every transition with admin user
- WalletLedger records every balance change
- JournalEntry ensures all money movements are double-booked
- OutboxMessage prevents silent failures

---

## Known Limitations & Future Enhancements

### Current Limitations

1. **Commission Splits (Hardcoded)**
   - CommissionService has fixed percentages (1.5%, 5%, 75%/25%)
   - Should be externalized to admin settings table

2. **Custom Domains for Storefronts**
   - StorefrontContext only handles `/store/{slug}` pattern
   - Custom domain support (ResellerStorefront.CustomDomain) not yet implemented
   - Requires DNS resolver logic + CORS handling

3. **Multi-Currency**
   - System assumes single currency
   - Wallet, Escrow, GL all in one currency
   - No exchange rate handling for international suppliers

4. **Scheduled Task Frequency**
   - ReserveReleaseTask runs every 4 hours
   - Could use SLA-based scheduling for compliance

5. **Reserve Hold Calculation**
   - Calculates hold per individual order
   - No aggregate hold caps (e.g., don't hold more than 50% of total balance)

### Recommended Enhancements

1. **Webhook Integration**
   - Allow external systems to listen to settlement events
   - Custom business logic hooks

2. **Chargeback Prediction (ML)**
   - Flag orders with chargeback risk
   - Increase reserve hold automatically

3. **Dynamic Commission Rules**
   - Tiered commissions based on vendor volume
   - Promotional commission reductions

4. **Dispute Resolution SLA**
   - Auto-escalate disputes after 7 days
   - Track resolution time metrics

5. **Tax Calculation**
   - Integrate with TaxJar/Avalara for B2B VAT
   - Record tax-exempt status per vendor

6. **Batch Settlements**
   - Weekly/daily batch processing instead of per-order
   - Reduce transaction overhead

7. **Refund Processing**
   - Automatic refund to consumer payment method
   - Tracking of refund status

8. **Multi-Warehouse Support**
   - Link products to physical warehouses
   - Route orders to nearest warehouse

9. **Real-time Notifications**
   - WebSocket updates for fulfillment queue
   - Real-time balance changes

10. **Vendor Performance Scoring**
    - Track quality metrics (chargeback rate, dispute rate, on-time delivery)
    - Display badges/ratings

---

## Implementation Roadmap Recommendations

### Phase 1: Foundation (Current State)
✅ Core models & event architecture  
✅ Business/KYC onboarding  
✅ Wholesale sourcing & light cloning  
✅ Dropship fulfillment routing  
✅ Escrow state machine  
✅ Wallet balance management  
✅ Accounting double-entry  
✅ Risk reserve holds  

### Phase 2: Marketplace Storefront (Next)
⚠️ Custom domain support  
⚠️ Advanced branding (custom CSS)  
⚠️ Reseller analytics dashboard  
⚠️ Multi-language support  

### Phase 3: Financial Management
⚠️ Commission rule engine (externalize from CommissionService)  
⚠️ Automated reconciliation  
⚠️ Tax compliance (VAT, withholding)  
⚠️ Payment method integration  

### Phase 4: Advanced Risk
⚠️ Chargeback prediction model  
⚠️ Fraud detection rules engine  
⚠️ Dynamic reserve hold adjustment  
⚠️ Vendor scoring & rating system  

### Phase 5: Scale & Performance
⚠️ Database sharding strategy  
⚠️ Message queue (RabbitMQ/ServiceBus)  
⚠️ Real-time notifications  
⚠️ Global CDN for storefront assets  

---

## Glossary of Terms

| Term | Definition |
|------|-----------|
| **Escrow** | Funds held by marketplace until delivery confirmed |
| **Light Clone** | Reseller's product copy without inventory management |
| **Dropship** | Supplier ships directly to end consumer |
| **Procurement Policy** | Payment terms for reseller to source from supplier |
| **Reserve Hold** | Percentage of earnings held for chargeback protection |
| **Idempotency Key** | Unique ID preventing duplicate processing |
| **Outbox Pattern** | Event sourcing via database table for reliability |
| **Double-Entry** | Every transaction has debit & credit pair (accounting) |
| **GL Account** | General Ledger account (Chart of Accounts) |
| **Journal Entry** | Transaction header containing GL postings |
| **Wallet** | Vendor's balance account (Available, Pending, Reserve) |
| **Withdrawal** | Cash-out request from vendor wallet |

---

## Conclusion

The nopCommerce Marketplace system is a sophisticated, enterprise-grade B2B/B2C platform built on proven architectural patterns (Event-Driven, Outbox, Double-Entry Accounting). It provides:

- **Vendor Ecosystem:** Hierarchical (Supplier → Reseller → Consumer)
- **Financial Controls:** Escrow, Accounting, Risk Management
- **Operational Efficiency:** Automated fulfillment, settlement, reserve management
- **Compliance & Audit:** KYC, double-entry accounting, state history
- **Flexibility:** Pluggable procurement policies, configurable hold rules

The system is production-ready for marketplace launch and scales to enterprise volumes with proper infrastructure investment (caching, database sharding, async message queues).

---

**Report Prepared For:** AI Development Assistants & Integration Partners  
**Recommended Review:** System architects, DevOps engineers, financial compliance officers  
**Last Updated:** January 2025
