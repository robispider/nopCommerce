# DELIVERABLE 4: Order & Inventory Specification

---

## ORDER OWNERSHIP & LIFECYCLE MODEL

```
┌──────────────────────────────────────────────────────────────┐
│                    NATIVE B2C ORDER                           │
│                  (nopCommerce Order)                          │
│                                                               │
│  Properties:                                                 │
│  ├─ OrderId (PK)                                             │
│  ├─ CustomerId                                               │
│  ├─ OrderTotal = $100                                        │
│  ├─ Items:                                                   │
│  │  ├─ Item A: Qty=1, Price=$60 (from Reseller #5)           │
│  │  └─ Item B: Qty=2, Price=$20 each (from Supplier #2)      │
│  └─ Status: [New → Processing → Complete → Shipped]          │
│                                                               │
│  [Event: OrderPlacedEvent] → Marketplace Layer               │
│                                                               │
└──────────────────────────────────────────────────────────────┘
         ↓ (OrderPlacedEventConsumer)
         ↓
┌──────────────────────────────────────────────────────────────┐
│              MARKETPLACE ORDER GROUP                          │
│       (Marketplace-specific meta-wrapper)                     │
│                                                               │
│  MarketplaceOrderGroup                                       │
│  ├─ Id = 1001                                                │
│  ├─ NativeOrderId = 456 (FK→Order)                           │
│  ├─ TotalAmount = $100                                       │
│  ├─ Status: Created → Allocated → Fulfilling → Completed     │
│  │                                                            │
│  └─ Allocations:                                             │
│                                                              │
│     ALLOCATION #1 (Reseller-Sourced)                         │
│     ├─ VendorId = 5 (Reseller "ShoeKing")                    │
│     ├─ AllocatedAmount = $60                                 │
│     ├─ FulfillmentMethod = Dropship                          │
│     │  └─ Reseller sources from Supplier #2                  │
│     │                                                         │
│     │  DropshipFulfillment (Ticket)                          │
│     │  ├─ SupplierVendorId = 2 (Manufacturer)                │
│     │  ├─ ResellerVendorId = 5 (Reseller)                    │
│     │  ├─ LockedWholesalePrice = $40 (supplier cost)         │
│     │  ├─ LockedRetailPrice = $60 (consumer paid)            │
│     │  ├─ Margin = $20 (reseller keeps)                      │
│     │  ├─ Status: Pending → Accepted → Shipped → Delivered   │
│     │  └─ [Financial Impact]                                 │
│     │     ├─ Supplier: +$40 (on delivery)                    │
│     │     ├─ Reseller: +$20 (margin, subject to hold)        │
│     │     └─ Platform: Commission (% of $100 or $60?)        │
│     │                                                         │
│     └─ Status: Pending → Confirmed → Fulfilling → Delivered  │
│                                                               │
│     ALLOCATION #2 (Direct Supplier)                          │
│     ├─ VendorId = 2 (Supplier "ManufactureCorp")            │
│     ├─ AllocatedAmount = $40 (2 × $20)                       │
│     ├─ FulfillmentMethod = StandardShipping                  │
│     │  └─ Supplier ships directly                            │
│     │                                                         │
│     │  DropshipFulfillment (Ticket)                          │
│     │  ├─ SupplierVendorId = 2                               │
│     │  ├─ ResellerVendorId = null (direct)                   │
│     │  ├─ LockedWholesalePrice = $40                         │
│     │  ├─ LockedRetailPrice = $40                            │
│     │  └─ [Financial Impact]                                 │
│     │     ├─ Supplier: +$40 (on delivery)                    │
│     │     └─ Platform: Commission on $40                     │
│     │                                                         │
│     └─ Status: Pending → Confirmed → Fulfilling → Delivered  │
│                                                               │
└──────────────────────────────────────────────────────────────┘
         ↓
┌──────────────────────────────────────────────────────────────┐
│                    ESCROW TRANSACTION                         │
│              (Financial Hold for all allocations)             │
│                                                               │
│  EscrowTransaction                                           │
│  ├─ MarketplaceOrderGroupId = 1001                           │
│  ├─ Amount = $100 (total order value)                        │
│  ├─ State: Created → Funded → Processing → Delivered → Settled
│  │                                                            │
│  └─ [At Settlement]                                          │
│     ├─ Escrow releases funds                                 │
│     ├─ SettlementRequestedEvent published                    │
│     ├─ Wallet credits both vendors (async)                   │
│     ├─ Risk holds % of earnings (async)                      │
│     └─ GL records all (async)                                │
│                                                               │
└──────────────────────────────────────────────────────────────┘
```

**Key Insight:** One native order → One marketplace order group → Multiple allocations (per vendor) → One escrow (covers all).

---

## INVENTORY OWNERSHIP MODEL

**Question:** "Who owns stock?"

**Answer:** Depends on sourcing strategy.

```
┌─────────────────────────────────────────────────────────────┐
│            INVENTORY BUCKET ARCHITECTURE                     │
│  (Multi-source stock tracking per product)                  │
└─────────────────────────────────────────────────────────────┘

PRODUCT: "Running Shoes (Size 10)"
└─ ProductId = 999

InventoryBucket #1: SUPPLIER STOCK
├─ SourceVendorId = 2 (Manufacturer)
├─ BucketType = SupplierStock
├─ AvailableQuantity = 100
├─ ReservedQuantity = 15 (for reseller orders)
├─ BackorderQuantity = 10 (pending restock)
└─ [This is THE source of truth for supplier]
   └─ When reseller imports product, they view this qty
   └─ Reseller can set MarginPercentage & Procurement Policy

InventoryBucket #2: RESELLER INVENTORY
├─ SourceVendorId = 5 (Reseller "ShoeKing")
├─ BucketType = ResellerInventory
├─ AvailableQuantity = 30 (synced from supplier)
├─ ReservedQuantity = 8
├─ BackorderQuantity = 0
└─ [Reseller's view of stock]
   └─ If SyncInventory=true: Updates when supplier qty changes
   └─ If SyncInventory=false: Manual sync or out-of-sync

InventoryBucket #3: PLATFORM COMMON STOCK
├─ SourceVendorId = null (Platform-owned)
├─ BucketType = PlatformCommon
├─ AvailableQuantity = 50
├─ ReservedQuantity = 5
├─ BackorderQuantity = 0
└─ [Platform bulk buys from supplier]
   └─ Any vendor can fulfill from this pool
   └─ Used when supplier stock low

ALLOCATION PRIORITY (When customer orders):

Order for 2 × "Running Shoes (Size 10)"
├─ Check InventoryBucket allocation rule (defined by Reseller)
│
├─ Option A: "Use my reseller inventory first"
│  └─ Allocate 2 from Bucket #2 (Reseller)
│  └─ If insufficient: Fallback to Supplier
│
├─ Option B: "Supplier direct (dropship)"
│  └─ Allocate 2 from Bucket #1 (Supplier)
│  └─ Reseller's bucket untouched
│
└─ Option C: "Platform pool first"
   └─ Allocate 2 from Bucket #3 (Platform)
   └─ If insufficient: Allocate from Supplier

RESERVATION FLOW:

StockReservation (Immutable Record)
├─ InventoryBucketId = Bucket#1 (Supplier)
├─ OrderItemId = OrderItem#456 (customer's item)
├─ QuantityReserved = 2
├─ ExpiresOnUtc = NOW + 15min (hold duration)
├─ Status: Active
└─ [Prevents double-selling]

When OrderPlacedEvent fires:
├─ For each item, create StockReservation
├─ InventoryBucket.ReservedQuantity += 2 (unavailable for other orders)
├─ InventoryBucket.AvailableQuantity -= 2 (but record still exists)

If order payment fails or cancelled:
├─ Delete StockReservation
├─ InventoryBucket.ReservedQuantity -= 2
├─ InventoryBucket.AvailableQuantity += 2 (back in stock)

If order confirmed (payment clears):
├─ Update StockReservation.Status = Reserved (confirmed)
├─ ExpiresOnUtc = null (no longer temporary)

On fulfillment (ticket shipped):
├─ InventoryBucket.AvailableQuantity -= 2 (truly deducted)
├─ InventoryBucket.ReservedQuantity -= 2 (no longer reserved)
```

---

## INVENTORY OWNERSHIP POLICIES

**Policy 1: Supplier-Owned (Default)**
```
Supplier controls all stock.
├─ Supplier updates quantity in SupplierProduct
├─ Reseller sees read-only view (cannot edit)
├─ Reseller can only enable/disable sales
├─ Advantage: Simplicity, no sync complexity
├─ Disadvantage: Reseller cannot differentiate
└─ Example: Alibaba B2B marketplace
```

**Policy 2: Reseller Consignment**
```
Reseller purchases inventory upfront.
├─ Supplier ships items to Reseller warehouse
├─ InventoryBucket #2 tracks Reseller's physical stock
├─ Reseller owns goods (accounting liability)
├─ Reseller sets independent pricing
├─ Advantage: Reseller controls margin & availability
├─ Disadvantage: Inventory carrying cost on reseller
└─ Example: Shopify dropship model
```

**Policy 3: Hybrid (Mixed)**
```
Reseller has standing inventory + dropship backup.
├─ Reseller maintains safety stock (Policy #2)
├─ If Reseller stock low: Automatic dropship from supplier
├─ InventoryBucket #2 (Reseller) checked first
├─ InventoryBucket #1 (Supplier) fallback
├─ Advantage: Best customer experience
├─ Disadvantage: Complex orchestration
└─ Example: Modern B2C marketplaces
```

**Implementation Rule:**
```
For each ResellerProductMapping:
├─ Load AllocationRule (default or custom)
├─ AllocationRule specifies priority:
│  ├─ "ResellerFirst" (Bucket#2, then Bucket#1)
│  ├─ "SupplierDirect" (Bucket#1 only)
│  ├─ "PlatformPoolFirst" (Bucket#3, then Bucket#1)
│  └─ Custom sequence
├─ Query buckets in order
├─ Reserve from first available bucket with sufficient qty
└─ Create StockReservation with InventoryBucketId
```

---

## OVERSELL & BACKORDER RULES

**Scenario 1: True Oversell (Customer expects delivery, stock runs out)**

```
Reseller lists "Running Shoes": qty = 30 (InventoryBucket.AvailableQuantity)
Customer A orders: qty = 20
Customer B orders: qty = 15 (total requested = 35, available = 30)

Without Oversell Rules:
├─ Customer A reservation: ✓ (20 reserved, 10 left)
├─ Customer B: ✗ (insufficient stock, order fails)
└─ Risk: Lost sales

With Oversell Rules (AllowOversell=true):
├─ Customer A reservation: ✓ (20 reserved, 10 left)
├─ Customer B reservation: ✓ (15 reserved, -5 backorder)
├─ InventoryBucket.BackorderQuantity = 5
├─ On fulfillment:
│  ├─ Customer A: Shipped immediately (qty available)
│  ├─ Customer B: Backorder created (qty on hold for restock)
│  └─ When supplier restocks: BackorderQuantity decrements
└─ Risk: Customer B waits longer, but no lost sale
```

**Backorder Lifecycle:**

```
BackorderRequest
├─ CustomerId
├─ ProductId
├─ QuantityBackordered = 5
├─ Status: Active
├─ CreatedOnUtc = 2024-01-15
├─ DesiredDeliveryDate = 2024-02-15 (customer expectation)
│
Supplier restocks (SupplierStockChangedEvent):
├─ SupplierProduct qty += 10 (now 10 available beyond reservations)
├─ System queries active BackorderRequest for this product
├─ Fulfills oldest first (FIFO)
├─ BackorderRequest #1: Fulfill qty=5 (out of 10 available)
├─ Creates new DropshipFulfillment for backorder item
├─ Customer notified: "Your backorder is now being shipped"
└─ Status: Fulfilled
```

**Business Rule:**

```
BackorderPolicy
├─ MaxBackorderDays: 45 (auto-cancel if not fulfilled)
├─ AllowBackorderNotification: true (email when available)
├─ BackorderPriority: "FIFO" or "VIP_FIRST" (customer tier)
└─ AllowPartialFulfillment: true (ship qty available, backorder rest)
```

---

## INVENTORY SYNC CONFLICTS

**Problem:** Supplier qty changes while reseller has inventory. Who wins?

```
Day 1: Reseller imports "Running Shoes" (qty = 100)
├─ InventoryBucket #1 (Supplier): 100 available
├─ InventoryBucket #2 (Reseller): 100 available (synced)
└─ ResellerProductMapping.SyncInventory = true

Day 2: Supplier deletes product (qty = 0)
├─ SupplierStockChangedEvent published
├─ System checks: ResellerProductMapping.SyncInventory = true
├─ Reseller still has 100 in their bucket
│
├─ Option A: "Hard Sync" (Supplier wins)
│  ├─ InventoryBucket #2 (Reseller): 0 (hard-set)
│  ├─ Reseller's listings immediately unpublished
│  └─ Risk: Angry reseller, broken orders
│
├─ Option B: "Soft Sync" (Current inventory preserved)
│  ├─ InventoryBucket #2 (Reseller): 100 (preserved)
│  ├─ InventoryBucket #1 (Supplier): 0
│  ├─ New orders fail (check supplier bucket on fulfillment)
│  ├─ Existing reservations honored (soft-reserved)
│  └─ Reseller can fulfill from existing stock
│
└─ Recommendation: Option B (Soft Sync)
   ├─ Publish InventorySyncConflictDetectedEvent
   ├─ Admin notified to review
   ├─ Reseller notified to restock
   └─ Orders continue (fulfill from reseller bucket)
```

**Implementation:**

```
SyncPolicy per ResellerProductMapping:
├─ HardSync (Supplier qty is source of truth, always override)
├─ SoftSync (Preserve reseller qty, warn on conflict)
└─ ManualSync (Reseller controls when to update)

When SupplierStockChangedEvent fires:
├─ Load ResellerProductMapping(SupplierProductId)
├─ If SyncPolicy = HardSync:
│  └─ Update InventoryBucket#2 = SupplierProduct.Qty
├─ If SyncPolicy = SoftSync:
│  ├─ Load InventoryBucket#2.AvailableQuantity
│  ├─ If different from SupplierProduct.Qty:
│  │  └─ Publish InventorySyncConflictDetectedEvent
│  ├─ Preserve InventoryBucket#2 (reseller keeps their qty)
│  └─ For new orders: Check InventoryBucket#1 first (supplier)
├─ If SyncPolicy = ManualSync:
│  └─ Do nothing (reseller will update manually)
└─ AdminNotification: "Stock sync conflict: Review & resolve"
```

---

## MISSING INVENTORY WORKFLOWS

1. **Return Processing**
   - Customer returns item
   - Inventory restored to which bucket?
   - Refund processed to wallet
   - Reseller reputation affected

2. **Quality Control Rejections**
   - Reseller rejects supplier shipment (damaged)
   - Item re-allocated to different supplier
   - Financial reversal needed

3. **Inventory Audit**
   - Physical vs. system stock mismatch
   - Reconciliation process
   - Who bears loss?

4. **Bulk Import/Export**
   - Reseller imports 10K SKUs from supplier catalog
   - Performance considerations (batch processing)
   - Sync conflicts at scale

5. **Inventory Forecasting**
   - Predict stock-outs based on demand
   - Suggest reorder quantities to supplier/reseller
   - Integrate with preorder system

---

## PREORDER INVENTORY MODEL

```
Preorder Strategy:
├─ Product: "Upcoming Sneaker Release" (launches Feb 15)
├─ Current qty in SupplierProduct: 0 (not yet manufactured)
├─ IsPreorderEnabled = true
├─ PreorderStartDate = Jan 1, PreorderEndDate = Feb 15
│
Reseller Actions:
├─ Imports product with Margin 30%
├─ Product goes live (with "Preorder" badge)
├─ Customers can reserve at retail price ($150)

Customer Orders Preorder:
├─ Order placed, qty reserved but labeled "Preorder"
├─ EscrowTransaction created (funds held)
├─ InventoryBucket.BackorderQuantity += qty
├─ Customer gets "Preorder - Ships Feb 15" status

On PreorderReleaseDate (Feb 15):
├─ Supplier publishes actual stock available
├─ SupplierStockChangedEvent fired
├─ System fulfills backorders in FIFO order
├─ Excess preorders: Either cancelled or backorder created
├─ GL records: Revenue recognized on shipment (not order)

Accounting Treatment:
├─ On OrderPlaced: Dr. Cash / Cr. DeferredRevenue (liability)
├─ On Shipment: Dr. DeferredRevenue / Cr. Revenue (realized)
└─ On Cancellation: Dr. DeferredRevenue / Cr. Cash (refund)
```

---

## ALLOCATION CONFLICT RESOLUTION

**Problem:** Multiple allocation strategies defined. Order has mixed items. Who fulfills?

```
Example Order:
├─ Item A (SupplierProduct #1): Can use Dropship or ResellerInventory
├─ Item B (SupplierProduct #2): Dropship-only
└─ Reseller policy: "Reseller inventory first, then dropship"

Allocation Algorithm (Resolution Order):
1. Parse ResellerProductMapping for each item
2. Get Reseller's AllocationPolicy
3. Apply allocation rules:
   ├─ For Item A: Check Reseller bucket, then Supplier bucket
   ├─ For Item B: Use Supplier bucket only
4. Create StockReservations for each bucket used
5. Create MarketplaceOrderAllocations (one per unique fulfiller):
   ├─ Allocation #1: ResellerId=5, Items=[ItemA], Method=ResellInventory, Qty=1
   ├─ Allocation #2: SupplierVendorId=2, Items=[ItemA+ItemB], Method=Dropship, Qty=3
   └─ Total=4 items ✓

Creation of Fulfillment Tickets:
├─ DropshipFulfillment #1: ResellId=5, SupplierId=2, Items=1 (Item A via reseller)
├─ DropshipFulfillment #2: SupplierId=2, ResellerID=null, Items=2 (Item B direct)
└─ EscrowTransaction covers all 3 items
```

