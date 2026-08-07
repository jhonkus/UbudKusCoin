using System;
using System.Collections.Generic;
using System.Linq;
using UbudKusCoin.Core.Signing;
using UbudKusCoin.Core.Types;

namespace UbudKusCoin.Sdk;

/// <summary>
/// Helper class to construct and sign transactions offline.
/// </summary>
public sealed class TransactionBuilder
{
    private readonly uint _chainId;
    private TransactionKind _kind = TransactionKind.Transfer;
    private string _from = string.Empty;
    private string _to = string.Empty;
    private Money _amount = Money.Zero;
    private Money _fee = FeePolicy.BaseFee;
    private ulong _nonce = 0;
    private long _lockPeriod = 0;
    private long _validFrom = 0;
    private long _validUntil = 0;
    private byte[] _pubKey = Array.Empty<byte>();
    private byte[] _validatorPubKey = Array.Empty<byte>();

    public TransactionBuilder(uint chainId)
    {
        _chainId = chainId;
    }

    public TransactionBuilder SetTransfer(string from, string to, Money amount)
    {
        _kind = TransactionKind.Transfer;
        _from = from;
        _to = to;
        _amount = amount;
        return this;
    }

    public TransactionBuilder SetBond(string from, Money amount, byte[] validatorPubKey)
    {
        _kind = TransactionKind.Bond;
        _from = from;
        _to = from;
        _amount = amount;
        _validatorPubKey = validatorPubKey;
        return this;
    }

    public TransactionBuilder SetUnbond(string from, long lockPeriod)
    {
        _kind = TransactionKind.Unbond;
        _from = from;
        _to = from;
        _amount = Money.Zero;
        _lockPeriod = lockPeriod;
        return this;
    }

    public TransactionBuilder SetWithdraw(string from)
    {
        _kind = TransactionKind.Withdraw;
        _from = from;
        _to = from;
        _amount = Money.Zero;
        return this;
    }

    public TransactionBuilder SetRotateValidatorKey(string from, byte[] newValidatorPubKey)
    {
        _kind = TransactionKind.RotateValidatorKey;
        _from = from;
        _to = from;
        _amount = Money.Zero;
        _validatorPubKey = newValidatorPubKey;
        return this;
    }

    public TransactionBuilder SetFee(Money fee)
    {
        _fee = fee;
        return this;
    }

    public TransactionBuilder SetNonce(ulong nonce)
    {
        _nonce = nonce;
        return this;
    }

    public TransactionBuilder SetValidity(long validFrom, long validUntil)
    {
        _validFrom = validFrom;
        _validUntil = validUntil;
        return this;
    }

    public TransactionBuilder SetPubKey(byte[] pubKey)
    {
        _pubKey = pubKey;
        return this;
    }

    /// <summary>
    /// Builds an unsigned transaction.
    /// </summary>
    public Transaction BuildUnsigned()
    {
        if (string.IsNullOrWhiteSpace(_from)) throw new InvalidOperationException("From address is required.");
        if (string.IsNullOrWhiteSpace(_to)) throw new InvalidOperationException("To address is required.");

        return new Transaction
        {
            Version = ChainInfo.TxVersion,
            ChainId = _chainId,
            Kind = _kind,
            From = Address.Parse(_from),
            To = Address.Parse(_to),
            Amount = _amount,
            Fee = _fee,
            Nonce = _nonce,
            LockPeriod = _lockPeriod,
            ValidFrom = _validFrom,
            ValidUntil = _validUntil,
            PubKey = _pubKey,
            ValidatorPubKey = _validatorPubKey,
            Signature = Array.Empty<byte>()
        };
    }

    /// <summary>
    /// Builds and signs a standard single-signature transaction.
    /// </summary>
    public Transaction BuildAndSign(byte[] privateKeyBytes)
    {
        // Auto-populate PubKey from private key if not set.
        if (_pubKey == null || _pubKey.Length == 0)
        {
            var key = new NBitcoin.Key(privateKeyBytes);
            _pubKey = key.PubKey.ToBytes();
        }

        var tx = BuildUnsigned();
        tx.Signature = TransactionSigner.Sign(tx, privateKeyBytes);
        return tx;
    }

    /// <summary>
    /// Builds and signs a multi-signature transaction using the gathered signatures.
    /// </summary>
    public Transaction BuildAndSignMultiSig(
        uint threshold,
        IEnumerable<byte[]> allPublicKeys,
        IEnumerable<byte[]> signaturesOfSigners)
    {
        // For MultiSig, PubKey parameter can represent any public key of a signer
        if (_pubKey == null || _pubKey.Length == 0)
        {
            _pubKey = allPublicKeys.First();
        }

        var tx = BuildUnsigned();
        tx.Signature = MultiSigUtils.EncodeMultiSigPayload(threshold, allPublicKeys, signaturesOfSigners);
        return tx;
    }
}
