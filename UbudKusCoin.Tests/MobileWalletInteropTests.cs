using System;
using UbudKusCoin.Core.Types;
using Xunit;

namespace UbudKusCoin.Tests;

public sealed class MobileWalletInteropTests
{
    [Fact]
    public void ReactNativeWalletKtx2Vector_IsAcceptedByCoreVerifier()
    {
        const string encoded =
            "S1RYMgEAAAACAAAAAAAAAAEAAAAAAAAAMwAAADRrUGUyVmpZaVBLUFVwQ01icHVOVEJDOUVzRVhmc1VxMzdyVnR4S3JoeFRNbzk1TThoSjMAAAA0a045Q0taSmZBMVJNYW9yYlpaRmVlaHJLaWlKbmNLWGFHQ1JZNThCd21CSzduOFFTbzFAWXMHAAAAABAnAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAIQAAAAI5zV+Bu8efS2T0JaTp5wvp5kPqcIn1m/QBzE/knQTPpwAAAABHAAAAMEUCIQDhO4pPEKeaxTe4qwAMQ69EBsqmkBTslJUr0GNPD4hAGQIgI+R5kHLRtqIO8QUv1tchY8eyc/kQPgGm/GV4AVB/Dsc=";

        var bytes = Convert.FromBase64String(encoded);
        Assert.True(TransactionCodec.TryDecode(bytes, out var transaction, out var error), error);
        Assert.NotNull(transaction);
        Assert.True(transaction!.IsEnvelopeWellFormed(ChainInfo.ChainIdTestnet));
        Assert.True(transaction.VerifySignature());
        Assert.Equal("59efc15c915b252264b19757cb2d9b5ab265a68e612899a0b578d93ffad8e400", transaction.ComputeIdHex());
    }
}
