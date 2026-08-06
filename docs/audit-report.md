# UbudKusCoin — Repository Audit & Threat Model

**Status:** Historical baseline audit (pre-hardening; retained for traceability)
**Date:** See git history
**Auditor:** Senior Blockchain Architect / Cryptography Engineer
**Scope:** `UbudKusCoin` (core node), `ConsoleWallet`, `BlockExplorer`, protobuf/gRPC definitions, config, deployment scripts.

> **Historical executive summary:** At the time of this audit, this codebase was a **learning prototype**, not a production blockchain. It compiled, but the
> "consensus" is not secure, the state transition is not deterministic, replay/fork protection is absent, the network
> layer is unauthenticated and unencrypted, and it carries a **known critical CVE in its storage dependency**. It must
> **not** be deployed to any real network until a full refactor, testnet validation, and independent security audit are
> completed. This document is **Step 1–2** of the roadmap; no code has been changed.

---

## 1. Build baseline

| Item | Result |
|------|--------|
| `dotnet build UbudKusCoin.sln -c Debug` | **Succeeds** (3 projects) |
| Test projects | **None exist** (`.gitignore` references a `/UnitTest` project that is absent) |
| Target framework | `net5.0` — **EOL**, no security patches (warning NETSDK1138) |
| Known vulnerable dependency | **LiteDB 5.0.10 — critical CVE** (warning NU1904, GHSA-3x49-g6rc-c284) |
| Package versions | gRPC 2.42, NBitcoin 4.2.16, Newtonsoft 13.0.1 — all dated/old |

---

## 2. Risk summary (by severity)

| # | Severity | Finding |
|---|----------|---------|
| C1 | **Critical** | Genesis block is **non-deterministic** (validator = node's own address), so each fresh node builds a *different* chain. Multi-node nodes can never agree on a common genesis. |
| C2 | **Critical** | "Consensus" is fake PoS: `AutoStake()` calls `StakeDb.DeleteAll()` then every node inserts a **random amount (10–100)** coin with **no balance/signature validation**. Validator = `GetMax()`. Trivially gameable; no randomness, no finality, no BFT. |
| C3 | **Critical** | `DownloadBlocks` inserts blocks into the DB **without any validation** (no hash, height, prev, or transaction checks). A malicious peer can inject arbitrary blocks and balances. |
| C4 | **Critical** | No replay/chain separation: transactions have **no nonce and no chain ID**; hash lacks separators (non-canonical). Same tx can be re-broadcast; testnet txs could be reusable on mainnet. |
| C5 | **Critical** | **Known critical CVE** in LiteDB 5.0.10 (GHSA-3x49-g6rc-c284). |
| H1 | **High** | gRPC is **plain HTTP, unauthenticated, no rate limiting**. Anyone can call `BlockService.Add`, `StakeService.Add`, `TransactionService.Transfer`, drain the mempool, or inject data. |
| H2 | **High** | Transaction/block state is not an atomic deterministic transition. `BlockService.Add` validates pool txs against *live* balance then mutates balances; concurrent blocks race. No fork-choice / no reorg handling. |
| H3 | **High** | **Double-spend window**: `IsValidTransfer` checks current DB balance, but a block can include txs and mutate balances without a serialized state check at that height. |
| H4 | **High** | Address = `Base58(SHA256(pubkey))` — **no version byte, no checksum**. Typos silently burn funds; no testnet/mainnet address tagging. |
| H5 | **High** | Private key material / mnemonic handled insecurely: node mnemonic from `NODE_PASSPHRASE` env (plaintext secret source); wallet **prints the full 12-word passphrase to console**; no encryption at rest; risk of secret leaking to logs. |
| M1 | **Medium** | `TransactionPoolFacade.TransactionExists` is dead code — always returns `false`. |
| M2 | **Medium** | `DbService` passes `DB_ACCOUNT` to `TransactionDb` (wrong DB wiring). |
| M3 | **Medium** | Mempool cleared with `DeleteAll()` on every block — drops txs not included in that block from other validators. |
| M4 | **Medium** | `GetRemaining` orders descending but P2P reverses; off-by-one race in sync (`> start` … `<= start+50`). |
| M5 | **Medium** | Floating point `double` used for all money (proto + C#). Precision loss is unacceptable for a currency. |
| M6 | **Medium** | No logging/metrics: `logging.ClearProviders()` disables all logging. No observability. |
| M7 | **Medium** | No backup, snapshot, recovery, or DB migration strategy. |
| L1 | **Low** | Errors broadly swallowed (`catch {}`); no structured error propagation. |
| L2 | **Low** | Difficulty hardcoded to `1`; PoW nonce is `rnd.Next(100000)` (removed `GetDifficullty()`); no difficulty logic. |
| L3 | **Low** | `Random` used for minting timing (`second >= 45`) — non-deterministic and not part of a consensus rule. |

---

## 3. Threat model

### 3.1 Assets
- **UTXO/account balances** (state) — the source of truth for value.
- **Private keys / mnemonics** — control over funds and validator identity.
- **Chain integrity** — hash-linked blocks that all honest nodes agree on.
- **Node availability** — ability to serve wallets/explorers and participate in consensus.

### 3.2 Trust boundaries
Deadline: a node runs by itself and in a small local cluster. There is currently **no trust distinction** between:
- a wallet submitting a transaction,
- a peer node broadcasting blocks/stakes,
- an anonymous remote client.

All three talk to the same unauthenticated gRPC surface. This is the primary boundary to fix.

### 3.3 Attacker model & scenarios
| Scenario | Exploit today | Impact |
|----------|---------------|--------|
| Sybil / fake stake | Any node stakes random 10–100 coins with no validation → becomes validator via `GetMax()`. | Chain takeover, minting arbitrary blocks. |
| Block injection | `DownloadBlocks` inserts unvalidated blocks. | Arbitrary balance manipulation. |
| Double spend | Replay same tx (no nonce); two validators produce competing blocks, both inserted. | Double-spend / balance inconsistency. |
| Reorg | No fork-choice rule; a late block overwriting/duplicating height is accepted. | Chain instability. |
| DoS | Unauthenticated gRPC, no rate limit. | Mempool/CPU/DB exhaustion. |
| Eavesdrop/tamper | Plain HTTP gRPC. | Transaction/block tampering in transit. |
| Fund burn | No address checksum. | User error loses funds permanently. |
| Secret leak | Passphrase printed to console; mnemonic from env. | Key compromise. |

---

## 4. Per-module findings

### 4.1 Core / consensus (`MintingService`, `Facade/BlockFacade`, `Facade/StakeFacade`)
- `MintingService.AutoStake()` **deletes all stakes** and inserts a random stake with no validation (comment confirms: *"I am not do balance validation, no signature validation"*).
- Validator selection = `StakeDb.GetMax()` → highest declared stake wins. No randomness (no VRF), no signatures, no slashing, no finality.
- `BlockFacade.New()` builds a block from the pool and signs it, but the block is **only validated by `IsValidBlock`** (height / prev-hash / hash / timestamp) on receipt — transaction state is not re-validated deterministically.
- Genesis `Validator` = the node's own address → **chain non-determinism** (C1).

### 4.2 Transaction format & validation (`Facade/TransactionFacade`, `Grpc/TransactionServiceImpl`)
- Hash = `SHA256(SHA256( time + sender + amount + fee + recipient ))` — non-canonical (no separators), no nonce, no chain ID, no version.
- `IsValidTransfer` checks: sender ≠ "-", amount/fee finite, amount>0, fee≥0, hash matches, signature valid, **balance ≥ amount+fee**. Balance check is against live DB, not a height-bound state.
- No limit on number/pool size → spam.
- Coinbase (`Validator_Fee`) is added with `Amount = totalFees` and credited to minter, but there is **no validation** that the coinbase amount equals the actual sum of fees in the block, nor a supply cap.

### 4.3 P2P / networking (`P2P/P2PService`, `Grpc/*`)
- `GrpcChannel.ForAddress` over plain HTTP (clients set `Http2UnencryptedSupport`).
- No node auth (no mTLS / no shared secret / no signature of node identity).
- `DownloadBlocks` trusts the peer entirely (C3).
- `PeerServiceImpl.Add` is a no-op; `GetNodeState` auto-adds any caller as a peer.

### 4.4 Storage (`Services/DbService`, `DB/*`)
- LiteDB with a **known critical CVE** (C5).
- Multiple DB files; `TransactionDb` is wired with the **account** database (M2).
- No indexes that enforce uniqueness on `Height`/`Hash` → duplicate/competing blocks possible.
- No backup/snapshot/recovery/migration (M7).

### 4.5 Wallet (`Services/WalletService`, `ConsoleWallet`)
- Node mnemonic from `NODE_PASSPHRASE` env.
- Address has no checksum/version (H4).
- Wallet prints mnemonic to console (`WalletInfo`).
- Private `ExtKey` kept in memory only; no encrypted persistence, no secure backup/recovery UX.

### 4.6 Explorer & API (`BlockExplorer`, `Startup`)
- gRPC Web + CORS `AllowAnyOrigin` → open surface.
- No auth, no rate limiting, no TLS.
- No structured logging/metrics.

---

## 5. What is good (to preserve)
- Clean separation of `Services` / `Facade` / `DB` / `Grpc` / `P2P` layers — a reasonable starting point for modularization.
- Protobuf-based RPC is a sane choice for a node API.
- HD wallet via NBitcoin mnemonics (BIP-39) is a solid base.
- ECDSA signing already present (NBitcoin `SignMessage`/`VerifyMessage`).
- Merkle root implementation is a reasonable base (needs test coverage).

---

## 6. Recommended remediation order
1. Freeze features; **do not ship** to any network.
2. Upgrade storage (LiteDB vulnerability) and target a supported .NET LTS.
3. Define canonical, versioned, integer-fixed-point transaction format with **nonce + chain ID**.
4. Make state transition **deterministic and atomic** (single-threaded state machine).
5. Replace fake PoS with a specified PoS-BFT scheme (or a mature engine) + proper validator selection.
6. Add TLS/mTLS, node auth, and rate limiting.
7. Add deterministic genesis + chain ID (testnet/mainnet separation).
8. Add tests (unit/integration/multi-node/fuzz/load), CI, observability, backup/snapshot/recovery.
9. Only after audit → testnet → mainnet.

Detailed remediation is in `docs/architecture-design.md` and `docs/implementation-roadmap.md`.
