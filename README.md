# UbudKusCoin

UbudKusCoin is a production-oriented proof-of-stake blockchain platform for
digital payments, community economies, and asset-backed applications. The
project is no longer positioned as a tutorial or toy chain: protocol rules,
state transitions, staking, finality, CometBFT integration, persistence, and
multi-validator testing are being developed as a serious foundation for a
future public testnet and mainnet.

## Potential Use Cases

- **Payments and merchant settlement:** fast, deterministic finality with a
  wallet and explorer integration path.
- **Community and local economies:** transparent issuance, staking, rewards,
  and governance for cooperatives, tourism communities, or digital membership.
- **Loyalty and utility assets:** auditable points or service credits with
  programmable issuance policies.
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
- Atomic block validation and persistence, staking lock/unbonding, validator
  updates, slashing evidence, and sequential finality tracking.
- Storage backend migrated from LiteDB to LightningDB (LMDB) for improved
  performance and durability.
- CometBFT v0.38 ABCI integration with four-validator local quorum, restart,
  partition/recovery, and snapshot-restore tests.
- Validated external genesis manifest support for reproducible chain bootstrap;
  production manifests must be reviewed, hash-pinned, and distributed out of band.
- Explicit validator key custody policy with fail-closed local-file or external-signer modes.
- Signed validator consensus-key rotation with deterministic old-key removal and new-key activation.
- Console wallet, explorer integration, gRPC APIs, and deployment harnesses.

## Status and Production Gate

Stage 6 is in progress. The repository has a strong protocol and integration
foundation, but it is **not yet approved for real funds or mainnet**. Remaining
gates include cross-node state-sync evidence, secure node/API transport,
encrypted key custody, observability, backup and migration runbooks, fuzz/load
testing, public testnet operation, and an independent security audit. See
[`docs/implementation-roadmap.md`](docs/implementation-roadmap.md) and
[`docs/consensus-security-gate.md`](docs/consensus-security-gate.md).

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
dotnet test UbudKusCoin.sln --no-restore --nologo
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
components; CometBFT owns validator rounds, quorum, and finality. Read
[`AGENTS.md`](AGENTS.md) for repository conventions and the roadmap before
changing consensus or monetary logic. All protocol changes require tests and
must preserve deterministic state-root and replay behavior.

## License

See [`THIRD-PARTY-NOTICES.md`](THIRD-PARTY-NOTICES.md) for dependency notices
and licensing information.
