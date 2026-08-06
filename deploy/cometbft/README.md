# CometBFT Smoke Harness

This directory contains test-only CometBFT harnesses. They are not production
deployments: they use a development mnemonic, generated validator keys, and
anonymous local ports.

## Run

From the repository root:

```powershell
$env:WALLET_ENCRYPTION_KEY = [Convert]::ToBase64String((0..31 | ForEach-Object { [byte]$_ }))
docker compose -f deploy/cometbft/docker-compose.yml up --build
```

The multi-node compose file also requires this environment variable. It is a
test-only key for the development vault and must never be reused in production.

In another terminal, verify both processes:

```powershell
Invoke-RestMethod http://localhost:26657/status
Invoke-RestMethod http://localhost:5001/health/consensus
```

The application exposes both the public gRPC surface and the CometBFT ABCI
socket transport. CometBFT v0.38 connects to `tcp://ukc-app:26658`; the public
HTTP/gRPC port is not used for consensus transport.
The smoke genesis includes the deterministic UKC genesis app hash so CometBFT
handshake replay can verify the initial state.
The app reads only the generated CometBFT validator public key from the shared
test volume so `InitChain` returns the validator set expected by CometBFT. Do
not use this shared-volume key arrangement for production key management.
The application waits up to `COMETBFT_STARTUP_TIMEOUT_SECONDS` for the engine
RPC and never falls back to the in-process driver.

## Genesis manifest

The Compose testnet mounts `genesis-manifest.testnet.json` into each application
container and sets `GENESIS_MANIFEST_PATH`. The application validates the chain
ID, timestamp, validator key, account keys, duplicate accounts, and balances
before constructing the canonical chain. `GENESIS_MANIFEST_SHA256` can pin the
exact bytes before parsing. Production deployments must replace this fixture
with a reviewed, hash-pinned manifest distributed out of band; do not put
validator private keys in the manifest.

## Limitations

- This validates process wiring and ABCI reachability only.
- It does not prove multi-validator quorum, partition recovery, or production
  key management.
- Delete the test volumes after the run with `docker compose ... down -v`.

## Multi-process smoke test

The four-validator harness uses CometBFT's generated testnet topology and four
independent application processes. Four validators provide a realistic local
quorum: one validator can be unavailable while the remaining three continue
committing blocks.

```powershell
docker compose -f deploy/cometbft/docker-compose.multinode.yml up --build -d
Invoke-RestMethod http://localhost:26657/status
Invoke-RestMethod http://localhost:26658/status
Invoke-RestMethod http://localhost:26659/status
Invoke-RestMethod http://localhost:26660/status
docker compose -f deploy/cometbft/docker-compose.multinode.yml down -v
```

This validates process isolation, shared genesis, validator proposer mapping,
ABCI finalization, and peer consensus. It remains a development harness and
does not replace a production key-management or network-failure test plan.

## Validator key rotation drill

Run the end-to-end Bond plus RotateValidatorKey drill with:

```powershell
powershell -ExecutionPolicy Bypass -File .\deploy\cometbft\test-multinode-validator-update.ps1
```

The drill resets volumes, commits a staking transaction, generates a new
Ed25519 key, switches the test validator signer, waits for the CometBFT update
to become effective, and verifies
the old key has zero power while the new key is active. It uses one base unit
of stake so the three remaining genesis validators retain quorum.

To verify that the rotated identity survives a fresh state-sync restore, run:

```powershell
powershell -ExecutionPolicy Bypass -File .\deploy\cometbft\test-multinode-rotation-state-sync.ps1
```

This combined drill performs rotation first, restores validator 1 through
state-sync, and verifies the rotated key remains active afterward.

## Partition recovery drill

With Docker Desktop running, execute the repeatable network fault injection:

```powershell
powershell -ExecutionPolicy Bypass -File .\deploy\cometbft\test-multinode-partition.ps1
```

The script disconnects validator 1 from the Compose network for eight seconds,
while validators 0, 2, and 3 retain quorum. It reconnects validator 1 and waits
for all four nodes to report the same height and latest block hash. It is
intentionally test-only and should not be run against a production network.

## State-sync drill boundary

The application implements deterministic snapshot listing, stable chunk
transfer, hash verification, and canonical restore. Run the reproducible
cross-node drill with:

```powershell
powershell -ExecutionPolicy Bypass -File .\deploy\cometbft\test-multinode-state-sync.ps1
```

The drill resets validator 1, obtains a trusted height/hash from validator 0,
enables state sync, and verifies that the joining validator converges with the
three-validator source quorum. Four validators are required because stopping
one node in a two-validator network removes the 2/3 quorum needed to finalize
the trusted light block.

## Delayed-message recovery drill

Run the delayed-message drill with:

```powershell
powershell -ExecutionPolicy Bypass -File .\deploy\cometbft\test-multinode-delayed-message.ps1
```

It disconnects validator 1 for 20 seconds, confirms the three-node quorum
continues finalizing, then verifies the rejoined node converges to the same
height and block hash. This is a local fault drill, not a substitute for a
production network chaos plan.

## Validator key boundary

CometBFT private validator keys remain owned by CometBFT. The application reads
only the public key, requires exactly 32 Ed25519 bytes, and rejects a key that
is not present in the local genesis validator set. A production deployment
still needs an external secret manager or HSM, audited key rotation, backup,
and recovery procedures; this repository does not implement those services.
Set `VALIDATOR_KEY_CUSTODY_MODE=external-signer` together with a
`COMETBFT_PRIV_VALIDATOR_LADDR=tcp://...` endpoint when CometBFT is configured
to delegate signing. In this mode also set
`COMETBFT_VALIDATOR_PUBKEY_HEX` to the exact 32-byte public key returned by the
signer. The application will not read `priv_validator_key.json`, and it
validates the configured identity against genesis. The application does not
implement the external signer protocol; CometBFT and the signer remain the
only components responsible for signing.

Consensus key rotation is authorized by the stake owner's secp256k1
transaction signature. The committed state removes the old Ed25519 key and
activates the new key; the validator update builder emits both changes
deterministically. Rotation still requires an operational signer rollout and
recovery plan.
