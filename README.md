# UbudKusChain

UbudKusChain is a **production‑oriented proof‑of‑stake blockchain infrastructure** for business networks, verifiable records, digital assets, loyalty programs, vouchers, membership, organization applications, and ERP integrations.
The project is no longer positioned as a tutorial or toy chain: protocol rules, state transitions, staking, finality, CometBFT integration, persistence, and multi‑validator testing are being developed as a serious foundation for a future public testnet and mainnet.

**Positioning disclaimer:** UbudKusChain does **not** aim to replace the Indonesian Rupiah or any sovereign currency for retail transactions. The native UKSC token is intended solely for validator‑staking, network security, governance, and optional transaction‑fee mechanisms. Business applications built on top of the chain must comply with applicable local laws and regulations.


## Potential Use Cases

- **Community and local economies:** transparent issuance, staking, rewards, and governance for cooperatives, tourism communities, or digital membership.
- **Loyalty and utility assets:** auditable points or service credits with programmable issuance policies, usage caps, expiry, and redemption rules.
- **Business‑focused applications:** voucher issuance, membership management, organization identity, document & certificate verification, supply‑chain provenance, ERP audit trails, B2B invoice verification, and custom ERP integration.
- **Tokenized real-world projects:** a settlement layer for verified assets,
  provided legal custody, compliance, and issuer controls are implemented.
- **Public infrastructure:** an application-specific chain for organizations
  that need their own validator set and transparent transaction history.

Smart contracts and EVM compatibility are intentionally deferred. The first
release prioritizes a small, deterministic, auditable state machine over broad
feature count.

## Current Foundation

- .NET 10 and C# with a dependency-light deterministic Core state machine.
- Integer fixed-point amounts, versioned transactions, nonces, chain IDs,
  canonical hashing, Merkle roots, and deterministic genesis.
- Atomic block validation and persistence, staking lock/unbonding (`Bond`, `Unbond`, `Withdraw`), validator
  updates, slashing evidence, and sequential finality tracking.
- Storage backend migrated from LiteDB to LightningDB (LMDB) for improved
  performance, durability, and atomic snapshots.
- Storage backup and restore service (`StorageBackupService`) for database files, state, finality, and wallet vaults.
- CometBFT v0.38 ABCI 2.0 integration with four-validator local quorum, restart,
  partition/recovery, validator updates, and state-sync snapshot-restore drills.
- Validated external genesis manifest support for reproducible chain bootstrap;
  production manifests must be reviewed, hash-pinned, and distributed out of band.
- Explicit validator key custody policy with fail-closed local-file or external-signer modes.
- Signed validator consensus-key rotation with deterministic old-key removal and new-key activation.
- Encrypted wallet vault for seed storage at rest using AES-GCM and DPAPI (`WalletVault`).
- API middleware protection including CORS origin allowlist, API key authentication, per-IP rate limiting, and readiness health probes (`/health/ready`, `/health/consensus`).
- Comprehensive unit, integration, multi-node harness, and protocol mutation/fuzz testing (143/143 tests passing).
- Console wallet, explorer integration, gRPC APIs, and deployment harnesses.

## Status and Production Gate

Stage 6 core protocol and ABCI engine integration items are complete, alongside foundational Stage 7 (API middleware, rate limiting, health probes) and Stage 8 (encrypted wallet vault, LMDB storage backup/restore) features.

The repository has a strong protocol, consensus, and integration foundation, but it is **not yet approved for real funds or mainnet**. Remaining gates prior to production include:
- Node-to-node mTLS transport security.
- Comprehensive load/soak testing and reorg-safe explorer indexer.
- Public testnet deployment with faucet and operational runbooks (Stage 10).
- Independent security audit (Stage 11).

See [`docs/implementation-roadmap.md`](docs/implementation-roadmap.md) and [`docs/consensus-security-gate.md`](docs/consensus-security-gate.md).

## Quick Start

Requirements: .NET 10 SDK. Docker Desktop is required for the CometBFT
multi-validator harness.

For browser clients, configure `API_CORS_ORIGINS` with a comma-separated
allowlist such as `https://wallet.example.com`; if it is empty, cross-origin
browser requests are rejected by default. CORS is not authentication, so
public deployments also require network policy. Enable the API protection
settings below for the gRPC-Web/API port:

```text
API_AUTH_TOKEN=replace-with-a-secret-from-your-secret-manager
API_RATE_LIMIT_PER_MINUTE=120
API_TLS_CERT_PATH=C:\secrets\node-api.pfx
API_TLS_CERT_PASSWORD=loaded-from-secret-manager
```

`API_AUTH_TOKEN` is sent as the `X-API-Key` header. TLS and authentication
apply to the browser-facing gRPC-Web port; the native gRPC port remains
reserved for trusted node and server-side clients. Do not commit certificates,
passwords, or tokens.

The node also rejects gRPC messages larger than 1 MiB on receive and 4 MiB on
send. Keep explorer queries paginated rather than increasing these limits.

```powershell
dotnet restore
dotnet test UbudKusChain.sln --no-restore --nologo
```

Set the test wallet encryption key and start the multi-validator harness:

```powershell
$env:WALLET_ENCRYPTION_KEY = [Convert]::ToBase64String((0..31 | ForEach-Object { [byte]$_ }))
docker compose -f deploy/cometbft/docker-compose.multinode.yml up --build -d
```

Verify all four validators are running:

```powershell
Invoke-RestMethod http://localhost:26657/status
Invoke-RestMethod http://localhost:26658/status
Invoke-RestMethod http://localhost:26659/status
Invoke-RestMethod http://localhost:26660/status
```

Run the drill tests:

```powershell
powershell -ExecutionPolicy Bypass -File .\deploy\cometbft\test-multinode-partition.ps1
powershell -ExecutionPolicy Bypass -File .\deploy\cometbft\test-multinode-delayed-message.ps1
powershell -ExecutionPolicy Bypass -File .\deploy\cometbft\test-multinode-state-sync.ps1
powershell -ExecutionPolicy Bypass -File .\deploy\cometbft\test-multinode-validator-update.ps1
```

Stop the local harness and remove its test volumes with:

```powershell
docker compose -f deploy/cometbft/docker-compose.multinode.yml down -v
```

## Architecture and Contribution

The Core protocol is isolated from transport and infrastructure. The node
contains application services, persistence, gRPC/ABCI adapters, and wallet
components; CometBFT owns validator rounds, quorum, and finality. All protocol changes require tests and
must preserve deterministic state-root and replay behavior.
**Legal / Compliance Note:** Applications built on UbudKusChain must not present the native UKSC token as a retail payment method or legal tender. Business use‑cases should adhere to Indonesian financial regulations (Bank Indonesia, OJK) and any relevant consumer‑protection laws. The chain itself is intended solely as infrastructure for verifiable business processes.

## License

See [`THIRD-PARTY-NOTICES.md`](THIRD-PARTY-NOTICES.md) for dependency notices
and licensing information.
