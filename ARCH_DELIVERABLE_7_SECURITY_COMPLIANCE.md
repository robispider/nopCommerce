# DELIVERABLE 7: Security & Compliance Review

---

## ACCESS CONTROL & AUTHORIZATION

**Role-Based Access Control (RBAC) Model:**

```
Platform Roles:

Administrator (Full):
├─ Approve/reject vendor KYC
├─ Suspend/deactivate vendors
├─ View all settlements & disputes
├─ Initiate refunds/chargebacks
├─ Override commission rules
├─ Access accounting (GL view)
├─ Manage admin users
└─ Audit log full access

Risk Manager:
├─ View chargeback cases
├─ Review dispute evidence
├─ Calculate vendor reserves
├─ Adjust vendor risk scores
├─ Approve/deny overrides
└─ Generate risk reports

Finance Officer:
├─ View GL accounts & trial balance
├─ Approve settlement batches
├─ Reconcile vendor payables
├─ Export financial reports
└─ Audit log (finance-scoped)

Vendor (Supplier/Reseller):
├─ View own orders & fulfillment tickets
├─ Accept/reject dropship tickets
├─ Track shipments
├─ View wallet balance
├─ Request withdrawals
├─ Upload KYC documents
├─ View own commission/fees
└─ Access own sales reports

Customer (B2C):
├─ Place orders
├─ Raise disputes
├─ Request refunds
└─ View order history

Permission Matrix:

Entity: MarketplaceBusiness
├─ Create: Admin only
├─ Read: Admin (all), Vendor (own)
├─ Update: Admin only
├─ Delete: Admin only
└─ Audit: All changes logged with admin ID

Entity: EscrowTransaction
├─ Create: System (auto on order)
├─ Read: Admin, RiskManager, FinanceOfficer, Vendor (own)
├─ Update: System (state machine)
├─ Transition: Admin/RiskManager (after review)
└─ Audit: IdempotencyKey, timestamp, who approved

Entity: WalletAccount
├─ Read: Owner Vendor, Admin
├─ View balance: Vendor (own), Admin
├─ Withdraw: Vendor (own)
├─ Approve withdrawal: FinanceOfficer
└─ Audit: Every debit/credit with IdempotencyKey

Entity: JournalEntry
├─ Create: System only (no manual GL entry in MVP)
├─ Read: FinanceOfficer
├─ Verify: FinanceOfficer (sign-off trial balance)
└─ Export: FinanceOfficer (audit trail)
```

---

## KYC & DOCUMENT SECURITY

**Document Handling:**

```
Upload Flow:
1. Vendor submits document (PDF, JPG, PNG, max 10MB)
2. Client validates: Format, size, no malicious code
3. Server validation:
   ├─ Scan file for viruses (ClamAV)
   ├─ Check MIME type (not executable)
   ├─ Store in MinIO (encrypted)
   └─ Generate pre-signed URL (15-min expiry)
4. Admin downloads: Pre-signed URL expires automatically
5. After verification:
   ├─ Mark as Approved/Rejected
   ├─ Delete from hot storage after 7 years
   └─ Archive to cold storage (S3 Glacier)

File Storage Security:
├─ Encryption: MinIO server-side encryption (AES-256)
├─ Access: Only authenticated admin + temporary pre-signed URL
├─ No public URLs (private by default)
├─ Virus scan: ClamAV on upload
├─ Retention: 7 years compliance
└─ Audit: Every access logged (who, when, file name encrypted)

Approved Document Lifecycle:
├─ TaxId document: Extracted taxId → encrypted field in DB
├─ BankStatement: Used for verification, no extraction needed
├─ BusinessLicense: Validation proof, no personal data
└─ All: Permanently delete option after 7-year retention
```

---

## FINANCIAL TRANSACTION SECURITY

**Atomic Guarantees:**

```
Escrow State Transitions (No race condition):
├─ BEGIN TRANSACTION (Serializable)
├─ SELECT EscrowTransaction FOR UPDATE (locked row)
├─ Validate: CurrentState → NewState legal
├─ UPDATE CurrentState
├─ INSERT EscrowStateHistory (audit)
├─ INSERT OutboxMessage (for side-effects)
├─ COMMIT (all-or-nothing)
└─ If conflict: Retry with exponential backoff

Wallet Balance Update (ConcurrencyVersion):
├─ BEGIN TRANSACTION (Serializable)
├─ SELECT WalletAccount WHERE VendorId=X FOR UPDATE
├─ Validate: ConcurrencyVersion matches (optimistic lock)
├─ If mismatch: ROLLBACK, retry
├─ AvailableBalance += Amount
├─ ConcurrencyVersion++
├─ INSERT WalletLedger with IdempotencyKey
├─ COMMIT
└─ Idempotency: If duplicate IdempotencyKey, skip (no error)

Settlement Handshake (Two-Phase):
├─ PHASE 1: Escrow publishes SettlementRequestedEvent
│  ├─ IdempotencyKey = ESC_123_SETTLE_001
│  ├─ Persisted to OutboxMessage (durable)
│  └─ OutboxMessage.IsProcessed = false
│
├─ PHASE 2: Wallet consumes event
│  ├─ BEGIN TRANSACTION
│  ├─ Check IdempotencyKey not in WalletLedger
│  ├─ If exists: Return (idempotent, no error)
│  ├─ Load WalletAccount(Supplier) FOR UPDATE
│  ├─ Credit AvailableBalance
│  ├─ Insert WalletLedger with IdempotencyKey
│  ├─ Load WalletAccount(Reseller) FOR UPDATE
│  ├─ Credit AvailableBalance
│  ├─ Insert WalletLedger
│  ├─ COMMIT
│  └─ Publish WalletSettledEvent (same IdempotencyKey)
│
├─ PHASE 3: Update OutboxMessage
│  ├─ UPDATE OutboxMessage.IsProcessed = true
│  └─ OutboxMessage.ProcessedOnUtc = NOW
└─ Guarantee: If any phase fails, others retry independently
```

**GL Posting Verification:**

```
Before posting:
├─ Sum(DebitAmount) MUST equal Sum(CreditAmount)
├─ No rounding errors (use DECIMAL(18,2))
├─ Reference ID must be valid (order/escrow/settlement)
└─ IdempotencyKey unique (no duplicate postings)

After posting:
├─ SELECT * FROM JournalEntry where IdempotencyKey=X
├─ Verify exactly ONE entry with this key
├─ Nightly reconciliation: Calculate trial balance
│  ├─ For each GlAccount: SUM(Debits) - SUM(Credits)
│  ├─ Compare to expected balance
│  └─ Flag discrepancies
└─ Monthly: Full GL audit trail review
```

---

## API & WEBHOOK SECURITY

**Rate Limiting:**

```
Per Vendor (Marketplace API):
├─ Supplier Dashboard: 10 req/sec
├─ Reseller import products: 100 req/5min (batch limit)
├─ Order fulfillment updates: 1000 req/min
└─ Breach: 429 Too Many Requests, exponential backoff

Per IP (DDoS mitigation):
├─ Cloudflare DDoS protection
├─ Rate limit by country (block high-risk)
└─ Automated bot detection

Webhook Delivery (External Integrations):
├─ Webhook URL must be HTTPS
├─ Signed with HMAC-SHA256 (secret key)
├─ Signature header: X-Marketplace-Signature
├─ Receiver MUST verify signature
├─ Retry: 3 attempts (exponential backoff)
├─ Timeout: 30 seconds per attempt
└─ Dead letter: After 3 failures, notify admin
```

**API Authentication:**

```
Vendor API Key:
├─ Generated on vendor approval
├─ Format: mp_[vendor-id]_[random-64-char-key]
├─ Storage: Hashed in database (bcrypt)
├─ Usage: Authorization: Bearer <api_key>
├─ Rotation: Every 90 days (with grace period)
└─ Revocation: Admin can immediately revoke

JWT Tokens (Optional):
├─ Short-lived access token (1 hour)
├─ Refresh token (30 days)
├─ Issued on login, validated on each request
├─ Claims: VendorId, Role, Exp, IssuedAt
└─ Signing key: RS256 (private key on server)
```

---

## DATA BREACH & COMPLIANCE

**PII Data Classification:**

```
Sensitive (Encrypted):
├─ TaxId (encrypted in DB, never logged)
├─ Bank account (never displayed, masked: ****1234)
├─ Personal names (logged, but not in debug output)
└─ Address (for vendors, not displayed in logs)

Restricted (Logged, but limited access):
├─ Order totals
├─ Wallet balances
├─ Vendor metrics
└─ Accessed by: Vendor (own), Admin, Finance

Public:
├─ Product names
├─ Storefront URLs
├─ Vendor names
└─ Review scores (vendor performance)

Breach Response:
├─ Incident detected: Pause API, check logs
├─ Scope: Identify affected vendors/customers
├─ Notification: Within 72 hours (GDPR/CCPA)
├─ Remediation: Rotate API keys, password reset
└─ Post-mortem: Review and prevent recurrence
```

**Compliance Standards:**

```
GDPR (EU customers):
├─ Right to deletion: DELETE FROM WalletLedger/JournalEntry (keep anonymized copy)
├─ Data portability: Export vendor data (JSON)
├─ Purpose limitation: Use order data only for fulfillment
├─ Data retention: Delete after retention period
└─ Audit: Log all data accesses

CCPA (CA customers):
├─ Right to know: Customer can download personal data
├─ Right to delete: Anonymize customer profile
├─ Right to opt-out: Marketing emails
└─ Opt-in (for minors): Explicit consent before processing

PCI-DSS (Payment Processing):
├─ NOT applicable: Marketplace does not store card data
├─ Cards processed by payment gateway (Stripe/PayPal)
├─ Marketplace receives: Order amount, status, reference
├─ Logging: Never log card numbers, even truncated
└─ Audit: Annual penetration testing

SOC 2 Type II (Trust):
├─ Access controls: RBAC enforced
├─ Audit trails: All financial transactions logged
├─ Change management: Only admins can modify GL
├─ Incident response: 72-hour SLA for escalations
└─ Monitoring: 24/7 alerts for anomalies
```

---

## ANTI-FRAUD & RISK DETECTION

**Chargeback Prevention:**

```
Pre-Order Risk Scoring:
├─ Order amount (flag if unusual spike)
├─ Order frequency (new vendor = higher risk)
├─ Geolocation mismatch (billing vs shipping)
├─ Delivery address (POBox, etc. = flag)
├─ Item category (electronics = higher risk)
├─ Customer account age (< 7 days = flag)
└─ Scoring: Sum of risk signals, flag if score > threshold

Delivery Confirmation:
├─ Require tracking number before escrow release
├─ Verify delivery status from carrier
├─ Geolocation match: Item shipped/delivered
├─ Photo proof: Optional (for high-value items)
└─ Grace period: 72h after delivery before settlement

Chargeback Reserve:
├─ Default: 20% of order amount, held 45 days
├─ Adjusted by vendor score:
│  ├─ < 5 chargebacks/year: 15% (low risk)
│  ├─ 5-20 chargebacks/year: 20% (standard)
│  ├─ 20-50 chargebacks/year: 30% (elevated)
│  ├─ > 50 chargebacks/year: 50% (high risk)
│  └─ Suspend vendor if chargeback rate > 5%
└─ Manual review: RiskManager can adjust per case
```

**Vendor Fraud Detection:**

```
Red Flags:
├─ Multiple KYC rejections (flag for manual review)
├─ TaxId mismatch with legal name (verify with authority)
├─ Sudden inventory dumps (liquidation signal)
├─ Price undercut by 50%+ from competitors (dumping)
├─ Returns/refund rate > 10% (quality issue)
├─ Chargebacks > 2% (suspicious)
├─ Dispute rate > 5% (quality/fraud)
├─ Location changes frequently (shell company pattern)
└─ Admin can place vendor under review / suspend

Automated Blocks:
├─ Vendor not KYC approved: Cannot list products
├─ Vendor suspended: All listings unpublished
├─ Vendor negative balance: Withdrawal denied (lien placed)
├─ New vendor: Limited daily order volume (ramp-up)
└─ High-risk category: Manual approval required
```

---

## SEPARATION OF DUTIES

**Admin Functions Require Multi-Party Approval:**

```
High-Risk Operations (Require 2 Admin Signatures):

1. Approve Vendor KYC
   ├─ Admin 1: Review documents & approve
   ├─ Admin 2: Second review & confirm
   ├─ Log both IDs, timestamps
   └─ Vendor notified after both sign-off

2. Initiate Chargeback Deduction
   ├─ Admin 1: Record chargeback case
   ├─ Admin 2: Approve deduction
   ├─ Amount verified against external evidence
   └─ Vendor notified with appeal process

3. Reverse Settlement (Refund)
   ├─ Admin 1: Document reason (dispute evidence)
   ├─ Admin 2: Approve reversal
   ├─ Amount must match escrow balance
   └─ GL entry created (reverse effect)

4. Manual GL Entry (Future)
   ├─ Finance Officer: Submit entry (Dr/Cr pairs)
   ├─ Finance Manager: Approve entry
   ├─ Verify: Sum(Dr) == Sum(Cr)
   └─ Post to ledger

Medium-Risk Operations (Single Admin):

├─ Suspend vendor (temporary)
├─ Adjust commission rule (vendor-specific)
├─ Resolve dispute (in favor of one party)
├─ Release reserve hold early
└─ All logged with admin ID and timestamp
```

---

## AUDIT LOGGING & FORENSICS

**What Gets Logged:**

```
Financial Transactions:
├─ Every WalletLedger entry: IdempotencyKey, Amount, RefType
├─ Every JournalEntry: IdempotencyKey, Dr/Cr, GL Accounts
├─ Every EscrowStateHistory: OldState, NewState, Reason, Admin
├─ Every ChargebackCase: Amount, Reason, Deducted, Admin
└─ Immutable: CreatedOnUtc, cannot be deleted

Admin Actions:
├─ Vendor approved/suspended: Admin ID, timestamp, reason
├─ Dispute resolved: Admin ID, decision (favor of), reasoning
├─ Commission rule changed: Admin ID, before/after values
├─ KYC rejection: Admin ID, reason, document reviewed
├─ Withdrawal approved: Finance officer ID, method, timestamp
└─ GL reconciliation: Finance officer ID, trial balance delta, action

Security Events:
├─ Failed login (admin): IP, username, timestamp
├─ API key generated: Vendor ID, expiry date
├─ API key rotated: Vendor ID, old/new key hash (partial)
├─ Role changed: Admin ID, user, old/new role
├─ Policy override: Admin ID, policy, reason
└─ Incident detected: Alert timestamp, severity, action taken

Log Retention:
├─ Hot logs: 90 days (searchable)
├─ Warm logs: 2 years (archived, accessible)
├─ Cold logs: 7 years (compliance, read-only S3)
└─ Deletion: After retention period, with audit trail
```

**Query Audit Scenarios:**

```
"Reconcile all WalletLedger entries for vendor 123 in Q4 2024"
├─ SELECT * FROM WalletLedger
│  WHERE WalletAccountId=(SELECT ID FROM WalletAccount WHERE VendorId=123)
│  AND CreatedOnUtc BETWEEN 2024-10-01 AND 2024-12-31
├─ Sum(Debits) vs Sum(Credits)
├─ Cross-reference with EscrowStateHistory and ChargebackCase
└─ Verify: Total balance matches

"Find all GL entries posted by Admin 'john@company.com' in last 30 days"
├─ Query audit logs, not GL (to identify suspicious batches)
├─ If found: Spot-check GL entries for accuracy
├─ If discrepancies: Investigate intent (legitimate vs fraud)
└─ Action: Revert entries if needed

"Identify vendors with dispute rate > 5% in last 90 days"
├─ SELECT VendorId, COUNT(*) as disputes FROM DisputeCase
│  WHERE ResolvedOnUtc >= NOW() - 90 DAYS
│  GROUP BY VendorId
│  HAVING disputes / ORDER_COUNT > 0.05
├─ Manual review: Are disputes legitimate or pattern of fraud?
├─ Action: Place vendor under review, request corrective plan
└─ If unresolved: Suspend vendor
```

---

## INCIDENT RESPONSE PLAYBOOK

```
Chargeback Detected:
├─ 1. Alert fires: Chargeback notification from payment gateway
├─ 2. Log incident: Record in ChargebackCase table
├─ 3. RiskManager: Review evidence (delivery confirmation, tracking)
├─ 4. Decision:
│  ├─ Strong evidence of delivery? Dispute with processor
│  ├─ Weak evidence? Accept chargeback loss
│  └─ Fraudulent order? Place vendor under review
├─ 5. Deduct from wallet: ChargebackDeductedEvent
├─ 6. Notify vendor: "Chargeback recorded, balance affected"
└─ 7. Log: Admin ID, decision, reasoning

Settlement Fails (Wallet down):
├─ 1. OutboxMessage.IsProcessed remains false
├─ 2. Retry mechanism: Exponential backoff
├─ 3. Manual intervention:
│  ├─ Check Wallet service status
│  ├─ Review OutboxMessage for pattern
│  ├─ Trigger manual settlement if safe
│  └─ Vendor notified of delay
├─ 4. Post-mortem: Why did wallet fail? (DB, network, bug)
└─ 5. Prevent recurrence: Add monitoring, improve error handling

GL Reconciliation Mismatch:
├─ 1. Alert: Trial balance DR != CR
├─ 2. Investigate:
│  ├─ Check for unposted entries (OutboxMessage backlog)
│  ├─ Find missing IdempotencyKey (duplicate detection)
│  ├─ Scan for manual GL entries (if allowed)
│  └─ Review recent GL posting errors
├─ 3. Root cause:
│  ├─ Bug in accounting logic? Fix + replay
│  ├─ Missing settlement? Trigger GL posting
│  ├─ Corrupt entry? Reverse with approval
│  └─ Database bug? Restore from backup
├─ 4. Fix: Post correcting entries with new IdempotencyKey
└─ 5. Verify: Trial balance now balances, update audit trail

Vendor Dispute Escalation:
├─ 1. Vendor claims "I didn't receive payment for order 123"
├─ 2. Check: Is settlement for order 123 complete?
│  ├─ Query EscrowTransaction state
│  ├─ Check WalletLedger for credit
│  ├─ Verify withdrawal request processed
├─ 3. If settled but not withdrawn:
│  ├─ Show wallet balance to vendor
│  ├─ Process withdrawal if requested
├─ 4. If NOT settled:
│  ├─ Verify escrow state (why not settled?)
│  ├─ Check for disputes/holds
│  ├─ Trigger settlement if cleared
├─ 5. Follow-up:
│  ├─ Confirm vendor received funds
│  ├─ Document resolution
│  └─ Close ticket
└─ 6. Prevent: Improve settlement SLA visibility to vendors
```

