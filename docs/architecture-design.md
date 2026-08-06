# UbudKusCoin — Target Architecture Design

**Status:** Living target architecture; implementation is tracked by stage in the roadmap.
**Principles:** Auditability · Safety of funds first · Deterministic state · Mature crypto/consensus · Supported
dependencies · Test-net before main-net. **No claim of production-readiness is made.**

---

## 1. Goals and non-goals

### Goals (in priority order)
1. **Safety of funds**: deterministic, atomic, auditable state transitions; no double-spend; replay protection.
2. **Consensus correctness**: a specified PoS-BFT family (or a mature engine) with deterministic validator selection,
   finality, and node authentication — not a bespoke scheme.
3. **Payments/merchant utility**: low fee, fast finality, predictable fees, good wallet UX, cheap interaction.
4. **Operability**: observability (logs/metrics/tracing), backup/snapshot/recovery, upgradeability, clean CI/CD.

### Non-goals for v1
- Smart contracts / EVM (deferring; keep the state machine simple and auditable).
- High TPS at launch (target correctness over throughput).
- Novel cryptography. We use well-reviewed primitives only.

---

## 2. Architectural pillars

### Pillar 1 — Integer fixed-point money
All monetary amounts are **64-bit signed integers (satoshis-like units)**. No `double`/`float` anywhere in consensus
criteria.

```proto
// unit = 1e8 base units per UKC (mirrors BTC convention; define in genesis)
int64 amount = 1;
int64 fee    = 2;
```

### Pillar 2 — Canonical, versioned transaction envelope
```proto
message Tx {
  uint32   version    = 1;   // tx format version
  uint32   chain_id   = 2;   // 0=reserved/undefined, 1=testnet, 2=mainnet
  uint64   nonce      = 3;   // per-sender monotonic counter (replay protection)
  string   from       = 4;   // sender address
  string   to         = 5;   // recipient address
  int64    amount     = 6;   // in base units
  int64    fee        = 7;   // in base units
  int64    valid_from = 8;   // optional: min unix ts (not required)
  int64    valid_until= 9;   // optional: tx expiry
  bytes    pub_key    = 10;  // compressed ECDSA pubkey
  bytes    signature  = 11;  // ECDSA over canonical digest only
}
```

Canonical hash:
```
tx_digest = SHA256(Le32(version) || Le32(chain_id) || Le64(nonce) ||
                   from || to || Le64(amount) || Le64(fee) ||
                   Le64(valid_from) || Le64(valid_until) || pub_key)
tx_hash   = SHA256(tx_digest)
```
- Fields are **fixed-length binary** or **length-prefixed**; no string concatenation ambiguity.
- Signature covers `tx_digest` **only** (never the hash-of-hash, never a string).
- Address: introduce **versioned+checksummed** format (Base58Check-style) to prevent fund-burn and separate
  testnet/mainnet.
- Nonce = per-account counter; account state must include `Nonce`. Each accepted tx requires `tx.nonce == account.nonce+1`
  (or `== last_nonce+1` per account) → replay protection.

### Pillar 3 — Deterministic state transition
A single `StateTransition` module (pure function) applies block/txs to a `State` snapshot:

```
State apply(State s, Block b)` or:
(State, ValidationResult) ApplyBlock(State s, Block b, ConsensusParams p)
```

Guarantees:
- **No I/O inside the transition** — the transition only touches an in-memory `State` (account balances+nonces).
- **Atomic**: fully applied or fully rejected; never partially persisted.
- **Idempotent & flat**: same block + same genesis + same conensus params ⇒ identical resulting state hash.
- Compute a rolling `state_root` (e.g., Merkle root over ordered account fields) included in the block header. This is
  how distant nodes prove exact state equality without replaying everything.

### Pillar 4 — Header structure with state root + finality
```proto
message BlockHeader {
  uint32  version        = 1;
  uint32  chain_id       = 2;
  int64   height         = 3;
  int64   time_stamp     = 4;
  bytes   prev_hash      = 5;
  bytes   merkle_root    = 6;   // txs
  bytes   state_root     = 7;   // state after applying this block
  bytes   validator      = 8;   // consensus pubkey/address
  uint64  epoch/round    = 9;   // PoS-BFT view/round
  bytes   signature      = 10;  // validator signature of header
  // BFT commit fields (QC / 2f+1 signatures) added by consensus layer
}
message Block { BlockHeader header = 1; repeated Tx txs = 2; ... }
```

### Pillar 5 — Consensus: specified PoS-BFT, not bespoke
**Decision:** adopt a documented PoS-BFT family. Two viable options with different trade-offs:

| Option | Pros | Cons |
|--------|------|------|
| **A. Tendermint-style BFT (e.g., port via `CometBFT`/dep in separate process), app = UKC** | Mature, tested for years, finality in ~seconds, slashing/unjail, IBC-ready | Adds an operational dependency; requires state machine in same process/app boundary; heavier ops. |
| **B. In-process practical BFT (Rust-style HotStuff/Streamlet) hand-implemented** | No external dependency; full control | Must be specified + fuzz-tested + audited; high engineering + audit cost. |

**Recommendation:** start with **Option A** (an application built on a mature consensus engine) and keep a clean
`Consensus` interface so it can be swapped. Route/gateway: the core exposes a `ConsensusDriver` with
`Propose/Prevote/Precommit/Commit` or a higher `SubmitBlock/ValidateBlock` abstraction. Do **not** hand-roll
cryptography or a new BFT scheme without a formal spec and a dedicated security audit.

Validator selection: **weighted by stake** (locked stake in a staking module, with lock period and slashing for
equivocation). No more "delete all stakes and pick max random".

### Pillar 6 — Network security (P2P + API)
- **TLS 1.2+ / mTLS** for node-to-node; **TLS** for client APIs.
- **Node identity**: each node has a long-term key; peers sign handshake; peer registry keyed by `(chain_id, node_id)`.
- **Peer scoring** and connection caps; banned list; no auto-add of arbitrary callers.
- **Rate limiting** (per-IP, per-account) on all RPCs; max body/block size; max mempool size per sender.
- **P2P sync**: request blocks by `(height, hash)` ranges, validate every block (header, state transition, signatures)
  **before** persisting; disjoint-set/retry logic; **no blind inserts**.
- **Gossip**: txs/bids filtered and validated before relay, with per-peer credit/debit (anti-spam/sybil).

### Pillar 7 — Storage & recovery
- Replace vulnerable LiteDB version (upgrade or swap). Each change requires a **documented backup + migration**
  strategy (export/import tool, DB version tag, checksums).
- Single writer (the state machine) + read replicas for queries; block/state stored consistently (WAL or atomic file
  swap; never partial).
- Periodic **snapshots** of `state_root` + blocks; **restore from snapshot** fast-path for new nodes.
- Backup tool to dump DBs with checksums; documented recovery drill.

### Pillar 8 — Module map (target)
```
UbudKusCoin.Core                 => protocol, state machine, hashing, merkle, types (no I/O)
UbudKusCoin.Storage              => LiteDB (upgraded) or RocksDB/sharp; snapshots; migrations
UbudKusCoin.Consensus            => interface + drivers (gRPC to engine or in-proc)
UbudKusCoin.Network.P2P          => peers, handshake, sync, gossip, mTLS
UbudKusCoin.Api                  => gRPC (and optional REST/gRPC-Web) with middleware (authn, rate limit, validation)
UbudKusCoin.Wallet              => BIP-39/44, checksummed address, encrypted key storage, sign-only
UbudKusCoin.Indexer             => derived query/explorer data, reorg-safe
UbudKusCoin.Observability       => structured logs, OpenTelemetry metrics/traces, health
UbudKusCoin.KeyStore            => encrypted private key at rest (DPAPI/AES-GCM), never logs secrets
```

### Pillar 9 — Testnet/Mainnet separation
- `chain_id` in every tx, block header, and genesis; peers refuse mismatched `chain_id`.
- Plug-in **genesis JSON** (deterministic, fixing C1): fixed timestamp, fixed genesis validator set, fixed supply, all
  hashes reproducible by any node.
- Network config selects genesis file + params; testnet has a distinct `chain_id` and seeds; mainnet gated behind the
  security audit gate.

### Pillar 10 — Observability
- Structured logging (serilog) with no secrets; levels per subsystem.
- Metrics: block height, tps, mempool size, sync lag, consensus round, p2p peers, API latency (OpenTelemetry Prometheus).
- Health/readiness endpoints; crash-recovery logging; alert on divergence (state_root mismatch).

---

## 3. Key designs to land soon (in order)

| # | Design | Why first |
|---|--------|-----------|
| D1 | Canonical Tx format with nonce+chain_id, integer amounts | Blocks every security item |
| D2 | Deterministic state transition + state_root in header | Detects divergence; auditability |
| D3 | Deterministic genesis + chain ID selection | Testnet/mainnet separation |
| D4 | Consensus engine integration (driver) | Replaces fake PoS |
| D5 | P2P secure handshake + validated sync | Removes arbitrary remote code path |
| D6 | API middleware (TLS/authn/rate-limit) | Public exposure safety |
| D7 | Wallet key protection + checksummed address | Fund safety/UX |
| D8 | Storage upgrade, snapshots, migrations | Ops safety |

---

## 4. Trade-offs explicitly acknowledged

1. **Consensus engine dependency (CometBFT-style)** adds ops complexity but buys years of battle-testing. Hand-rolled
   BFT risks catastrophic consensus bugs. → Choose mature engine unless we fund a real audit of a new scheme.
2. **Integer base units** (vs double) lowers ergonomics of display but is non-negotiable for correctness.
3. **Deterministic state replay** costs speed (recompute state on restart) but is the cornerstone of auditability.
4. **Sidecar engine** (separate process) vs in-proc: in-proc is simpler to run; external engine is more tested. See D4.

---

## 5. What is explicitly out of scope for v1
- EVM/smart contracts, sharding, mn-bridge, token issuance, off-chain channels.
- "Marketing-ready" claims. The project only proceeds to testnet after the security audit gate in the roadmap.

