# Consensus Engine Decision

## Decision

UbudKusChain will target a CometBFT-style mature BFT engine for production
consensus. The application state machine remains in `UbudKusChain.Core`; the
engine owns rounds, peer voting, quorum certificates, timeouts, and finality.

The current `DeterministicBftDriver` is a protocol and development driver. It
is useful for deterministic unit and local-cluster tests, but it is not a
production consensus implementation and must not be presented as audited.

## Boundary

- Core validates transactions, blocks, state roots, validator signatures, and
  application-level staking/slashing effects.
- The consensus engine proposes blocks, transports votes, forms commits, and
  reports finalized block hashes.
- The node persists finalized height/hash and refuses state rollback below the
  finalized checkpoint.

## Production Gate

Before testnet, the engine adapter needs a real multi-process test cluster,
partition and restart drills, evidence that finalized blocks cannot be
reverted, and an independent consensus/security review.

The current ABCI transport targets CometBFT v0.38 / ABCI 2.0. Its protobuf
contract must be upgraded deliberately when changing CometBFT minor versions;
CometBFT documents minor-version API changes as potentially breaking.
