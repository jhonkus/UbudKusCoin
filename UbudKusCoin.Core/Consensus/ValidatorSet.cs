using System.Buffers.Binary;
using UbudKusCoin.Core.Hashing;
using UbudKusCoin.Core.Types;

namespace UbudKusCoin.Core.Consensus;

public sealed record Validator(Address Address, byte[] PubKey, Money Stake);

public sealed class ValidatorSet
{
    private readonly IReadOnlyList<Validator> validators;
    private readonly Dictionary<string, Validator> byAddress;

    public ValidatorSet(IEnumerable<Validator> validators)
    {
        this.validators = validators
            .Where(x => x.Stake > Money.Zero)
            .OrderBy(x => x.Address.Encoded, StringComparer.Ordinal)
            .ToArray();
        byAddress = this.validators.ToDictionary(x => x.Address.Encoded, StringComparer.Ordinal);
        TotalStake = this.validators.Aggregate(Money.Zero, (total, validator) => total + validator.Stake);
        if (this.validators.Count == 0)
        {
            throw new ArgumentException("Validator set cannot be empty.", nameof(validators));
        }
    }

    public IReadOnlyList<Validator> Validators => validators;
    public Money TotalStake { get; }

    public bool TryGet(Address address, out Validator validator)
        => byAddress.TryGetValue(address.Encoded, out validator!);

    public Validator SelectProposer(uint chainId, long height, uint round)
    {
        using var stream = new MemoryStream();
        HashUtils.AppendLe32(stream, chainId);
        HashUtils.AppendLe64(stream, (ulong)height);
        HashUtils.AppendLe32(stream, round);
        foreach (var validator in validators)
        {
            HashUtils.AppendLengthPrefixed(stream, validator.Address.Encoded);
            HashUtils.AppendLe64(stream, (ulong)validator.Stake.BaseUnits);
        }

        var digest = HashUtils.Sha256(stream.ToArray());
        var sample = BinaryPrimitives.ReadUInt64LittleEndian(digest);
        var ticket = sample % (ulong)TotalStake.BaseUnits;
        ulong cursor = 0;
        foreach (var validator in validators)
        {
            cursor += (ulong)validator.Stake.BaseUnits;
            if (ticket < cursor)
            {
                return validator;
            }
        }

        return validators[^1];
    }
}
