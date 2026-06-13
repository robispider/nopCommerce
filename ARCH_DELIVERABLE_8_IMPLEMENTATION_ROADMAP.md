# DELIVERABLE 8: Implementation Roadmap

---

## PHASED DELIVERY STRATEGY (12-16 Weeks)

### PHASE 0: Foundation (Weeks 1-2) — Pre-MVP

**Objective:** Infrastructure, core plugin framework, event system.

**Deliverables:**
- [ ] PostgreSQL schema (all tables from Deliverable 2)
- [ ] Redis cluster (3-node, local dev + staging)
- [ ] RabbitMQ broker (local dev + staging)
- [ ] FluentMigrator plugin for Marketplace.Core
- [ ] OutboxMessage infrastructure
- [ ] IdempotencyKey utility library
- [ ] Event publisher/consumer base classes

**Scope:**
- Create Nop.Plugin.Marketplace.Core
  - Domain events (settled set of core events)
  - OutboxMessage entity & service
  - IdempotencyKey helpers
  - DI registration pattern
  - Unit test scaffolding

**Dependencies:** None (foundation layer)

**Success Criteria:**
- [ ] All schema migrations run cleanly
- [ ] OutboxMessage table persists events
- [ ] Event consumers can subscribe (no publishing yet)
- [ ] Tests: OutboxMessage lifecycle

**Risks:**
- Database schema conflicts with nopCommerce upgrades (mitigate: separate plugin schema)
- RabbitMQ not available in test environments (mitigate: in-memory fallback)

---

### PHASE 1: Business & Onboarding (Weeks 3-4)

**Objective:** Vendor registration, KYC, role assignment.

**Deliverables:**
- [ ] Nop.Plugin.Marketplace.Business plugin
- [ ] MarketplaceBusiness aggregate & repository
- [ ] BusinessDocument upload flow (MinIO integration)
- [ ] KYC verification workflow (Admin UI)
- [ ] Publish BusinessApprovedEvent
- [ ] Admin API for vendor approval

**Scope:**
- MarketplaceBusiness entity (aggregate root)
- BusinessDocument entity
- DocumentUploadService (virus scanning, storage)
- VerificationService (state machine)
- Admin controller: Approve/Reject vendor
- Integration: MinIO for document storage

**Dependencies:**
- Marketplace.Core (OutboxMessage, event system)

**Success Criteria:**
- [ ] Vendor can upload KYC documents
- [ ] Admin can approve/reject
- [ ] BusinessApprovedEvent fires on approval
- [ ] Documents stored in MinIO (not DB)
- [ ] Tests: State machine transitions, permission checks

**Risks:**
- MinIO setup delays (mitigate: mock in dev)
- Virus scanning performance (mitigate: async background job)

---

### PHASE 2: Wholesale & Product Catalog (Weeks 5-6)

**Objective:** Supplier product registration, B2B pricing.

**Deliverables:**
- [ ] Nop.Plugin.Marketplace.Wholesale plugin
- [ ] SupplierProduct aggregate
- [ ] Procurement policy flags
- [ ] Wholesale price management
- [ ] Product clone mapping (Reseller imports)
- [ ] Publish SupplierStockChangedEvent
- [ ] Supplier dashboard (view product catalog)

**Scope:**
- SupplierProduct entity (ProductId, WholesalePrice, MOQ, LeadTime)
- ResellerProductMapping (relation to cloned product)
- AllocationRule (dropship vs inventory vs hybrid)
- ProcurementPolicy flags (FullEscrow, ResellerPrepay, etc.)
- Dashboard: Suppliers list active products
- Dashboard: Resellers search & import products

**Dependencies:**
- Marketplace.Business (only approved vendors)
- Marketplace.Core

**Success Criteria:**
- [ ] Supplier can register product as wholesale
- [ ] Reseller can search & import product
- [ ] Clone product created in nopCommerce native Product table
- [ ] Margin configured at import time
- [ ] Tests: Product clone, allocation rule validation

**Risks:**
- Product clone complexity (mitigate: use service layer, not direct DB copy)

---

### PHASE 3: Inventory & Reservations (Weeks 7-8)

**Objective:** Multi-source stock tracking, reservation, allocation.

**Deliverables:**
- [ ] Nop.Plugin.Marketplace.Inventory plugin
- [ ] InventoryBucket aggregate (3 bucket types)
- [ ] StockReservation entity (15-min TTL)
- [ ] AllocationRuleService (resolve conflicts)
- [ ] Consume OrderPlacedEvent → Reserve stock
- [ ] Inventory sync (Supplier ↔ Reseller)
- [ ] Admin conflict resolution dashboard

**Scope:**
- InventoryBucket per product/vendor/type
- StockReservation creation on order
- Soft-sync vs Hard-sync policy per product
- Oversell allowance (configurable)
- Backorder tracking
- Scheduled task: Release expired reservations

**Dependencies:**
- Marketplace.Wholesale (SupplierProduct validation)
- Marketplace.Core

**Success Criteria:**
- [ ] Order triggers stock reservation
- [ ] Reserved qty unavailable for other orders
- [ ] Sync conflict detected & logged
- [ ] Expired reservation auto-released
- [ ] Tests: Oversell scenario, sync conflict handling

**Risks:**
- Concurrent reservation race conditions (mitigate: optimistic locking, retry)
- Sync logic complexity (mitigate: extensive scenario testing)

---

### PHASE 4: Order Management & Allocation (Weeks 9-10)

**Objective:** Order splitting, fulfillment routing.

**Deliverables:**
- [ ] Nop.Plugin.Marketplace.Order plugin
- [ ] MarketplaceOrderGroup aggregate
- [ ] MarketplaceOrderAllocation entity
- [ ] OrderSplittingService (route by vendor)
- [ ] Consume OrderPlacedEvent → Create group + allocations
- [ ] Publish OrderSplitCompletedEvent
- [ ] Dashboard: Vendor order view

**Scope:**
- 1:1 mapping: NativeOrder ↔ MarketplaceOrderGroup
- M:1 mapping: OrderAllocations → VendorId
- Allocation method: Dropship, LocalPickup, StandardShipping
- Financial impact immutable: LockedWholesalePrice, LockedRetailPrice
- Vendor dashboard: See all my allocations

**Dependencies:**
- Marketplace.Inventory (reservation must exist)
- Marketplace.Wholesale (product routing)
- Marketplace.Core

**Success Criteria:**
- [ ] Multi-vendor order split into allocations
- [ ] Each allocation routed to fulfillment owner
- [ ] Prices locked immutably
- [ ] Tests: Order with 3+ vendors, allocation routing

**Risks:**
- Complex splitting logic (mitigate: extensive unit tests, scenario matrix)

---

### PHASE 5: Fulfillment & Tickets (Weeks 11-12)

**Objective:** Supplier dropship tickets, status tracking.

**Deliverables:**
- [ ] Nop.Plugin.Marketplace.Fulfillment plugin
- [ ] DropshipFulfillment aggregate
- [ ] Ticket state machine (6 states)
- [ ] Consume OrderSplitCompletedEvent → Create tickets
- [ ] Supplier ticket dashboard (Accept/Reject/Ship)
- [ ] Tracking number input & validation
- [ ] Publish TicketShippedEvent, DeliveryConfirmedEvent

**Scope:**
- DropshipFulfillment entity (Supplier, Reseller, OrderItem level)
- States: Pending → Accepted → Shipped → Delivered
- Status page: Suppliers manage tickets
- Tracking integration: Carrier validation (optional v1)
- Deadline enforcement: Auto-escalate if not accepted in 24h

**Dependencies:**
- Marketplace.Order (allocations must exist)
- Marketplace.Core

**Success Criteria:**
- [ ] Supplier receives ticket notification
- [ ] Supplier accepts within 24h
- [ ] Tracking provided before shipment
- [ ] Delivery confirmed (manual or carrier API)
- [ ] Tests: State machine, deadline escalation

**Risks:**
- Carrier API unreliability (mitigate: optional, manual fallback in v1)

---

### PHASE 6: Escrow & Settlement (Weeks 13-14)

**Objective:** Financial hold, 13-state machine, settlement handshake.

**Deliverables:**
- [ ] Nop.Plugin.Marketplace.Escrow plugin
- [ ] EscrowTransaction aggregate (13 states)
- [ ] EscrowStateHistory (audit trail)
- [ ] DisputeCase entity (with evidence attachment)
- [ ] Consume OrderPaidEvent → Create escrow
- [ ] Consume DeliveryConfirmedEvent → Transition to GracePeriod
- [ ] Auto-settle after 72h grace period
- [ ] Publish SettlementReadyEvent
- [ ] Admin dispute dashboard

**Scope:**
- State machine (13 states per Deliverable 3)
- Automatic state transitions (via scheduled tasks)
- Dispute workflow: Evidence collection, admin review
- Grace period enforcement (72h)
- Two-phase settlement (Escrow ↔ Wallet)

**Dependencies:**
- Marketplace.Fulfillment (delivery must be confirmed)
- Marketplace.Core

**Success Criteria:**
- [ ] Escrow created & funded on order payment
- [ ] Delivered order auto-settles after 72h
- [ ] Dispute blocks settlement
- [ ] SettlementReadyEvent published reliably
- [ ] Tests: All 13 state paths, dispute resolution

**Risks:**
- Settlement deadlock (mitigate: idempotency + retry logic)
- Dispute timeout (mitigate: admin escalation task)

---

### PHASE 7: Wallet & Ledger (Weeks 15-16)

**Objective:** Vendor balance, settlement processing, withdrawals.

**Deliverables:**
- [ ] Nop.Plugin.Marketplace.Wallet plugin
- [ ] WalletAccount aggregate (tri-state balance)
- [ ] WalletLedger (immutable transaction log)
- [ ] WithdrawalRequest entity
- [ ] Consume SettlementReadyEvent → Credit wallet
- [ ] Settlement handshake (two-phase idempotent)
- [ ] Withdrawal approval workflow (FinanceOfficer)
- [ ] Publish WalletCreditedEvent, WithdrawalApprovedEvent
- [ ] Admin dashboard: Manage withdrawals
- [ ] Vendor dashboard: View balance, request withdrawal

**Scope:**
- Balance model: Available, Pending, Reserve
- Idempotent crediting (IdempotencyKey enforcement)
- Serializable transactions (ConcurrencyVersion lock)
- Withdrawal request → FinanceOfficer approval → Payment
- Full audit trail (every debit/credit logged)

**Dependencies:**
- Marketplace.Escrow (SettlementReadyEvent)
- Marketplace.Core

**Success Criteria:**
- [ ] Wallet created on vendor approval
- [ ] Settlement credits wallet atomically
- [ ] Vendor can request withdrawal
- [ ] Finance officer can approve/deny
- [ ] Tests: Settlement handshake, concurrent crediting, idempotency

**Risks:**
- Race conditions on balance updates (mitigate: serializable TX + ConcurrencyVersion)
- Settlement ordering (mitigate: queue-based processing)

---

### PHASE 8: Accounting & GL (Post-MVP, Weeks 17+)

**Objective:** Double-entry bookkeeping, financial reporting.

**Deliverables:**
- [ ] Nop.Plugin.Marketplace.Accounting plugin
- [ ] GlAccount aggregate (chart of accounts)
- [ ] JournalEntry aggregate (double-entry pairs)
- [ ] JournalEntryLine entity
- [ ] Consume all financial events → Post GL
- [ ] GL reconciliation task (nightly verification)
- [ ] Finance dashboard: Trial balance, reports
- [ ] Export GL to CSV/Excel

**Scope:**
- Chart of accounts (Assets, Liabilities, Equity, Revenue, Expense)
- Auto GL posting from SettlementReadyEvent, ChargebackDeductedEvent, etc.
- Validation: DR == CR before posting
- Audit trail: Every entry with IdempotencyKey + admin ID
- Nightly reconciliation (alert if DR != CR)

**Dependencies:**
- Marketplace.Wallet (settlements)
- Marketplace.Risk (chargebacks)
- Marketplace.Commission (platform revenue)
- Marketplace.Core

**Success Criteria:**
- [ ] GL entries automatically posted
- [ ] Trial balance daily verification
- [ ] No duplicate GL postings (IdempotencyKey enforced)
- [ ] Tests: GL reconciliation, double-entry validation

---

### PHASE 9: Risk & Reserves (Concurrent with Phase 8)

**Objective:** Vendor reserves, chargeback protection, risk scoring.

**Deliverables:**
- [ ] Nop.Plugin.Marketplace.Risk plugin
- [ ] VendorReserveRule aggregate
- [ ] ReserveSchedule entity (staggered holds)
- [ ] ChargebackCase entity
- [ ] Consume WalletCreditedEvent → Calculate hold
- [ ] Publish ReserveHoldCreatedEvent
- [ ] Consume ChargebackNotificationEvent → Record chargeback
- [ ] Auto-release schedule (via scheduled task)
- [ ] Admin dashboard: Reserve management

**Scope:**
- Default reserve rule: 20% × 45 days
- Dynamic adjustment by vendor risk score
- Staggered release (20% per week instead of lump sum)
- Chargeback tracking & deduction from available balance
- Vendor suspension on chargeback rate > threshold

**Dependencies:**
- Marketplace.Wallet (balance updates)
- Marketplace.Escrow (order-level tracking)
- Marketplace.Core

**Success Criteria:**
- [ ] Reserve hold applied on settlement
- [ ] Hold released on schedule
- [ ] Chargeback deducted from wallet
- [ ] Tests: Hold calculation, release scheduling, chargeback flow

---

### PHASE 10: Commission (Concurrent with Phase 9)

**Objective:** Commission rate engine, split calculation.

**Deliverables:**
- [ ] Nop.Plugin.Marketplace.Commission plugin
- [ ] CommissionRule aggregate (tiered rates)
- [ ] CommissionSplit entity (immutable calculation)
- [ ] Consume OrderPaidEvent → Calculate split
- [ ] Consume SettlementReadyEvent → Verify split
- [ ] Admin dashboard: Commission rule management
- [ ] Vendor dashboard: Commission transparency

**Scope:**
- Default rule: 5% platform commission
- Vendor-specific overrides
- Tiered rules (order threshold triggers rate change)
- Caps & minimums
- Commission split immutable (stored at order time)
- Audit: Every split traceable to rule version

**Dependencies:**
- Marketplace.Order (order context)
- Marketplace.Accounting (GL posting)
- Marketplace.Core

**Success Criteria:**
- [ ] Commission calculated on order
- [ ] Split recorded & immutable
- [ ] GL entries for platform revenue
- [ ] Tests: Tiered calculation, split validation

---

### PHASE 11: Storefront & Discovery (Post-MVP, Weeks 21+)

**Objective:** Dynamic URLs, reseller branding, product search.

**Deliverables:**
- [ ] Nop.Plugin.Marketplace.Storefront plugin
- [ ] ResellerStorefront aggregate (URL slug, branding)
- [ ] OpenSearch product indexing
- [ ] Storefront URL route provider
- [ ] Branded storefront rendering (Razor Pages)
- [ ] Product search (B2C + B2B scoped)
- [ ] Reseller dashboard: Storefront settings

**Scope:**
- One storefront per reseller
- Custom URL slug (e.g., /storefronts/shoeking/)
- Branding: Logo, colors, fonts
- Product listing filtered to reseller's products
- Search: Full-text, faceted (category, price, supplier)
- SEO: Sitemap, Open Graph, Schema markup

**Dependencies:**
- Marketplace.Business (reseller approval)
- Marketplace.Wholesale (product catalog)
- OpenSearch (indexing)
- Marketplace.Core

**Success Criteria:**
- [ ] Storefront created & URL slug unique
- [ ] Storefront indexable by search engines
- [ ] Product search returns results < 500ms
- [ ] Tests: URL routing, search relevance

---

### PHASE 12: Notification & API Integration (Post-MVP, Weeks 23+)

**Objective:** Multi-channel notifications, webhooks.

**Deliverables:**
- [ ] Nop.Plugin.Marketplace.Notification plugin
- [ ] NotificationTemplate entity
- [ ] Email sender (SMTP)
- [ ] Webhook infrastructure (signed deliveries)
- [ ] Vendor preferences (opt-in/out)
- [ ] Nop.Plugin.Marketplace.ApiIntegration plugin
- [ ] Rate limiting, retry policies

**Scope:**
- Email notifications: Order created, shipped, delivered, dispute, settlement
- SMS (optional): Critical alerts
- Webhook: External ERP/tax systems
- Vendor controls: Opt-in to email/SMS/webhook
- Admin controls: Template management

**Dependencies:**
- All contexts (event subscribers)
- Marketplace.Core

**Success Criteria:**
- [ ] Vendor receives email on order
- [ ] Webhook retries on failure
- [ ] Vendor preferences respected
- [ ] Tests: Email templates, webhook retry logic

---

## DEPENDENCY GRAPH

```
Phase 0 (Foundation)
    ↓
Phase 1 (Business)
    ↓ BusinessApprovedEvent
Phase 2 (Wholesale) ← Phase 1
    ↓ SupplierStockChangedEvent
Phase 3 (Inventory) ← Phase 2
    ↓ StockReservedEvent
Phase 4 (Order) ← Phase 3
    ↓ OrderSplitCompletedEvent
Phase 5 (Fulfillment) ← Phase 4
    ↓ DeliveryConfirmedEvent
Phase 6 (Escrow) ← Phase 5
    ↓ SettlementReadyEvent
Phase 7 (Wallet) ← Phase 6
    ↓ WalletCreditedEvent
Phase 8 (Accounting) ← Phase 7 + Phase 9 + Phase 10
Phase 9 (Risk) ← Phase 7
Phase 10 (Commission) ← Phase 4 + Phase 8
Phase 11 (Storefront) ← Phase 1 + Phase 2
Phase 12 (Notification + API) ← All phases
```

---

## CRITICAL SUCCESS FACTORS

| Factor | Metric | Target |
|--------|--------|--------|
| **Schema Correctness** | Zero constraint violations | 100% clean migrations |
| **Idempotency** | Duplicate event test | Pass all scenarios |
| **Settlement Integrity** | GL reconciliation daily | Zero discrepancies |
| **Performance** | P95 latency | < 1 sec (API), < 500ms (search) |
| **Availability** | Uptime SLA | 99.9% |
| **Security** | OWASP Top 10 pass | Zero critical findings |
| **Compliance** | Audit trail completeness | 100% transactions logged |

---

## RISKS & MITIGATION

| Risk | Severity | Mitigation |
|------|----------|-----------|
| **Schema conflicts with nopCommerce upgrades** | High | Separate plugin schema, version-aware migrations |
| **Settlement deadlock (Escrow ↔ Wallet race)** | Critical | IdempotencyKey + Serializable TX + exponential backoff |
| **Inventory sync divergence** | High | Soft-sync policy, event-driven reconciliation, admin dashboard |
| **GL reconciliation drift** | High | Nightly verification task, flag discrepancies, revert corrupt entries |
| **Concurrent wallet updates** | High | ConcurrencyVersion + optimistic locking |
| **Message queue overload** | Medium | RabbitMQ cluster scaling, dead-letter queue monitoring |
| **KYC document virus** | Medium | ClamAV scanning, quarantine malicious files |
| **Chargeback surge** | Medium | Reserve hold scaling, vendor suspension threshold |

---

## MATURITY SCORECARD (Post-MVP Checklist)

- [ ] **Phase 0-7 Complete** (Core MVP: Order → Settlement → Wallet)
- [ ] **Escrow Lifecycle Verified** (All 13 states tested, no skipped paths)
- [ ] **Settlement Idempotency Validated** (Duplicate event = no double-credit)
- [ ] **GL Reconciliation Passing** (7-day trial balance verification, zero drift)
- [ ] **Performance Targets Met** (P95 < 1s, search < 500ms)
- [ ] **Security Audit Passed** (OWASP Top 10 assessment)
- [ ] **Load Testing 10K Concurrent Users** (No degradation)
- [ ] **Disaster Recovery Drill** (Failover tested, RTO/RPO met)
- [ ] **Operational Runbooks Complete** (Incident response, scaling guide)
- [ ] **Vendor Documentation Ready** (API docs, dashboard guides)
- [ ] **Phase 8-12 Complete** (Accounting, Risk, Commission, Storefront, Notifications)

