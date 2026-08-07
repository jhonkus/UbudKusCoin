using UbudKusCoin.Core.Hashing;

namespace UbudKusCoin.Core.Types;

/// <summary>
/// The node's world state: a set of accounts keyed by address, tagged with the
/// chain id it belongs to and the height/head hash of the block that produced
/// it. This type is deliberately pure (no I/O) and the state transition in
/// <see cref="StateTransition"/> works on a derived copy so it is atomic.
/// </summary>
public sealed class State
{
    private readonly Dictionary<string, Account> _accounts = new(StringComparer.Ordinal);
    private readonly Dictionary<string, StakePositionState> _stakes = new(StringComparer.Ordinal);

    public uint ChainId { get; }
    public long Height { get; private set; }
    public long TimeStamp { get; private set; }
    public byte[] Head { get; private set; } = Merkle.ZeroRoot;

    /// <summary>
    /// The current minimum fee-per-transaction required by the dynamic base fee model.
    /// Updated deterministically at the end of each block by <see cref="StateTransition"/>.
    /// </summary>
    public Money BaseFee { get; set; } = FeePolicy.BaseFee;

    public State(uint chainId, long height = 0, byte[]? head = null, long timeStamp = 0)
    {
        ChainId = chainId;
        Height = height;
        Head = head ?? Merkle.ZeroRoot;
        TimeStamp = timeStamp;
    }

    public Account? GetAccount(Address address)
        => _accounts.TryGetValue(address.Encoded, out var account) ? account : null;

    /// <summary>Creates the account if absent, otherwise returns the existing one.</summary>
    public Account EnsureAccount(Address address)
    {
        if (!_accounts.TryGetValue(address.Encoded, out var account))
        {
            account = new Account { Address = address, Balance = Money.Zero };
            _accounts[address.Encoded] = account;
        }

        return account;
    }

    public void SetAccount(Account account)
        => _accounts[account.Address.Encoded] = account;

    /// <summary>
    /// Advances the chain position after a successful apply. Internal because
    /// only the deterministic state transition may move the head.
    /// </summary>
    internal void Advance(long height, long timeStamp, byte[] head)
    {
        Height = height;
        TimeStamp = timeStamp;
        Head = head;
    }

    public IReadOnlyCollection<Account> Accounts => _accounts.Values;
    public IReadOnlyCollection<StakePositionState> Stakes => _stakes.Values;

    public StakePositionState? GetStake(Address address)
        => _stakes.TryGetValue(address.Encoded, out var stake) ? stake : null;

    public void SetStake(StakePositionState stake)
        => _stakes[stake.Address.Encoded] = stake;

    public bool RemoveStake(Address address)
        => _stakes.Remove(address.Encoded);

    /// <summary>
    /// Returns a deep copy of this state. Used by the state transition so a
    /// failed block never leaves a partially-applied state behind.
    /// </summary>
    public State Derive()
    {
        var copy = new State(ChainId, Height, Head, TimeStamp)
        {
            BaseFee = BaseFee
        };
        foreach (var pair in _accounts)
        {
            copy._accounts[pair.Key] = pair.Value.ShallowClone();
        }
        foreach (var pair in _stakes)
        {
            copy._stakes[pair.Key] = pair.Value.Clone();
        }

        return copy;
    }

    /// <summary>
    /// Deterministic Merkle root over all accounts, ordered by address string.
    /// This is the <c>state_root</c> embedded in each block header so distant
    /// nodes can prove exact state equality.
    /// </summary>
    public byte[] ComputeStateRoot()
    {
        var leaves = _accounts.Values
            .OrderBy(a => a.Address.Encoded, StringComparer.Ordinal)
            .Select(HashAccount)
            .ToArray();

        var accountRoot = Merkle.ComputeRoot(leaves);
        var stakeLeaves = _stakes.Values
            .OrderBy(s => s.Address.Encoded, StringComparer.Ordinal)
            .Select(HashStake)
            .ToArray();
        var stakeRoot = Merkle.ComputeRoot(stakeLeaves);
        return HashUtils.Sha256(accountRoot.Concat(stakeRoot).ToArray());
    }

    private static byte[] HashAccount(Account account)
    {
        using var ms = new MemoryStream();
        HashUtils.AppendLengthPrefixed(ms, account.Address.Encoded);
        HashUtils.AppendLe64(ms, (ulong)account.Balance.BaseUnits);
        HashUtils.AppendLe64(ms, account.Nonce);
        return HashUtils.Sha256(ms.ToArray());
    }

    private static byte[] HashStake(StakePositionState stake)
    {
        using var ms = new MemoryStream();
        HashUtils.AppendLengthPrefixed(ms, stake.Address.Encoded);
        HashUtils.AppendLengthPrefixed(ms, stake.PubKey);
        HashUtils.AppendLengthPrefixed(ms, stake.ConsensusPubKey);
        HashUtils.AppendLe64(ms, (ulong)stake.Amount.BaseUnits);
        HashUtils.AppendLe64(ms, unchecked((ulong)stake.BondedHeight));
        HashUtils.AppendLe64(ms, unchecked((ulong)stake.UnlockHeight));
        ms.WriteByte(stake.Jailed ? (byte)1 : (byte)0);
        return HashUtils.Sha256(ms.ToArray());
    }
}
