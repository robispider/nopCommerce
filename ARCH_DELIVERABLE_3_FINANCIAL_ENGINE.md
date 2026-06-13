# DELIVERABLE 3: Financial Engine Specification

---

## ESCROW STATE MACHINE (13 States)

```
┌─────────────────────────────────────────────────────────────────┐
│                        SUCCESS PATH                              │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  Created (10)                                                   │
│  └─ [OrderPlacedEvent] Payment pending                          │
│                                                                  │
│  Funded (30)                                                    │
│  └─ [OrderPaidEvent] Funds locked in escrow                     │
│  └─ [Supplier accepts ticket] Processing begins                 │
│                                                                  │
│  Processing (50)                                                │
│  └─ Supplier packing/preparing                                  │
│                                                                  │
│  Shipped (70)                                                   │
│  └─ [TicketShippedEvent] Tracking provided                      │
│  └─ Customer in-transit notification                            │
│                                                                  │
│  Delivered (90)                                                 │
│  └─ [DeliveryConfirmedEvent] Courier confirmed delivery         │
│  └─ Start 72-hour grace period                                  │
│                                                                  │
│  GracePeriod (110)                                              │
│  └─ Consumer can raise dispute                                  │
│  └─ Auto-transition to SettlementPending if no dispute          │
│                                                                  │
│  SettlementPending (130) [CRITICAL HANDSHAKE POINT]             │
│  └─ [PublishSettlementRequestedEvent] Escrow → Wallet           │
│  └─ Wallet processes: Lock funds, Credit both vendors           │
│  └─ Wallet: [PublishWalletSettledEvent]                         │
│  └─ Escrow transitions to Settled                               │
│                                                                  │
│  Settled (150) [TERMINAL - FUNDS DISBURSED]                     │
│  └─ Vendors can withdraw funds                                  │
│  └─ Commission recorded in GL                                   │
│  └─ No further state changes possible                           │
│                                                                  │
├─────────────────────────────────────────────────────────────────┤
│                        DISPUTE PATH                              │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  [From any pre-settled state]                                   │
│  └─ Consumer or Admin raises dispute                            │
│                                                                  │
│  Disputed (170)                                                 │
│  └─ [EscrowDisputedEvent] Admin review initiated                │
│  └─ Evidence collected (photos, messages)                       │
│  └─ Admin makes decision (within SLA, e.g., 7 days)            │
│                                                                  │
│  Decision 1: Approve Release                                    │
│  └─ Continue to SettlementPending (normal flow)                 │
│                                                                  │
│  Decision 2: Authorize Refund                                   │
│  └─ Refunded (190) [TERMINAL]                                   │
│  └─ [EscrowRefundedEvent]                                       │
│  └─ Wallet: Debit supplier, Credit consumer                     │
│  └─ Supplier loses sale                                         │
│  └─ Record chargeback loss in GL                                │
│                                                                  │
├─────────────────────────────────────────────────────────────────┤
│                      CANCELLATION PATH                           │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  [Before payment or before fulfillment]                         │
│  └─ Order cancelled (customer or admin)                         │
│                                                                  │
│  Cancelled (210) [TERMINAL]                                     │
│  └─ [EscrowCancelledEvent]                                      │
│  └─ If already Funded: Refund to customer payment method        │
│  └─ No wallet transfers (nothing to settle)                     │
│  └─ Inventory released                                          │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

---

## SETTLEMENT HANDSHAKE (Critical)

**Problem:** Escrow releases funds → Wallet credits vendor → Risk calculates hold.
If Wallet fails, funds are lost. If Risk fails, hold isn't applied.

**Solution:** Idempotent, two-phase commit pattern.

```
PHASE 1: Escrow Ready
├─ EscrowTransaction.State = SettlementPending
├─ Publish SettlementRequestedEvent with:
│  ├─ IdempotencyKey = "ESC_123_SETTLE_001" (immutable)
│  ├─ SupplierVendorId, SupplierAmount
│  ├─ ResellerVendorId, ResellerAmount
│  ├─ PlatformFeeAmount
│  └─ Timestamp
└─ Persist to OutboxMessage (reliable delivery)

PHASE 2: Wallet Processing [Serializable Isolation]
├─ Consume SettlementRequestedEvent
├─ BEGIN TRANSACTION (Serializable)
│  ├─ Check: IdempotencyKey not already processed
│  │  └─ If exists: Return silently (idempotent)
│  ├─ Load WalletAccount (Supplier) with LOCK
│  ├─ AvailableBalance += SupplierAmount
│  ├─ Deduct PendingBalance if applicable
│  ├─ ConcurrencyVersion++
│  ├─ Insert WalletLedger with IdempotencyKey
│  ├─ Repeat for Reseller
│  └─ COMMIT (all-or-nothing)
├─ Publish WalletSettledEvent with same IdempotencyKey
└─ Write to OutboxMessage

PHASE 3: Risk Reserve Calculation [Independent]
├─ Consume WalletSettledEvent
├─ Load VendorReserveRule (Reseller)
├─ CalculateHold = Amount × (HoldPercentage / 100)
├─ Create ReserveSchedule (ReleaseOnUtc = NOW + HoldDays)
├─ Update WalletAccount:
│  ├─ AvailableBalance -= CalculateHold
│  └─ ReserveBalance += CalculateHold
└─ Publish ReserveHoldCreatedEvent

PHASE 4: Accounting [Async]
├─ Consume SettlementRequestedEvent
├─ BEGIN TRANSACTION
│  ├─ Create JournalEntry with IdempotencyKey
│  ├─ Lines:
│  │  ├─ Dr. SupplierPayable $X, Cr. EscrowLiability
│  │  ├─ Dr. ResellerPayable $Y, Cr. EscrowLiability
│  │  └─ Dr. PlatformIncome $Z, Cr. EscrowLiability
│  └─ Post (double-entry verified)
└─ COMMIT
```

**Failure Scenarios:**

| Failure Point | Detection | Recovery |
|---------------|-----------|----------|
| Wallet service down | OutboxMessage.IsProcessed = false | Automatic retry (5 min, 10x exponential backoff) |
| Wallet transaction fails | Exception logged | Manual intervention: Review & retry from admin dashboard |
| Risk service down | OutboxMessage.IsProcessed = false | Automatic retry (funds held in AvailableBalance temporarily) |
| Duplicate wallet event | IdempotencyKey check | Silent return (no duplicate credit) |
| Accounting fails silently | No JournalEntry created | Weekly reconciliation task flags missing entries |

---

## WALLET BALANCE MODEL

**Tri-State Balance:**

```
Total Vendor Balance = Available + Pending + Reserve

Available Balance
├─ Can be withdrawn immediately
├─ Increases on: Settlement, Reserve Release, Promotional Credit
├─ Decreases on: Withdrawal, Reserve Hold, Chargeback, Refund
└─ Example: $1000 (vendor can cash out)

Pending Balance
├─ Awaiting final confirmation
├─ Used for: Pre-authorization holds
├─ Decreases when: Confirmed or timeout
└─ Example: $0 (no pending)

Reserve Balance
├─ Held for chargeback protection (typically 45 days)
├─ Immovable during hold period
├─ Cannot be withdrawn
├─ Auto-releases after HoldDays
└─ Example: $200 (20% of $1000 settlement held 45 days)

LEDGER PERSPECTIVE:
Entry:  Credit $1000
├─ Supplier WalletAccount:
│  ├─ AvailableBalance: $0 → $1000
│  ├─ PendingBalance: $0 → $0
│  ├─ ReserveBalance: $0 → $0
│  └─ WalletLedger: [Credit $1000, ReferenceType=Settlement, IdempotencyKey=ESC_123]

Withdrawal Request: $500
├─ Supplier WalletAccount:
│  ├─ AvailableBalance: $1000 → $500 (deducted)
│  ├─ PendingBalance: $0 → $500 (pending approval)
│  └─ WalletLedger: [Debit $500, ReferenceType=Withdrawal, IdempotencyKey=WD_001]

Reserve Hold (20% × $1000):
├─ Reseller WalletAccount:
│  ├─ AvailableBalance: $1000 → $800 (deducted $200)
│  ├─ ReserveBalance: $0 → $200 (held)
│  └─ WalletLedger: [Debit $200, ReferenceType=Hold, IdempotencyKey=RSV_123]

Reserve Released (45 days later):
├─ Reseller WalletAccount:
│  ├─ AvailableBalance: $800 → $1000 (returned)
│  ├─ ReserveBalance: $200 → $0
│  └─ WalletLedger: [Credit $200, ReferenceType=ReleaseReserve, IdempotencyKey=RLS_123]
```

---

## COMMISSION CALCULATION ENGINE

**Architecture:**

```
CommissionRule (Rate Card)
├─ Global defaults (e.g., 5% platform fee)
├─ Vendor-specific overrides (e.g., preferred supplier: 3%)
├─ Time-based (effective from/to dates)
├─ Order-threshold tiers (e.g., >$10K = 4%, >$50K = 3%)
└─ Caps (e.g., max $100 per order)

Commission Calculation Flow:
1. Get Order Total = $1000
2. Load CommissionRule (Supplier)
   └─ If none, load Global Default (5%)
3. Calculate Gross Commission = $1000 × 5% = $50
4. Apply tiering (if applicable)
   └─ If order > $500: Commission -= 1% (new rate = 4% = $40)
5. Apply caps (if applicable)
   └─ If max = $35: Commission = $35
6. Calculate Splits:
   ├─ Supplier gets (100% - PlatformFee): $950
   ├─ Platform takes Commission: $35
   ├─ Reseller margin (already locked in LockedRetailPrice): $50
   └─ Total = $1000 ✓

Commission Audit:
├─ CommissionSplit record with IdempotencyKey
├─ Stored at settlement time (immutable)
├─ GL entry: Dr. Commission Expense, Cr. Revenue
├─ Queryable per order, per vendor, per date range
```

**Business Rules Defin:**

```
PlatformCommission:
├─ Fixed: "Flat $2 per order"
├─ Percentage: "5% of order total"
├─ Tiered: "$0 for <$100, 2.5% for $100-500, 5% for >$500"
├─ Dynamic: Adjust based on vendor performance score

SupplierShare:
├─ LockedWholesalePrice (immutable at order time)
├─ If FullEscrow: Supplier waits for delivery confirmation
├─ If ResellerPrepay: Supplier gets prepay amount upfront (in pending)
└─ If CreditLimit: Supplier bills reseller on invoice (accounting)

ResellerShare:
├─ Margin = LockedRetailPrice - LockedWholesalePrice
├─ Reseller retains margin
├─ Subject to reserve hold (e.g., 20% for 45 days)
└─ Can be disputed if feels undercut
```

---

## DISPUTE RESOLUTION WORKFLOW

```
┌─────────────────────────────────────────────────────┐
│           DISPUTE LIFECYCLE (SLA-Driven)             │
└─────────────────────────────────────────────────────┘

Day 0: Delivery Confirmed
├─ EscrowTransaction.State = Delivered
├─ GracePeriod starts (72 hours = Day 3)
└─ Consumer has until Day 3 to raise dispute

Day 1-3: Dispute Window
├─ Consumer: "Item not as described" (Photo evidence)
├─ System: Create DisputeCase, State = Open
├─ Admin Notification: "New dispute for review"
└─ EscrowTransaction.State = Disputed

Day 3-10: Under Review (SLA = 7 days)
├─ Admin: Review evidence from both sides
├─ Supplier given 2 days to respond
├─ Consumer given 2 days to rebut
├─ Admin deliberates
└─ DisputeCase.Status = UnderReview

Day 10: Resolution
├─ DECISION 1: Approve Release
│  ├─ Admin finds supplier not at fault
│  ├─ EscrowTransaction.State = SettlementPending
│  ├─ Proceed to normal settlement
│  └─ Supplier gets paid, Reseller keeps margin
│
├─ DECISION 2: Authorize Refund
│  ├─ Admin finds supplier at fault
│  ├─ EscrowTransaction.State = Refunded
│  ├─ Publish EscrowRefundedEvent
│  ├─ Wallet:
│  │  ├─ Debit Supplier (lose payment)
│  │  ├─ Credit Consumer (refund)
│  │  └─ Mark as ChargebackCase
│  ├─ GL: Dr. Loss / Cr. Vendor Payable
│  └─ Supplier reputation hit
│
└─ DisputeCase.Status = Resolved, ResolvedOnUtc = NOW

FINANCIAL IMPACT:
┌────────────────────────────────────────────┐
│ DECISION 1: Approve Release                │
├────────────────────────────────────────────┤
│ Supplier Wallet:  +$75 (wholesale)         │
│ Reseller Wallet:  +$20 (margin - $10 hold) │
│ Platform:         +$5 (commission)         │
│ GL: Normal accounting entries              │
└────────────────────────────────────────────┘

┌────────────────────────────────────────────┐
│ DECISION 2: Authorize Refund               │
├────────────────────────────────────────────┤
│ Supplier Wallet:  -$75 (loss)              │
│ Consumer:         +$100 (refund)           │
│ Platform:         -$5 (absorb commission)  │
│ GL: Dr. Chargeback Loss $75 / Cr. Payable  │
│ Risk: Supplier reserve NOT released        │
└────────────────────────────────────────────┘
```

---

## CHARGEBACK HANDLING (External)

**Scenario:** Days after settlement, consumer's payment processor disputes charge.

```
Day 0: Chargeback Notification
├─ Payment gateway notifies system (webhook)
├─ Amount: $100 (original order total)
├─ Reason: "Unauthorized" or "Not as described"
└─ ExternalCaseId: "CB_12345" (processor case ID)

Day 1: Record Chargeback
├─ Admin logs into Risk dashboard
├─ Clicks "Record Chargeback" for order
├─ Creates ChargebackCase:
│  ├─ VendorId = Supplier or Reseller (who gets hit?)
│  ├─ Amount = $100
│  ├─ Status = Pending
│  └─ ExternalCaseId = "CB_12345"
│
├─ System:
│  ├─ Find EscrowTransaction (settled, vendor already paid)
│  ├─ Find WalletAccount(Supplier)
│  ├─ Trigger ChargebackDeductedEvent
│  └─ Consume in Wallet:
│     ├─ AvailableBalance -= $100 (if available)
│     ├─ Or ReserveBalance -= $100 (if in hold)
│     ├─ Or create negative balance (vendor owes platform)
│     └─ Insert WalletLedger: Debit $100, ReferenceType=Chargeback
│
├─ Accounting:
│  ├─ Dr. Chargeback Loss $100 / Cr. Vendor Payable
│  └─ Record in GL for dispute tracking
│
└─ Notification:
   ├─ Supplier: "Chargeback recorded for order #123"
   ├─ Admin: "Chargeback processed"
   └─ ChargebackCase.Status = Deducted

Days 2-30: Dispute Period (Supplier Can Challenge)
├─ Supplier can provide evidence (shipping proof, delivery confirmation)
├─ Update ChargebackCase with evidence
├─ Admin can overturn if evidence solid
├─ Otherwise, chargeback stands (ChargebackCase.Status = Resolved)

Accounting Recovery:
├─ If overturned: Reverse GL entry, restore wallet balance
├─ If upheld: Supplier loss is final
└─ Report chargeback rate for vendor scoring
```

**Financial Guarantee:**
- Platform never loses money (chargebacks come out of vendor balance)
- If vendor balance insufficient: Vendor owes platform (negative balance)
- Platform can place lien on future settlements until paid

---

## DOUBLE-ENTRY ACCOUNTING RULES

**Every transaction must satisfy:**

```
SUM(Debits) == SUM(Credits)

Example: Order Settlement $100

JOURNAL ENTRY: "ESC_123_SETTLE"
├─ IdempotencyKey = "ESC_123_SETTLE_001" (unique, prevents double posting)
├─ Reference = "ESC_123"
├─ Lines:
│  │
│  ├─ [Line 1] Dr. 1001 (Cash/Bank)         $100.00
│  │           Cr. 2001 (Escrow Liability)         $100.00
│  │           [When order paid]
│  │
│  ├─ [Line 2] Dr. 2002 (Supplier Payable)   $75.00
│  │           Dr. 2003 (Reseller Payable)   $20.00
│  │           Dr. 4001 (Platform Income)    $5.00
│  │           Cr. 2001 (Escrow Liability)          $100.00
│  │           [When settlement released]
│  │
│  └─ Validation: $75 + $20 + $5 (Debits) == $100 (Credits) ✓

GL Chart Structure:

ASSETS:
├─ 1001 Cash
├─ 1002 Accounts Receivable
└─ 1003 Inventory

LIABILITIES:
├─ 2001 Escrow Liability (Customer funds held)
├─ 2002 Supplier Payable
├─ 2003 Reseller Payable
└─ 2004 Platform Payable

EQUITY:
└─ 3001 Retained Earnings

REVENUE:
├─ 4001 Platform Commission
├─ 4002 Interchange Fees
└─ 4003 Listing Fees

EXPENSES:
├─ 5001 Chargeback Loss
├─ 5002 Refund Loss
├─ 5003 Fraud Loss
└─ 5004 Payment Processing Fees
```

**Posting Rules:**

| Scenario | Dr. | Cr. | Idempotency |
|----------|-----|-----|-------------|
| Order Paid | 1001 Cash | 2001 Escrow | SettlementRequestedEvent.IdempotencyKey |
| Settlement | 2002/2003 Payables | 2001 Escrow | SettlementRequestedEvent.IdempotencyKey |
| Refund | 5002 Refund Loss | 2002 Supplier Payable | EscrowRefundedEvent.IdempotencyKey |
| Chargeback | 5001 Chargeback Loss | 2002 Supplier Payable | ChargebackDeductedEvent.IdempotencyKey |
| Reserve Hold | 2005 Reserve Hold | 2002 Supplier Payable | ReserveHoldCreatedEvent.IdempotencyKey |
| Reserve Release | 2002 Supplier Payable | 2005 Reserve Hold | ReserveReleasedEvent.IdempotencyKey |

---

## REPLAYABILITY & AUDIT

**Requirement:** Any financial operation must be replayed from audit trail.

```
REPLAY SCENARIO: "Reconstruct Q3 Commission Report"

1. Query JournalEntry where PostedOnUtc BETWEEN Q3Start AND Q3End
2. Filter for: Reference LIKE 'ESC_%'
3. Sum all Lines where DebitAccount = 4001 (Platform Income)
4. Cross-reference with CommissionSplit records
5. Verify GL balance == Sum of CommissionSplit.PlatformFeeAmount
6. If mismatch: Flag for manual review

AUDIT TRAIL PROPERTIES:
├─ IdempotencyKey: Immutable, unique per operation
├─ CreatedOnUtc: Immutable timestamp
├─ IdempotencyKey in OutboxMessage: Prevents replay of events
├─ AdminUserId in EscrowStateHistory: Who approved transition
├─ ReferenceId in JournalEntry: Trace to source order
└─ WalletLedger.IdempotencyKey: Every balance change is traced

COMPLIANCE QUERIES:
├─ "Give me all chargebacks for vendor 123 in 2024"
   → SELECT * FROM ChargebackCase WHERE VendorId = 123
├─ "Reconcile wallet balance for vendor 456 as of 2024-12-31"
   → Load WalletAccount, Sum WalletLedger grouped by EntryType
├─ "Verify commission for order 789"
   → Load CommissionSplit, Cross-check GL entries
└─ "Audit trail for settlement ESC_999"
   → EscrowStateHistory, CommissionSplit, JournalEntry
```

---

## FINANCIAL INTEGRITY REQUIREMENTS

1. **No Money Creation:** Sum of WalletAccount.AvailableBalance ≤ Total Settled
2. **No Money Loss (Platform):** Platform never goes negative (vendor absorbs loss)
3. **Immutability:** LockedWholesalePrice, LockedRetailPrice cannot change post-order
4. **Idempotency:** Every significant operation has unique IdempotencyKey
5. **ACID Transactions:** All multi-step operations use Serializable isolation
6. **Audit Trail:** CreatedOnUtc, UpdatedOnUtc, IdempotencyKey on all tables
7. **GL Reconciliation:** Weekly automated verification (debits == credits)
8. **Reserve Correctness:** ReserveSchedule.ReleaseOnUtc must be deterministic

