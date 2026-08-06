# CometBFT Smoke Harness

This directory contains test-only CometBFT harnesses. They are not production
deployments: they use a development mnemonic, generated validator keys, and
anonymous local ports.

## Run

From the repository root:

```powershell
docker compose -f deploy/cometbft/docker-compose.yml up --build
```

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
