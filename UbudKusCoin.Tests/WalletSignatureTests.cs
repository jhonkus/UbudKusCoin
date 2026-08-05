using System;
using UbudKusCoin.ConsoleWallet;
using UbudKusCoin.Others;
using UbudKusCoin.Services;
using Xunit;

namespace UbudKusCoin.Tests;

public class WalletSignatureTests
{
    private const string MnemonicWords = "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about";

    [Fact]
    public void NodeSignature_IsAcceptedByNodeVerifier()
    {
        var mnemonic = new NBitcoin.Mnemonic(MnemonicWords);
        var wallet = new WalletService
        {
            KeyPair = WalletService.GenerateKeyPair(mnemonic, 0)
        };
        var hash = UkcUtils.GenHash("node-signature-test");

        var signature = wallet.Sign(hash);

        Assert.True(WalletService.CheckSignature(wallet.GetPublicKey().PubKey.ToHex(), signature, hash));
    }

    [Fact]
    public void ConsoleWalletSignature_IsAcceptedByNodeVerifier()
    {
        var wallet = new Wallet(MnemonicWords);
        var hash = UkcUtils.GenHash("console-wallet-signature-test");

        var signature = wallet.Sign(hash);

        Assert.True(WalletService.CheckSignature(wallet.GetPublicKey().PubKey.ToHex(), signature, hash));
    }

    [Fact]
    public void TamperedMessage_IsRejected()
    {
        var wallet = new Wallet(MnemonicWords);
        var hash = UkcUtils.GenHash("original-message");
        var signature = wallet.Sign(hash);

        Assert.False(WalletService.CheckSignature(wallet.GetPublicKey().PubKey.ToHex(), signature, UkcUtils.GenHash("tampered-message")));
    }

    [Fact]
    public void TamperedSignature_IsRejected()
    {
        var wallet = new Wallet(MnemonicWords);
        var hash = UkcUtils.GenHash("signature-integrity-test");
        var signature = wallet.Sign(hash);
        var tampered = Convert.ToBase64String(Convert.FromBase64String(signature)[..^1]);

        Assert.False(WalletService.CheckSignature(wallet.GetPublicKey().PubKey.ToHex(), tampered, hash));
    }
}
