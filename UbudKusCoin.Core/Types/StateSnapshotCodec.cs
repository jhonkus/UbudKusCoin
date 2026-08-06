using System.Text.Json;
using System.Text.Json.Serialization;
using UbudKusCoin.Core.Hashing;

namespace UbudKusCoin.Core.Types;

/// <summary>
/// Versioned state-sync payload. The canonical JSON shape is stable because
/// accounts and stakes are sorted before serialization; the state root is
/// checked again when a payload is imported.
/// </summary>
public static class StateSnapshotCodec
{
    public const uint Format = 1;

    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.General)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never
    };

    public static byte[] Encode(State state)
    {
        ArgumentNullException.ThrowIfNull(state);
        var snapshot = new Snapshot
        {
            Format = Format,
            ChainId = state.ChainId,
            Height = state.Height,
            TimeStamp = state.TimeStamp,
            Head = state.Head.ToArray(),
            StateRoot = state.ComputeStateRoot(),
            Accounts = state.Accounts
                .OrderBy(x => x.Address.Encoded, StringComparer.Ordinal)
                .Select(x => new AccountRecord
                {
                    Address = x.Address.Encoded,
                    Balance = x.Balance.BaseUnits,
                    Nonce = x.Nonce,
                    PubKey = x.PubKey.ToArray()
                }).ToList(),
            Stakes = state.Stakes
                .OrderBy(x => x.Address.Encoded, StringComparer.Ordinal)
                .Select(x => new StakeRecord
                {
                    Address = x.Address.Encoded,
                    PubKey = x.PubKey.ToArray(),
                    Amount = x.Amount.BaseUnits,
                    BondedHeight = x.BondedHeight,
                    UnlockHeight = x.UnlockHeight,
                    Jailed = x.Jailed
                }).ToList()
        };
        return JsonSerializer.SerializeToUtf8Bytes(snapshot, Options);
    }

    public static bool TryDecode(ReadOnlySpan<byte> encoded, out State? state, out string error)
    {
        state = null;
        error = string.Empty;
        try
        {
            var snapshot = JsonSerializer.Deserialize<Snapshot>(encoded, Options)
                ?? throw new InvalidDataException("Snapshot payload is empty.");
            if (snapshot.Format != Format || snapshot.Head.Length == 0 || snapshot.StateRoot.Length == 0)
                throw new InvalidDataException("Unsupported or incomplete snapshot format.");

            var restored = new State(snapshot.ChainId, snapshot.Height, snapshot.Head, snapshot.TimeStamp);
            foreach (var account in snapshot.Accounts)
            {
                var restoredAccount = restored.EnsureAccount(Address.Parse(account.Address));
                restoredAccount.Balance = new Money(account.Balance);
                restoredAccount.Nonce = account.Nonce;
                restoredAccount.PubKey = account.PubKey.ToArray();
            }
            foreach (var stake in snapshot.Stakes)
            {
                restored.SetStake(new StakePositionState
                {
                    Address = Address.Parse(stake.Address),
                    PubKey = stake.PubKey.ToArray(),
                    Amount = new Money(stake.Amount),
                    BondedHeight = stake.BondedHeight,
                    UnlockHeight = stake.UnlockHeight,
                    Jailed = stake.Jailed
                });
            }

            if (!restored.ComputeStateRoot().SequenceEqual(snapshot.StateRoot))
                throw new InvalidDataException("Snapshot state root does not match its contents.");

            state = restored;
            return true;
        }
        catch (Exception exception) when (exception is JsonException or InvalidDataException
            or FormatException or ArgumentException or OverflowException)
        {
            error = $"Invalid state snapshot: {exception.Message}";
            return false;
        }
    }

    public static byte[] ComputeHash(ReadOnlySpan<byte> encoded)
        => HashUtils.Sha256(encoded);

    private sealed class Snapshot
    {
        public uint Format { get; set; }
        public uint ChainId { get; set; }
        public long Height { get; set; }
        public long TimeStamp { get; set; }
        public byte[] Head { get; set; } = Array.Empty<byte>();
        public byte[] StateRoot { get; set; } = Array.Empty<byte>();
        public List<AccountRecord> Accounts { get; set; } = new();
        public List<StakeRecord> Stakes { get; set; } = new();
    }

    private sealed class AccountRecord
    {
        public string Address { get; set; } = string.Empty;
        public long Balance { get; set; }
        public ulong Nonce { get; set; }
        public byte[] PubKey { get; set; } = Array.Empty<byte>();
    }

    private sealed class StakeRecord
    {
        public string Address { get; set; } = string.Empty;
        public byte[] PubKey { get; set; } = Array.Empty<byte>();
        public long Amount { get; set; }
        public long BondedHeight { get; set; }
        public long UnlockHeight { get; set; }
        public bool Jailed { get; set; }
    }
}
