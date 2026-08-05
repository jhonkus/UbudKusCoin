namespace UbudKusCoin.Core.Types;

/// <summary>
/// A single on-chain account. The balance is integer fixed-point
/// (<see cref="Money"/>) and the <see cref="Nonce"/> is a per-account
/// monotonic counter used for replay protection (each accepted transfer must
/// have <c>nonce == account.Nonce + 1</c>).
/// </summary>
public sealed class Account
{
    public Address Address { get; set; }
    public Money Balance { get; set; }
    public ulong Nonce { get; set; }
    public byte[] PubKey { get; set; } = Array.Empty<byte>();

    public Account ShallowClone()
    {
        return new Account
        {
            Address = Address,
            Balance = Balance,
            Nonce = Nonce,
            PubKey = PubKey,
        };
    }
}
