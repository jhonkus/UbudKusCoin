using UbudKusCoin.Core.Hashing;

namespace UbudKusCoin.Core.Types;

/// <summary>
/// A minimal block for the deterministic state machine. The header carries the
/// chain id, height, previous hash, transaction merkle root, the post-apply
/// <see cref="StateRoot"/>, the validating account, and the coinbase subsidy
/// (<see cref="Reward"/>). The header hash binds all consensus-critical fields.
/// </summary>
public sealed class Block
{
    public uint Version { get; set; } = ChainInfo.TxVersion;
    public uint ChainId { get; set; } = ChainInfo.ChainIdUndefined;
    public long Height { get; set; }
    public long TimeStamp { get; set; } // unix seconds
    public byte[] PrevHash { get; set; } = Merkle.ZeroRoot;
    public byte[] MerkleRoot { get; set; } = Merkle.ZeroRoot; // over txs
    public byte[] StateRoot { get; set; } = Merkle.ZeroRoot;  // over accounts after apply
    public Address Validator { get; set; }
    public byte[] ValidatorPubKey { get; set; } = Array.Empty<byte>();
    public byte[] ValidatorSignature { get; set; } = Array.Empty<byte>();
    public Money Reward { get; set; } = Money.Zero; // coinbase subsidy to validator
    public List<Transaction> Txs { get; set; } = new();

    /// <summary>Compute the canonical header hash (binds all consensus fields).</summary>
    public byte[] ComputeHeaderHash()
    {
        using var ms = new MemoryStream();
        HashUtils.AppendLe32(ms, Version);
        HashUtils.AppendLe32(ms, ChainId);
        HashUtils.AppendLe64(ms, (ulong)Height);
        HashUtils.AppendLe64(ms, (ulong)TimeStamp);
        HashUtils.AppendLengthPrefixed(ms, PrevHash);
        HashUtils.AppendLengthPrefixed(ms, MerkleRoot);
        HashUtils.AppendLengthPrefixed(ms, StateRoot);
        HashUtils.AppendLengthPrefixed(ms, Validator.Encoded);
        HashUtils.AppendLe64(ms, (ulong)Reward.BaseUnits);
        return HashUtils.DoubleSha256(ms.ToArray());
    }

    public string ComputeHeaderHashHex() => Convert.ToHexStringLower(ComputeHeaderHash());

    public bool VerifyValidatorSignature()
    {
        try
        {
            if (ValidatorPubKey.Length is not (33 or 65) || ValidatorSignature.Length == 0)
            {
                return false;
            }

            var publicKey = new NBitcoin.PubKey(ValidatorPubKey);
            var expectedAddress = Address.FromPublicKey(Validator.Version, ValidatorPubKey);
            return expectedAddress.Encoded == Validator.Encoded
                && publicKey.Verify(new NBitcoin.uint256(ComputeHeaderHash()), ValidatorSignature);
        }
        catch
        {
            return false;
        }
    }
}
