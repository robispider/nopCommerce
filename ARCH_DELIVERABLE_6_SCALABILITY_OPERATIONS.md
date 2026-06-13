# DELIVERABLE 6: Scalability & Operations

---

## INFRASTRUCTURE ARCHITECTURE

```
┌─────────────────────────────────────────────────────────────┐
│                    LOAD BALANCER (Cloudflare)               │
│  ├─ DDoS protection, SSL/TLS termination, Rate limiting     │
│  └─ Geographic routing (latency-based)                      │
└────────────────────────────┬────────────────────────────────┘
                             │
         ┌───────────────────┼───────────────────┐
         ▼                   ▼                   ▼
    ┌─────────┐         ┌─────────┐         ┌─────────┐
    │ App Pod │         │ App Pod │         │ App Pod │
    │ (.NET 9)│         │ (.NET 9)│         │ (.NET 9)│
    └────┬────┘         └────┬────┘         └────┬────┘
         │                   │                   │
         └───────────────────┼───────────────────┘
                             │
         ┌───────────────────┼───────────────────┐
         ▼                   ▼                   ▼
    ┌─────────┐         ┌─────────┐         ┌─────────┐
    │ Redis   │         │RabbitMQ │         │PostgreSQL
    │ Cache   │         │ Message │         │ Primary │
    │ Cluster │         │ Queue   │         │   DB    │
    └─────────┘         └─────────┘         └────┬────┘
                                                 │
                                        ┌────────┴────────┐
                                        ▼                 ▼
                                    ┌────────┐      ┌────────┐
                                    │ Read   │      │Replica │
                                    │ Replica│      │ 2      │
                                    └────────┘      └────────┘

    ┌─────────┐         ┌─────────┐         ┌─────────┐
    │ MinIO   │         │OpenSearch         │Grafana  │
    │ Object  │         │ Full Text│        │Logging  │
    │ Storage │         │ & Analytics      │ & Audit │
    └─────────┘         └─────────┘         └─────────┘
```

---

## REDIS CACHING STRATEGY

**Purpose:** Reduce DB load, improve response times.

```
Cache Layers:

L1 Cache (Request-scoped):
├─ StorefrontContext → Request memory
├─ Resolution: URL slug → Storefront entity
├─ TTL: Request lifetime only
└─ Miss cost: DB query (~50ms)

L2 Cache (Distributed - Redis):
├─ ResellerStorefront by slug: TTL = 1 hour
├─ SupplierProduct by ProductId: TTL = 30 minutes
├─ VendorReserveRule: TTL = 1 day
├─ CommissionRule: TTL = 1 day
└─ GlAccount: TTL = 1 week

High-Hit Caches (Evict last):
├─ StorefrontBySlug: 10K entries, ~100KB
├─ ProductWholesaleRules: 50K entries, ~500KB
└─ Total cache size: ~100MB

Invalidation Strategy:
├─ Event-driven (consume entity updated events)
├─ Example: ProductUpdatedEvent → Invalidate ProductWholesale cache
├─ TTL-based (auto-expire, then re-populate on next query)
└─ Manual (admin dashboard "Flush Cache")

Cache Warm-up (On startup):
├─ Load all active Storefronts → Cache
├─ Load global settings (CommissionRule VendorId=null)
└─ Async background job every 6 hours
```

---

## RABBITMQ MESSAGE QUEUE

**Purpose:** Reliable, async event processing. Scale consumers independently.

```
Message Topology:

Exchanges (Fanout):
├─ marketplace.core (broadcasts all core events)
├─ marketplace.escrow (settlement & dispute events)
├─ marketplace.wallet (balance update events)
├─ marketplace.risk (reserve & chargeback events)
└─ marketplace.accounting (GL posting events)

Queues (Durable):
├─ marketplace.escrow.handler (1 consumer)
├─ marketplace.wallet.handler (1 consumer, process settlement in order)
├─ marketplace.risk.handler (1 consumer)
├─ marketplace.accounting.handler (N consumers, GL non-blocking)
├─ marketplace.notification.handler (N consumers, email async)
├─ marketplace.search.handler (N consumers, index updates)
└─ marketplace.deadletter (poison pill queue for failed messages)

Dead Letter Handling:
├─ Max retries: 5
├─ Backoff: Exponential (1s, 2s, 4s, 8s, 16s)
├─ If all retries fail: Move to deadletter queue
├─ Admin notified: Manual review & retry option
└─ Auto-archive after 30 days

Consumer Scaling:
├─ marketplace.wallet.handler: 1 instance (serializable settlement)
├─ marketplace.escrow.handler: 1 instance (state machine integrity)
├─ marketplace.accounting.handler: 3 instances (GL posting parallelizable)
├─ marketplace.notification.handler: 5 instances (email IO-bound)
└─ marketplace.search.handler: 2 instances (index updates)
```

---

## POSTGRESQL OPTIMIZATION

**High-Volume Tables & Partitioning:**

```
WalletLedger (50M+ rows/year):
├─ Partition by: Month (RANGE PARTITION BY MONTH(CreatedOnUtc))
├─ Retention: 7 years
├─ Archive: Move 2+ year-old partitions to cold storage
├─ Indexes:
│  ├─ IX_WalletAccountId_CreatedOnUtc (for vendor reports)
│  ├─ IX_IdempotencyKey (for duplicate detection)
│  └─ IX_ReferenceType_ReferenceId (for settlement reconciliation)
└─ Query optimization: Always filter by CreatedOnUtc range

JournalEntryLine (30M+ rows/year):
├─ Partition by: Month
├─ Retention: 7 years (compliance requirement)
├─ Compression: After 2 years
├─ Full-table scan: Avoid (PARTITION ELIMINATION)
└─ Index: IX_JournalEntryId (for entry lookup)

OutboxMessage (100M+ rows/year):
├─ Partition by: Month
├─ Retention: 30 days after IsProcessed=1
├─ Cleanup: Nightly batch delete (DELETE FROM OutboxMessage WHERE CreatedOnUtc < NOW() - 30 DAYS AND IsProcessed=1)
├─ Performance: INSERT-heavy (producers fast, consumers catch up)
└─ Index: IX_IsProcessed_CreatedOnUtc (for consumer query)

Connection Pooling:
├─ Min pool size: 10
├─ Max pool size: 100
├─ Idle timeout: 15 minutes
└─ Query timeout: 30 seconds (adjust per query type)

Backup Strategy:
├─ Daily full backup (to S3)
├─ Hourly incremental backup
├─ PITR: 30 days
├─ Test restore: Monthly
└─ RPO: 1 hour, RTO: 4 hours
```

---

## OPENSEARCH (FULL-TEXT SEARCH)

**Purpose:** Product discovery, vendor catalog search, advanced filtering.

```
Index Mapping:

products_index (Real-time search):
├─ Fields:
│  ├─ ProductId (keyword)
│  ├─ Name (text, analyzed with stemming)
│  ├─ Description (text)
│  ├─ Category (keyword)
│  ├─ SupplierVendorId (keyword)
│  ├─ ResellerVendorId (keyword)
│  ├─ Price (float)
│  ├─ MinimumOrderQuantity (integer)
│  ├─ IsActive (boolean)
│  ├─ IsDropshipEnabled (boolean)
│  └─ UpdatedOnUtc (date)
├─ Sharding: 5 primary shards, 2 replicas
├─ Refresh interval: 1 second (near real-time)
└─ TTL: N/A (persistent)

Indexing Strategy:

On ProductClonedEvent:
├─ Producer: Marketplace.Wholesale
├─ Index new reseller product into products_index
├─ Add facets: Category, SupplierName, Price Range
├─ Async: No block on checkout

Rebuild Index:
├─ Trigger: Admin command or scheduled (monthly)
├─ Process:
│  ├─ Create products_index_v2
│  ├─ Bulk import all active products
│  ├─ Test search queries
│  ├─ Alias switch: products → products_v2
│  └─ Delete old index
├─ Duration: ~5 minutes (100K products)
└─ Zero downtime: Via aliasing

Search Query Examples:

B2B Catalog Search (Reseller browsing):
├─ POST /products_index/_search
├─ Query: "running shoes" AND SupplierVendorId = 2
├─ Filters: Price [0-500], MinimumOrderQuantity ≤ 100
├─ Facets: Category, SupplierVendorId, Price ranges
└─ Results: Ranked by relevance + freshness

Storefront Search (B2C customer):
├─ Scoped to ResellerVendorId = 5
├─ Query: "running shoes"
├─ Filter: IsActive=true, InStock=true
├─ Sort: Relevance or Price
└─ Pagination: 20 results/page

Vendor Analytics Search:
├─ Products sold by VendorId = 5 in last 90 days
├─ Aggregation: Count, sum revenue
└─ Time bucket: Daily sales trend
```

---

## MINION OBJECT STORAGE

**Purpose:** Secure KYC document storage, product images, user uploads.

```
Bucket Structure:

marketplace-kyc-docs/
├─ {vendorId}/{documentType}/{fileName}
├─ Example: 123/TaxId/2024-01_tax_cert.pdf
├─ Lifecycle:
│  ├─ Upload: On document submission
│  ├─ Access: Admin review (restricted by role)
│  ├─ Retention: 7 years (compliance)
│  └─ Delete: Admin can purge after resolution
├─ Security:
│  ├─ Private by default (no public URLs)
│  ├─ Pre-signed URLs (15-min expiry for admin)
│  └─ Encryption at rest (MinIO default or KMS)
└─ Backup: Daily to cold storage

marketplace-product-images/
├─ {productId}/{imageNumber}.{ext}
├─ Lifecycle:
│  ├─ Upload: On product creation
│  ├─ Resize: Lambda function (400x400, 800x800)
│  ├─ CDN: Cloudflare cache (1 month TTL)
│  └─ Delete: On product deletion
├─ Public URLs (via Cloudflare CDN)
└─ Backup: Continuous replication

marketplace-invoices/
├─ {vendorId}/invoices/{invoiceId}.pdf
├─ Generate on settlement
├─ Private (vendor can download)
└─ Retention: 7 years
```

---

## MONITORING & LOGGING

**Stack: Grafana + Prometheus + Loki (or ELK)**

```
Key Metrics to Monitor:

Application:
├─ HTTP request rate (req/sec)
├─ P50, P95, P99 latencies
├─ Error rate (4xx, 5xx)
├─ Database connection pool utilization
├─ Cache hit/miss ratio
├─ Message queue depth (backlog)
└─ Event processing lag (OutboxMessage.IsProcessed timestamp)

Financial:
├─ Settlement count (per day)
├─ Escrow state distribution (how many in each state?)
├─ Reserve hold amount (total $ held)
├─ Chargeback rate (chargebacks / settlements)
├─ Vendor withdrawal request success rate
└─ GL reconciliation status (debits == credits?)

Scaling:
├─ Pod CPU/Memory utilization
├─ Database queries/sec
├─ Redis memory usage
├─ RabbitMQ messages/sec
├─ OpenSearch indexing rate
└─ MinIO storage usage

Alerts:

Critical:
├─ Error rate > 1% → PagerDuty
├─ Settlement processing time > 5 minutes → PagerDuty
├─ GL reconciliation failed → PagerDuty
├─ Database connection pool exhausted → Page
├─ Message queue dead-letter accumulation → Page

High:
├─ Cache hit ratio < 80% → Slack
├─ P95 latency > 1 second → Slack
├─ Pod restart (CrashLoopBackOff) → Slack
└─ Vendor withdrawal processing delayed → Slack

Low:
├─ Disk space < 20% → Slack
├─ DB backup failed → Slack
└─ Index rebuild in progress → Slack
```

---

## DISASTER RECOVERY

```
Failure Scenario → Recovery Plan:

PostgreSQL Primary Down:
├─ Detect: Heartbeat timeout (10s)
├─ Action: Failover to Read Replica (automated)
├─ Downtime: ~30 seconds
├─ Data loss: None (sync replication)
└─ Verify: Run test failover weekly

Redis Cache Down:
├─ Impact: Slow queries (DB queries instead of cache)
├─ TTL: 30 min for most caches
├─ No data loss (cache is ephemeral)
├─ Recovery: Restart instance, warm up on demand
└─ Mitigation: 3-node cluster (quorum)

RabbitMQ Down:
├─ Impact: No async processing
├─ Data loss: None (durable queues persist to disk)
├─ Recovery: 
│  ├─ Restart cluster
│  ├─ Messages re-process from disk
│  ├─ Reorder guarantee maintained
│  └─ Idempotency keys prevent double-processing
└─ Mitigation: 3-node cluster, automatic failover

MinIO Down:
├─ Impact: Cannot upload new documents
├─ Data: Replicated to cold storage
├─ Recovery: 
│  ├─ Bring up backup MinIO cluster
│  ├─ Restore from S3 backup
│  └─ DNS alias switch
└─ RTO: 2 hours, RPO: 1 hour

OpenSearch Down:
├─ Impact: Search unavailable (fallback to DB search)
├─ Data: Rebuilt from Products table
├─ Recovery: 
│  ├─ Restart cluster
│  ├─ Rebuild index (5 min)
│  └─ Resume search
└─ No transaction loss (read-only index)

Network Partition (App → DB isolated):
├─ Behavior: Circuit breaker trips
├─ App: Respond with cached data or error (graceful degradation)
├─ No financial operation proceeds (escrow, settlement blocked)
├─ Recovery: Reconciliation after network heals
└─ Mitigation: Multi-region deployment

Multi-Region Deployment:
├─ Primary: US-East (main)
├─ DR: US-West (warm standby)
├─ Replication:
│  ├─ Database: Async replication (5s lag acceptable)
│  ├─ Cache: Independent per region
│  ├─ Messages: Re-publish to DR queue on failover
│  └─ Storage: Cross-region S3 replication
├─ Failover: Manual (DNS switch) or automatic (30s detection)
└─ Test: Monthly failover drill
```

---

## PERFORMANCE TARGETS

```
Response Time:
├─ API (99th percentile):
│  ├─ Product search: < 500ms
│  ├─ Order placement: < 2 seconds
│  ├─ Wallet balance fetch: < 200ms
│  └─ Storefront render: < 800ms
└─ Background jobs: < 1 hour

Throughput:
├─ Concurrent users: 10,000
├─ Orders/second: 100 (peak)
├─ Settlements/day: 1M (250/sec batch)
├─ Wallet transactions/day: 5M
└─ Search queries/second: 1,000

Availability:
├─ Uptime SLA: 99.9% (43 minutes/month)
├─ Recovery: RTO ≤ 1 hour, RPO ≤ 15 minutes
├─ Planned maintenance window: 4 hours/quarter (announced)
└─ Financial transactions: 99.95% (13 minutes/month)

Database:
├─ Query response: P95 < 100ms
├─ Slow query log: Queries > 1 second logged
├─ Checkpoints: Every 5 minutes
├─ Replication lag: < 5 seconds
└─ Max connections: 500 concurrent

Cache:
├─ Redis: 99% hit ratio for hot paths
├─ Eviction: LRU when memory full
├─ Refresh: Background async (don't block requests)
└─ Invalidation: Event-driven < 1 second

Message Queue:
├─ Publish latency: < 50ms
├─ Consume latency: Settlement < 10s end-to-end
├─ Queue depth: < 10K messages (healthy)
├─ DLQ depth: < 100 messages (alert threshold)
└─ Delivery guarantee: At least once (with idempotency)
```

---

## BOTTLENECK ANALYSIS & MITIGATION

| Bottleneck | Root Cause | Mitigation |
|-----------|-----------|-----------|
| **Settlement Latency** | Serializable TX lock | Batch settlements, queue-based processing |
| **Wallet Balance Update** | ConcurrencyVersion contention | Read ledger instead of balance, calculate on demand |
| **Inventory Reservation** | DB lock on InventoryBucket | Denormalized cache + eventual consistency check |
| **Product Search** | Full-text index size | Partition by date, archive old products, faceted search |
| **Image CDN Bandwidth** | Large product images | Compress (75% reduction), lazy load, WebP format |
| **GL Posting** | Double-entry transaction size | Batch GL entries, async posting |
| **KYC Doc Upload** | MinIO latency | Multi-part upload, client-side compression |
| **Escrow State Query** | Full table scan | IX_CurrentStateId, cache dashboard counts |

