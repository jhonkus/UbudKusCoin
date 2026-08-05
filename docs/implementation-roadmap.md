# UbudKusCoin — Implementation Roadmap

**Status:** Plan (no code changes yet). Each stage is small, has a reason, tests, and a passing build. We do **not**
proceed to mainnet until the security audit gate (Stage 8) passes.

Rule: work one small stage per iteration; never migrate DB without backup/migration strategy; never claim
production-ready while critical gaps remain.

---

## Stage 0 — Baseline (DONE)
- [x] Read full repo & map dependencies.
- [x] `dotnet build UbudKusCoin.sln -c Debug` → succeeds, 3 projects.
- [x] Recorded build warnings (net5.0 EOL; LiteDB critical CVE).
- [x] Produced `docs/audit-report.md` (threat model) and `docs/architecture-design.md` (target).
- [ ] *(env-examples/.env* contain secrets; keep them out of git and never commit real secrets.)*

---

## Stage 1 — Foundation & dependency hygiene
**Why:** unblocks everything; removes known critical CVE and EOL runtime.
- Upgrade target framework to a supported LTS (e.g., `net8.0`).
- Resolve LiteDB critical CVE (upgrade to patched version or replace with RocksDB/sharp).
- Introduce `.editorconfig`, analyzers, and a `Directory.Build.props`.
- Add a build script + CI (GitHub Actions) that runs `dotnet build` + a placeholder test project.
- Add `docs/` index and a `SECURITY.md`.

**Exit criteria:** clean build with no critical advisories; CI green.

---

## Stage 2 — Core types & canonical hashing (no I/O)
**Why:** deterministic, canonical, replay-safe foundation.
- Add `Core` project: `Tx`, `BlockHeader`, `Block`, `Address`, `HashUtils`, `Merkle`, `UInt64 money`.
- Adopt integer fixed-point amounts (base units) everywhere in consensus types.
- Implement canonical `tx_digest`/`tx_hash` (nonce + `chain_id` + version, length-delimited fields).
- Implement checksummed/versioned address (Base58Check-style) for testnet/mainnet separation.

**Exit criteria:** unit tests for canonical hashing, merkle, address format, and money arithmetic.

---

## Stage 3 — Deterministic state transition engine
**Why:** atomicity + auditability; eliminates double-spend and non-deterministic genesis.
- Implement pure `State` (accounts: balance + nonce) and `StateTransition.ApplyBlock(s, block)`.
- Rule: `tx.nonce == account.nonce+1`; `amount+fee <= balance`; fee collection; coinbase checksum.
- Compute `state_root` (Merkle over ordered account fields) per block.
- Make genesis **deterministic** (fixed timestamp, fixed validator set, fixed supply) selected by `chain_id`.

**Exit criteria:** property tests: same input ⇒ same output; invalid cases rejected; no partial state.

---

## Stage 4 — Transaction validation & mempool (deterministic)
**Why:** validated, bounded, spam-resistant mempool.
- Validate tx envelopes (canonical hash, signature, nonce, chain_id, fee policy, size limits).
- Per-sender mempool caps + total cap; reject duplicates deterministically.
- Fees: base fee + optional tip; min relay fee; fee floor/ceiling in base units.
- Persist mempool safely (no blind `DeleteAll`).

**Exit criteria:** unit + integration tests for validation, dedup, bounds, and nonce ordering.

---

## Stage 5 — Block validation & atomic persistence
**Why:** nodes must never accept an invalid/partial block.
- Validate header (prev_hash, height, timestamp, merkle, state_root, validator signature, chain_id).
- Apply via `StateTransition`; on success persist block + resulting state atomically (single writer).
- Reject and quarantine invalid blocks; never trust a peer's block (`DownloadBlocks` fixed).
- Fork choice: longest/valid chain with highest committed finality (per consensus engine).

**Exit criteria:** integration tests: valid chain accepted; tampered/duplicate/reorg blocks rejected atomically.

---

## Stage 6 — Consensus engine (replace fake PoS)
**Why:** current PoS is insecure (random stakes, no finality).
- Wrap consensus behind `IConsensusDriver`: `Propose/Validate/Commit` + finality.
- **Option A (recommended):** integrate a mature engine (e.g., CometBFT-style) as the app/ABCI side.
- **Option B:** implement specified PoS-BFT (e.g., Streamlet/HotStuff) — only with a formal spec + fuzz + audit.
- Implement staking module: locked stake, weighted selection, lock period, slashing for equivocation; remove
  `AutoStake` random logic.

**Exit criteria:** multi-node tests showing finality, liveness under faults, and slashing.

---

## Stage 7 — Secure P2P & API middleware
**Why:** public-safe networking and API.
- TLS 1.2+/mTLS for node-node; node identity + signed handshake; peer scoring/caps; no auto-add.
- Validated sync by `(height, hash)`; request/retry logic.
- API middleware: TLS, authentication, per-IP/per-account rate limiting, max sizes, input validation.
- Structured logging (no secrets) + OpenTelemetry metrics/tracing + health/readiness.

**Exit criteria:** negative + load tests; no secret in logs; rate-limit enforced.

---

## Stage 8 — Wallet, storage, observability, recovery
**Why:** fund safety + ops safety.
- Wallet: BIP-39/44, checksummed address, **encrypted key storage at rest** (AES-GCM/DPAPI), never print mnemonic to
  logs/console, backup/recovery UX, sign-only (never send private key).
- Storage: upgrade vulnerable LiteDB (or swap), add snapshot + restore + migration with backup strategy.
- Indexer for explorer/query; reorg-safe.
- Full observability: metrics, dashboards, alerts (state_root divergence).

**Exit criteria:** wallet does not leak secrets; backup/restore drill passes; metrics emitted.

---

## Stage 9 — Testing & CI
**Why:** correctness evidence.
- Unit tests (Core/validation/state/hashing).
- Integration tests (single node APIs).
- **Multi-node tests** (local cluster: sync, consensus, reorg, partition).
- **Fuzzing** (tx/envelope/block parsing; state transition).
- **Load tests** (mempool, API, sync throughput).
- CI runs all suites + coverage gate + build for supported targets.

**Exit criteria:** all suites green; coverage threshold; fuzz finds no panic/invariant break.

---

## Stage 10 — Docs, deployment, testnet
**Why:** operational readiness for a real-but-isolated network.
- Protocol spec (canonical serialization, consensus rules, fees, staking).
- Operational runbooks: node setup, TLS certs, backups, snapshots, recovery, upgrades.
- Deploy **testnet** (distinct `chain_id`, genesis, seeds) with monitoring.
- Public testnet faucet + explorer + docs.

**Exit criteria:** a stable public testnet with monitoring and a documented incident process.

---

## Stage 11 — Security audit gate
**Why:** gate before any mainnet consideration.
- Independent security audit (consensus, crypto, state, network, storage, wallet).
- Threat-model review; address all Critical/High findings.
- Public disclosure-ready report.

**Exit criteria:** audits pass and Critical/High findings are resolved/waivered with evidence.

---

## Stage 12 — Mainnet (only after audit)
**Why:** the *only* path to a real network.
- Distinct mainnet `chain_id` + genesis; mainnet seed infra; operational runbooks final.
- Launch with a monitoring/alerting roster and a documented emergency response (pause/upgrade path).

**Exit criteria:** mainnet runs with observability; no claim of features beyond what is audited.

---

## How to sequence in this repo
- Each stage maps to a small, reviewable PR. Do not combine stages.
- Tests must be committed with the code they test.
- DB migrations only under Stage 8's backup/migration strategy.
- Use the `docs/*.md` files as living documents updated as stages land.

**Current position: Stage 0 complete. Awaiting approval to begin Stage 1.**
