namespace UbudKusCoin.Core.Types;

/// <summary>
/// A deterministic genesis definition. Every node on a given chain id derives
/// the exact same genesis state (and thus the same genesis state root) from
/// fixed parameters — no node identity or wall-clock time is involved. This
/// removes the audit finding C1 (non-deterministic genesis) and separates
/// testnet/mainnet via <see cref="ChainInfoChainId"/>.
/// </summary>
public static class Genesis
{
    /// <summary>Fixed genesis timestamp (unix seconds), identical on every node.</summary>
    public const long GenesisTime = 1_700_000_000L;

    /// <summary>Genesis block version.</summary>
    public const uint Version = ChainInfo.TxVersion;

    /// <summary>Genesis coinbase reward to the initial validator.</summary>
    public static readonly Money InitialReward = Money.FromCoins(0m);

    /// <summary>
    /// Builds the deterministic genesis state for the given chain id. The set of
    /// genesis accounts and their balances are fixed constants (no randomness).
    /// The genesis validator is included as an account so applying the genesis
    /// block (coinbase) reproduces the same state root.
    /// </summary>
    public static State CreateState(uint chainId)
    {
        var (accounts, _) = GenesisAccounts(chainId);
        var state = new State(chainId, height: 0, head: Merkle.ZeroRoot);

        foreach (var (address, balance) in accounts)
        {
            var account = state.EnsureAccount(address);
            account.Balance = balance;
            account.Nonce = 0;
        }

        return state;
    }

    /// <summary>
    /// Creates the genesis block that establishes the chain from the genesis
    /// state. The block has no transactions; its state root equals the genesis
    /// state root (the validator is already included in the state). The header
    /// hash is deterministic for a given chain id.
    /// </summary>
    public static Block CreateBlock(uint chainId)
    {
        var (_, validator) = GenesisAccounts(chainId);
        var state = CreateState(chainId);

        var block = new Block
        {
            Version = Version,
            ChainId = chainId,
            Height = 1,
            TimeStamp = GenesisTime,
            PrevHash = Merkle.ZeroRoot,
            MerkleRoot = Merkle.ComputeRoot(Array.Empty<byte[]>()),
            Validator = validator,
            Reward = InitialReward,
            Txs = new List<Transaction>(),
        };
        block.StateRoot = state.ComputeStateRoot();
        return block;
    }

    /// <summary>
    /// Fixed genesis accounts per chain id. Each entry is (address, initial
    /// balance). Addresses are derived from fixed public keys so they are
    /// reproducible; balances are in coin units converted to fixed-point.
    /// The validator is the first genesis account.
    /// </summary>
    private static (AccountSpec[] accounts, Address validator) GenesisAccounts(uint chainId)
    {
byte version = ChainInfo.AddressVersion(chainId);

        // Deterministic account public keys (fixed content, not secrets).
        // These are public halves of deterministic testnet fixtures. They are
        // valid secp256k1 keys so integration tests can sign genesis-account
        // transactions without inventing an invalid address format.
        var pub1 = Convert.FromHexString(
            "0279be667ef9dcbbac55a06295ce870b07029bfcdb2dce28d959f2815b16f81798");
        var pub2 = Convert.FromHexString(
            "02c6047f9441ed7d6d3045406e95c07cd85c778e4b8cef3ca7abac09b95c709ee5");

        var validator = Address.FromPublicKey(version, pub1);

        return (new[]
        {
            new AccountSpec(Address.FromPublicKey(version, pub1), Money.FromCoins(2_000_000_000m)),
            new AccountSpec(Address.FromPublicKey(version, pub2), Money.FromCoins(3_000_000_000m)),
        }, validator);
    }

    private sealed record AccountSpec(Address Address, Money Balance);
}
