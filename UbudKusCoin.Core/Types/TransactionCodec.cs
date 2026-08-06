using System.Buffers.Binary;
using System.Text;

namespace UbudKusCoin.Core.Types;

/// <summary>
/// Canonical byte envelope used when transactions cross the application
/// boundary. It is deliberately independent of JSON and reflection serializers.
/// </summary>
public static class TransactionCodec
{
    private const uint Magic = 0x3258544B; // KTX2
    private const int MaxAddressBytes = 128;
    private const int MaxPublicKeyBytes = 65;
    private const int MaxValidatorPublicKeyBytes = 32;
    private const int MaxSignatureBytes = 80;

    public static byte[] Encode(Transaction transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);
        var from = Encoding.UTF8.GetBytes(transaction.From.Encoded);
        var to = Encoding.UTF8.GetBytes(transaction.To.Encoded);
        ValidateLength(from.Length, MaxAddressBytes, "sender address");
        ValidateLength(to.Length, MaxAddressBytes, "recipient address");
        ValidateLength(transaction.PubKey.Length, MaxPublicKeyBytes, "public key");
        ValidateLength(transaction.ValidatorPubKey.Length, MaxValidatorPublicKeyBytes, "validator public key");
        ValidateLength(transaction.Signature.Length, MaxSignatureBytes, "signature");

        var size = 4 + 4 + 4 + 4 + 8 + 4 + from.Length + 4 + to.Length + 8 + 8 + 8 + 8 + 8
            + 4 + transaction.PubKey.Length + 4 + transaction.ValidatorPubKey.Length
            + 4 + transaction.Signature.Length;
        var result = new byte[size];
        var offset = 0;
        WriteUInt32(result, ref offset, Magic);
        WriteUInt32(result, ref offset, transaction.Version);
        WriteUInt32(result, ref offset, transaction.ChainId);
        WriteUInt32(result, ref offset, (uint)transaction.Kind);
        WriteUInt64(result, ref offset, transaction.Nonce);
        WriteBytes(result, ref offset, from);
        WriteBytes(result, ref offset, to);
        WriteUInt64(result, ref offset, checked((ulong)transaction.Amount.BaseUnits));
        WriteUInt64(result, ref offset, checked((ulong)transaction.Fee.BaseUnits));
        WriteUInt64(result, ref offset, unchecked((ulong)transaction.LockPeriod));
        WriteUInt64(result, ref offset, unchecked((ulong)transaction.ValidFrom));
        WriteUInt64(result, ref offset, unchecked((ulong)transaction.ValidUntil));
        WriteBytes(result, ref offset, transaction.PubKey);
        WriteBytes(result, ref offset, transaction.ValidatorPubKey);
        WriteBytes(result, ref offset, transaction.Signature);
        return result;
    }

    public static bool TryDecode(ReadOnlySpan<byte> encoded, out Transaction? transaction, out string error)
    {
        transaction = null;
        error = string.Empty;
        var reader = new Reader(encoded);
        try
        {
            if (reader.ReadUInt32() != Magic)
            {
                error = "Invalid transaction magic.";
                return false;
            }

            var result = new Transaction
            {
                Version = reader.ReadUInt32(),
                ChainId = reader.ReadUInt32(),
                Kind = (TransactionKind)reader.ReadUInt32(),
                Nonce = reader.ReadUInt64(),
                From = Address.Parse(reader.ReadString(MaxAddressBytes)),
                To = Address.Parse(reader.ReadString(MaxAddressBytes)),
                Amount = new Money(checked((long)reader.ReadUInt64())),
                Fee = new Money(checked((long)reader.ReadUInt64())),
                LockPeriod = unchecked((long)reader.ReadUInt64()),
                ValidFrom = unchecked((long)reader.ReadUInt64()),
                ValidUntil = unchecked((long)reader.ReadUInt64()),
                PubKey = reader.ReadBytes(MaxPublicKeyBytes),
                ValidatorPubKey = reader.ReadBytes(MaxValidatorPublicKeyBytes),
                Signature = reader.ReadBytes(MaxSignatureBytes)
            };
            if (!reader.IsAtEnd)
            {
                error = "Trailing transaction bytes are not allowed.";
                return false;
            }

            transaction = result;
            return true;
        }
        catch (Exception exception) when (exception is ArgumentException
            or FormatException or InvalidOperationException or OverflowException)
        {
            error = $"Invalid transaction encoding: {exception.Message}";
            return false;
        }
    }

    private static void ValidateLength(int length, int max, string field)
    {
        if (length > max)
        {
            throw new ArgumentOutOfRangeException(field, $"The {field} is too large.");
        }
    }

    private static void WriteUInt32(byte[] buffer, ref int offset, uint value)
    {
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(offset, 4), value);
        offset += 4;
    }

    private static void WriteUInt64(byte[] buffer, ref int offset, ulong value)
    {
        BinaryPrimitives.WriteUInt64LittleEndian(buffer.AsSpan(offset, 8), value);
        offset += 8;
    }

    private static void WriteBytes(byte[] buffer, ref int offset, byte[] value)
    {
        WriteUInt32(buffer, ref offset, checked((uint)value.Length));
        value.CopyTo(buffer, offset);
        offset += value.Length;
    }

    private ref struct Reader
    {
        private readonly ReadOnlySpan<byte> _buffer;
        private int _offset;

        public Reader(ReadOnlySpan<byte> buffer)
        {
            _buffer = buffer;
            _offset = 0;
        }
        public bool IsAtEnd => _offset == _buffer.Length;

        public uint ReadUInt32()
        {
            EnsureAvailable(4);
            var value = BinaryPrimitives.ReadUInt32LittleEndian(_buffer[_offset..]);
            _offset += 4;
            return value;
        }

        public ulong ReadUInt64()
        {
            EnsureAvailable(8);
            var value = BinaryPrimitives.ReadUInt64LittleEndian(_buffer[_offset..]);
            _offset += 8;
            return value;
        }

        public byte[] ReadBytes(int maxLength)
        {
            var length = checked((int)ReadUInt32());
            if (length > maxLength)
            {
                throw new InvalidOperationException("Transaction field exceeds its maximum size.");
            }

            EnsureAvailable(length);
            var value = _buffer.Slice(_offset, length).ToArray();
            _offset += length;
            return value;
        }

        public string ReadString(int maxLength)
            => Encoding.UTF8.GetString(ReadBytes(maxLength));

        private void EnsureAvailable(int length)
        {
            if (length < 0 || _offset > _buffer.Length - length)
            {
                throw new InvalidOperationException("Transaction encoding is truncated.");
            }
        }
    }
}
