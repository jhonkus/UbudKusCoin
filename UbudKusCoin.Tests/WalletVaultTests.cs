using System;
using System.IO;
using System.Security.Cryptography;
using UbudKusCoin.Services;
using Xunit;

namespace UbudKusCoin.Tests;

public sealed class WalletVaultTests
{
    private const string MnemonicWords = "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about";

    [Fact]
    public void WalletVault_RoundTripsEncryptedSnapshot()
    {
        var path = TempPath("wallet-vault");
        var key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var originalMode = Environment.GetEnvironmentVariable("WALLET_STORAGE_MODE");
        var originalKey = Environment.GetEnvironmentVariable("WALLET_ENCRYPTION_KEY");

        try
        {
            Environment.SetEnvironmentVariable("WALLET_STORAGE_MODE", "aes-gcm");
            Environment.SetEnvironmentVariable("WALLET_ENCRYPTION_KEY", key);

            var snapshot = new WalletVaultSnapshot(MnemonicWords, 0, DateTimeOffset.UtcNow);
            WalletVault.Save(path, snapshot);

            Assert.True(WalletVault.TryLoad(path, out var loaded));
            Assert.NotNull(loaded);
            Assert.Equal(snapshot.MnemonicWords, loaded!.MnemonicWords);
            Assert.Equal(snapshot.DerivationPath, loaded.DerivationPath);
        }
        finally
        {
            Environment.SetEnvironmentVariable("WALLET_STORAGE_MODE", originalMode);
            Environment.SetEnvironmentVariable("WALLET_ENCRYPTION_KEY", originalKey);
            DeleteFile(path);
        }
    }

    [Fact]
    public void WalletService_StartLoadsExistingVault()
    {
        var path = TempPath("wallet-service");
        var key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var originalMode = Environment.GetEnvironmentVariable("WALLET_STORAGE_MODE");
        var originalKey = Environment.GetEnvironmentVariable("WALLET_ENCRYPTION_KEY");
        var originalStore = Environment.GetEnvironmentVariable("WALLET_STORE_PATH");
        var originalMnemonic = Environment.GetEnvironmentVariable("NODE_PASSPHRASE");

        try
        {
            Environment.SetEnvironmentVariable("WALLET_STORAGE_MODE", "aes-gcm");
            Environment.SetEnvironmentVariable("WALLET_ENCRYPTION_KEY", key);
            Environment.SetEnvironmentVariable("WALLET_STORE_PATH", path);
            Environment.SetEnvironmentVariable("NODE_PASSPHRASE", string.Empty);

            WalletVault.Save(path, new WalletVaultSnapshot(MnemonicWords, 0, DateTimeOffset.UtcNow));

            var wallet = new WalletService();
            wallet.Start();

            var expected = WalletService.GenerateKeyPair(new NBitcoin.Mnemonic(MnemonicWords), 0);
            Assert.Equal(expected.PublicKey.PubKey.ToHex(), wallet.GetPublicKey().PubKey.ToHex());
            Assert.Equal(WalletService.GetAddress(expected.PublicKey.PubKey.ToBytes()), wallet.GetAddress());
        }
        finally
        {
            Environment.SetEnvironmentVariable("WALLET_STORAGE_MODE", originalMode);
            Environment.SetEnvironmentVariable("WALLET_ENCRYPTION_KEY", originalKey);
            Environment.SetEnvironmentVariable("WALLET_STORE_PATH", originalStore);
            Environment.SetEnvironmentVariable("NODE_PASSPHRASE", originalMnemonic);
            DeleteFile(path);
        }
    }

    private static string TempPath(string name)
        => Path.Combine(Path.GetTempPath(), $"ukc-wallet-{name}-{Guid.NewGuid():N}.vault");

    private static void DeleteFile(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
