using System.Security.Cryptography;
using UbudKusCoin.Core.Types;
using Xunit;

namespace UbudKusCoin.Tests;

public class AddressTests
{
    [Fact]
    public void Address_RoundTrips()
    {
        var pubkey = new byte[33];
        RandomNumberGenerator.Fill(pubkey);
        pubkey[0] = 0x02; // compressed even-y

        var addr = Address.FromPublicKey(Address.MainnetVersion, pubkey);
        Assert.True(Address.TryParse(addr.Encoded, out var parsed));
        Assert.Equal(addr.Encoded, parsed.Encoded);
        Assert.Equal(Address.MainnetVersion, parsed.Version);
    }

    [Fact]
    public void Address_DifferentNetworks_Differ()
    {
        var pubkey = new byte[33];
        RandomNumberGenerator.Fill(pubkey);
        pubkey[0] = 0x02;

        var mainnet = Address.FromPublicKey(Address.MainnetVersion, pubkey);
        var testnet = Address.FromPublicKey(Address.TestnetVersion, pubkey);

        Assert.NotEqual(mainnet.Encoded, testnet.Encoded);
        Assert.NotEqual(mainnet.Version, testnet.Version);
    }

    [Fact]
    public void Address_DetectsCorruptedChecksum()
    {
        var pubkey = new byte[33];
        RandomNumberGenerator.Fill(pubkey);
        pubkey[0] = 0x02;

        var addr = Address.FromPublicKey(Address.MainnetVersion, pubkey);
        var corrupted = addr.Encoded.Length > 1
            ? (addr.Encoded[0] == '1' ? addr.Encoded[1..] + "1" : "1" + addr.Encoded[1..])
            : addr.Encoded;

        Assert.False(Address.TryParse(corrupted, out _));
    }

    [Fact]
    public void Address_RejectsInvalidBase58()
    {
        Assert.False(Address.TryParse("0OIl-not-valid-characters", out _));
        Assert.False(Address.TryParse("", out _));
        Assert.False(Address.TryParse("short", out _));
    }
}
