# UbudKusCoin — Implementation Roadmap

**Status:** In progress. Each stage is small, has a reason, tests, and a passing build. We do **not**
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

## Stage 1 — Foundation & dependency hygiene (DONE)
**Why:** unblocks everything; removes known critical CVE and EOL runtime.
- [x] Upgraded all projects to a supported LTS: `net10.0` (was EOL `net5.0`).
- [x] Resolved LiteDB critical CVE: upgraded `LiteDB` 5.0.10 → **5.0.21** (patched).
- [x] Bumped outdated deps: Grpc 2.83, NBitcoin 10.0.7, Newtonsoft 13.0.4, DotNetEnv 3.2.0, Systemd/ConfigurationManager 10.0.10.
- [x] Disabled trim (`PublishTrimmed=false`) in core — reflection-based serializers/gRPC generated code are not trim-safe yet.
- [x] Added `UbudKusCoin.Tests` xUnit project (net10.0) with initial wallet-signature tests; wired into `UbudKusCoin.sln`.
- [x] `.gitignore` updated for test project bin/obj and temporary API probes.
- [ ] Remaining from plan: `.editorconfig`/analyzers/`Directory.Build.props`, CI workflow, `SECURITY.md`, docs index.

**Exit criteria:** clean build with no critical advisories; tests green. **Achieved:** `dotnet build` = 0 warnings; `dotnet test` = 4/4 pass.

---

## Stage 2 — Core types & canonical hashing (no I/O) (DONE)
**Why:** deterministic, canonical, replay-safe foundation.
- [x] Added dependency-free `UbudKusCoin.Core` project (net10.0, no gRPC/I/O); wired into solution.
- [x] `Money` — integer fixed-point in base units (1 UKC = 1e8 base units); no `double`; exact add/sub, rejects negative.
- [x] `HashUtils` — SHA-256/double-SHA-256, canonical little-endian + length-prefixed serializers.
- [x] `Address` — versioned + checksummed (Base58Check-style), mainnet/testnet version separation.
- [x] `Merkle` — deterministic binary tree, odd-leaf duplication, zero root.
- [x] `ChainInfo` — Tx version + chain IDs (mainnet/testnet/undefined).
- [x] `Transaction` — versioned envelope with `nonce` + `chain_id`, integer amounts/fees, canonical `ComputeDigest`/`ComputeId` (signature excluded from digest).

**Exit criteria:** unit tests for canonical hashing, merkle, address format, and money arithmetic. **Achieved:** 25/25 tests pass (Money, Address, Merkle, Transaction canonical hash).

---

## Stage 3 — Deterministic state transition engine (DONE)
**Why:** atomicity + auditability; eliminates double-spend and non-deterministic genesis.
- [x] `Account` (balance + nonce) and `State` (account set, chain id, height, head, `ComputeStateRoot`).
- [x] `Block` — canonical header hash binding chain_id, height, prev_hash, merkle_root, state_root, validator, reward.
- [x] `StateTransition` — pure/atomic `ApplyBlock` (works on a derived copy; never mutates input; rejects invalid nonce/balance/chain/height/prev-hash/state-root). `ComputeResultingState` for block builders.
- [x] `Genesis` — **deterministic** genesis state + block keyed by `chain_id` (fixed timestamp, fixed validator set, fixed supply); removes audit finding C1.

**Exit criteria:** property tests: same input ⇒ same output; invalid cases rejected; no partial state. **Achieved:** 40/40 tests pass (added `StateTransitionTests` + `GenesisTests`).

---

## Stage 4 — Transaction validation & mempool (deterministic) (DONE)
**Why:** validated, bounded, spam-resistant mempool.
- [x] Validate tx envelopes (canonical hash, signature, nonce, chain_id, fee policy, size limits).
- [x] Per-sender mempool caps + total cap; reject duplicates deterministically.
- [x] Fees: base fee + optional tip; min relay fee; fee floor/ceiling in base units.
- [x] Persist mempool safely (no blind `DeleteAll`).

**Exit criteria:** unit + integration tests for validation, dedup, bounds, and nonce ordering. **Achieved:** `dotnet build` = 0 warnings; `dotnet test` = 67/67 pass.

---

## Stage 5 — Block validation & atomic persistence
**Why:** nodes must never accept an invalid/partial block.
- [x] Validate legacy headers, merkle root, validator signature, coinbase, transaction signatures, balances, totals, and duplicates.
- [x] Serialize block commits through one writer and compensate with rollback on persistence failure.
- [x] Reject invalid peer blocks during gRPC receive, minting, and `DownloadBlocks` sync.
- [x] Apply via `UbudKusCoin.Core.StateTransition` and validate a canonical state root.
- [x] Quarantine invalid blocks and choose the longest valid fork deterministically.
- [x] Persist canonical blocks/state atomically and wire gRPC, P2P block sync, and minting to the Core protocol.

**Exit criteria:** integration tests for valid chains, tampered/duplicate/reorg blocks, and atomic rejection. Achieved with
canonical two-node exchange/rejection tests and crash-safe snapshot rebuild tests. Finality remains a Stage 6 consensus concern.

---

## Stage 6 — Consensus engine (replace fake PoS)
**Why:** current PoS is insecure (random stakes, no finality).
- [x] Wrap the protocol behind `IConsensusDriver`: proposer selection, proposal validation, vote, and commit boundary.
- [x] Add deterministic stake-weighted proposer selection, signed votes, `2/3+1` quorum certificates, and equivocation evidence.
- [x] Add legacy staking lock/unbonding rules, validator jail state, slashing, and sequential finality tracking.
- [x] Add `VALIDATOR_SET` configuration and runtime proposer gating; remove random `AutoStake` minting.
- **Option A (recommended):** integrate a mature engine (e.g., CometBFT-style) as the app/ABCI side.
- **Option B:** implement specified PoS-BFT (e.g., Streamlet/HotStuff) — only with a formal spec + fuzz + audit.
- [ ] Choose and integrate a mature engine (recommended) or complete a formally specified in-process BFT driver.
- [x] Implement legacy staking module: locked stake, weighted selection, lock period, slashing for equivocation; remove
  `AutoStake` random logic from runtime.
- [x] Transport signed votes between nodes and persist finalized heights atomically.
- [x] Add multi-node quorum tests, conflicting-vote rejection, and below-quorum liveness checks.
- [x] Add partition/reconnect and round-change progress tests.
- [x] Add an explicit consensus-engine adapter boundary and fail-closed
  `CONSENSUS_ENGINE`/`COMETBFT_RPC_URL` configuration.
- [x] Disable legacy local minting whenever an external consensus engine is configured.
- [x] Require a valid external-engine status response before production startup.
- [x] Add a deterministic Core application boundary for `CheckTx`, proposal
  validation, and atomic finalize/app-hash computation.
- [x] Add a bounded, versioned binary transaction codec for the application
  boundary; no JSON/reflection serialization in consensus bytes.
- [x] Add a CometBFT v0.38 ABCI 2.0 gRPC surface for core transaction and
  finalize methods; vote extensions remain explicitly unsupported.
- [x] Add versioned, chunked, hash-verified ABCI state snapshots with canonical
  head restore validation.
- [x] Persist ABCI external finalization through the canonical chain store and
  resynchronize the application state machine after commit.
- [x] Add the length-prefixed ABCI socket transport, `initial_height: 1`
  genesis configuration, validator bootstrap, and readiness ordering.
- [x] Add a pinned, test-only CometBFT container smoke harness.
- [x] Add a four-validator, multi-process CometBFT harness with proposer mapping
  and restart recovery verification, sized for quorum-preserving state-sync drills.
- [x] Add signed KTX2 staking transactions (`Bond`, `Unbond`, `Withdraw`) with
  deterministic lock-period rules and stake positions committed into `state_root`.
- [x] Emit CometBFT validator-set updates from committed staking state and
  resolve secp256k1 proposer addresses after validator activation.

**Exit criteria:** multi-node tests showing finality, liveness under faults, and slashing. Protocol-level finality and slashing
tests pass; runtime engine integration and fault/liveness tests remain.

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

**Current position: Stage 6 in progress (protocol, proposer gating, vote transport, finality persistence, quorum, partition,
round-change tests, CometBFT ABCI integration, multi-process smoke/restart verification, deterministic on-chain staking
transactions, CometBFT validator updates, restart recovery, network-partition fault injection, and verified local
snapshot restore landed). Next: cross-node state-sync drill on the four-validator harness, validator-update integration tests, delayed-message testing,
fuzzing, and independent consensus/security review. See
`docs/consensus-security-gate.md`.**
