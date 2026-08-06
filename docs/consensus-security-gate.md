# Consensus Security Gate

Stage 6 now has an explicit consensus-engine boundary and fail-closed
configuration. `CONSENSUS_ENGINE=development` uses the deterministic in-process
driver and is suitable only for local tests. A production node must use
`CONSENSUS_ENGINE=cometbft` and provide an absolute `COMETBFT_RPC_URL`.

## Remaining Production Evidence

- Run a real multi-process CometBFT cluster with the application state machine.
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
