using System;
using System.IO;
using UbudKusCoin.Services;
using Xunit;

namespace UbudKusCoin.Tests;

public sealed class CometBftValidatorKeyLoaderTests
{
    [Fact]
    public void ValidatePublicKey_RequiresEd25519Length()
    {
        CometBftValidatorKeyLoader.ValidatePublicKey(new byte[32]);

        Assert.Throws<InvalidDataException>(
            () => CometBftValidatorKeyLoader.ValidatePublicKey(new byte[33]));
    }

    [Fact]
    public void ValidatePublicKey_RejectsMissingKey()
    {
        Assert.Throws<InvalidDataException>(
            () => CometBftValidatorKeyLoader.ValidatePublicKey(Array.Empty<byte>()));
    }
}
