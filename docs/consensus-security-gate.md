# Consensus Security Gate

Stage 6 now has an explicit consensus-engine boundary and fail-closed
configuration. `CONSENSUS_ENGINE=development` uses the deterministic in-process
driver and is suitable only for local tests. A production node must use
`CONSENSUS_ENGINE=cometbft` and provide an absolute `COMETBFT_RPC_URL`.
Production startup also requires a valid CometBFT `/status` payload.
Startup retries the status check for `COMETBFT_STARTUP_TIMEOUT_SECONDS`
(default 60 seconds) to support container startup ordering without silently
falling back to the development driver.

The Core now exposes a deterministic application boundary for transaction
admission, proposal validation, and atomic finalization. The network transport
adapter and a real multi-process CometBFT cluster are still required before
this boundary can be called ABCI-compatible in production.

Transactions crossing that boundary use the bounded `TransactionCodec`; malformed,
oversized, and trailing bytes are rejected before application processing.

The repository now exposes the `tendermint.abci.ABCI` gRPC service using the
CometBFT v0.38 wire contract. `Info`, `CheckTx`, `PrepareProposal`,
`ProcessProposal`, `FinalizeBlock`, `Query`, and `Commit` are wired to the Core
state machine. Snapshot restore and vote extensions deliberately return
unsupported responses until implemented and tested.

`FinalizeBlock` now validates and persists the committed block through the
canonical chain store before returning its app hash, then resynchronizes the
application state machine from the persisted canonical state.

## Remaining Production Evidence

- Run a real multi-process CometBFT cluster with the application state machine.
- Run the reproducible test-only smoke harness under `deploy/cometbft/` first.
- Exercise validator failure, network partition, restart, delayed messages, and
  state recovery while recording finalized height and hash.
- Verify that finalized state cannot be reverted after restart or resynchronization.
- Review validator onboarding, key rotation, slashing evidence, and RPC access
  controls.
- Obtain an independent consensus and application security review before any
  public testnet or mainnet claim.

The adapter health check only proves that the configured CometBFT RPC endpoint
is reachable. It does not claim that block proposal, vote transport, commit
publication, or ABCI integration is complete.

When `CONSENSUS_ENGINE=cometbft`, the node also refuses to start the legacy
in-process minting loop. This is a safety stop, not a replacement for the
required ABCI application integration.
