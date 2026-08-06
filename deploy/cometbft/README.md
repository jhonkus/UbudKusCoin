# CometBFT Smoke Harness

This directory is a test-only, single-node smoke harness. It is not a testnet
or production deployment: it uses a development mnemonic, one validator, and
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

The application must expose its ABCI gRPC service before CometBFT can progress.
The application waits up to `COMETBFT_STARTUP_TIMEOUT_SECONDS` for the engine
RPC and never falls back to the in-process driver.

## Limitations

- This validates process wiring and ABCI reachability only.
- It does not prove multi-validator quorum, partition recovery, or production
  key management.
- Delete the test volumes after the run with `docker compose ... down -v`.
