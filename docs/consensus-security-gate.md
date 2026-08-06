# Consensus Security Gate

Stage 6 now has an explicit consensus-engine boundary and fail-closed
configuration. `CONSENSUS_ENGINE=development` uses the deterministic in-process
driver and is suitable only for local tests. A production node must use
`CONSENSUS_ENGINE=cometbft` and provide an absolute `COMETBFT_RPC_URL`.
Production readiness requires a valid CometBFT `/status` payload. The app opens
its ABCI socket first, then retries the status check for
`COMETBFT_STARTUP_TIMEOUT_SECONDS` (default 60 seconds) to avoid a circular
container startup dependency without silently falling back to the development
driver.

The Core now exposes a deterministic application boundary for transaction
admission, proposal validation, and atomic finalization. A length-prefixed ABCI
socket adapter and a four-validator multi-process CometBFT harness are
available; production key management and an independently operated network are
still required before this boundary can be called production-ready.

Transactions crossing that boundary use the bounded `TransactionCodec`; malformed,
oversized, and trailing bytes are rejected before application processing.

The repository now exposes the `tendermint.abci.ABCI` gRPC service using the
CometBFT v0.38 wire contract. `Info`, `CheckTx`, `PrepareProposal`,
`ProcessProposal`, `FinalizeBlock`, `Query`, and `Commit` are wired to the Core
state machine. Snapshot restore and vote extensions deliberately return
unsupported responses until implemented and tested.

`FinalizeBlock` now validates and persists the committed block through the
canonical chain store before returning its app hash, then resynchronizes the
application state machine from the persisted canonical state. External commit
retries are idempotent when the height, validator, transactions, and evidence
match the already accepted block.

State sync now exposes a versioned, SHA-256-verified snapshot format with
bounded chunks. Restore validates the offered app hash, state root, and
canonical head anchor before replacing local state. The local restore contract
and four-validator quorum harness are covered by tests. The reproducible
cross-node state-sync drill now resets validator 1, restores it from a trusted
snapshot, and verifies convergence with the source quorum.

The CometBFT genesis harness starts at `initial_height: 1`. The application
reports its internal genesis state as ABCI height `0`, allowing `InitChain` to
run once and making the first CometBFT block height `1`.

The repository includes a four-validator, multi-process harness that checks
shared genesis, proposer mapping, peer consensus, and restart recovery. Four
validators are intentional: three can keep quorum while one is offline for
state-sync testing. It is still test-only and does not provide production key
custody.

The repeatable partition drill has also been executed successfully: validator 1
was disconnected for eight seconds, validators 0, 2, and 3 retained quorum,
and all four nodes converged again with the same block hash.

## Remaining Production Evidence

- Run a production-shaped multi-process CometBFT cluster with managed keys.
- Run the reproducible test-only smoke harness under `deploy/cometbft/` first.
- Exercise validator failure and delayed messages while recording finalized
  height and hash. Restart recovery and a network partition drill are covered
  by the multi-validator harness; the repeatable partition script is available at
  `deploy/cometbft/test-multinode-partition.ps1`.
- Repeat the state-sync drill with rotated validator keys and a production-shaped
  snapshot retention policy before public testnet launch.
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
