using System;
using System.IO;
using UbudKusCoin.Services;
using Xunit;

namespace UbudKusCoin.Tests;

public sealed class StorageBackupServiceTests
{
    [Fact]
    public void BackupAndRestore_RoundsTripStorageFiles()
    {
        var storageRoot = Path.Combine(Path.GetTempPath(), $"ukc-storage-{Guid.NewGuid():N}");
        var backupRoot = Path.Combine(Path.GetTempPath(), $"ukc-backup-{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(storageRoot);
            File.WriteAllText(Path.Combine(storageRoot, "wallet.vault"), "wallet-secret");
            File.WriteAllText(Path.Combine(storageRoot, "block.db"), "block-data");
            Directory.CreateDirectory(Path.Combine(storageRoot, "nested"));
            File.WriteAllText(Path.Combine(storageRoot, "nested", "canonical-chain.json"), "chain-data");

            var items = StorageBackupService.Discover(
                storageRoot,
                "wallet.vault",
                "block.db",
                Path.Combine("nested", "canonical-chain.json"));
            var backupDirectory = StorageBackupService.CreateBackup(storageRoot, backupRoot, items);

            File.Delete(Path.Combine(storageRoot, "wallet.vault"));
            File.Delete(Path.Combine(storageRoot, "block.db"));
            File.Delete(Path.Combine(storageRoot, "nested", "canonical-chain.json"));

            StorageBackupService.RestoreBackup(backupDirectory, storageRoot);

            Assert.Equal("wallet-secret", File.ReadAllText(Path.Combine(storageRoot, "wallet.vault")));
            Assert.Equal("block-data", File.ReadAllText(Path.Combine(storageRoot, "block.db")));
            Assert.Equal("chain-data", File.ReadAllText(Path.Combine(storageRoot, "nested", "canonical-chain.json")));
        }
        finally
        {
            if (Directory.Exists(storageRoot))
            {
                Directory.Delete(storageRoot, true);
            }

            if (Directory.Exists(backupRoot))
            {
                Directory.Delete(backupRoot, true);
            }
        }
    }

    [Fact]
    public void BackupRejectsPathsOutsideStorageRoot()
    {
        var storageRoot = Path.Combine(Path.GetTempPath(), $"ukc-storage-{Guid.NewGuid():N}");
        var outside = Path.Combine(Path.GetTempPath(), $"ukc-outside-{Guid.NewGuid():N}.txt");
        var backupRoot = Path.Combine(Path.GetTempPath(), $"ukc-backup-{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(storageRoot);
            File.WriteAllText(outside, "outside");

            var items = new[] { new StorageBackupItem(outside, "outside.txt") };

            Assert.Throws<InvalidOperationException>(() =>
                StorageBackupService.CreateBackup(storageRoot, backupRoot, items));
        }
        finally
        {
            if (Directory.Exists(storageRoot))
            {
                Directory.Delete(storageRoot, true);
            }

            if (File.Exists(outside))
            {
                File.Delete(outside);
            }

            if (Directory.Exists(backupRoot))
            {
                Directory.Delete(backupRoot, true);
            }
        }
    }
}
