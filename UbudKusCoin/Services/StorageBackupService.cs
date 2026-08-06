using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace UbudKusCoin.Services;

public sealed record StorageBackupItem(string AbsolutePath, string RelativePath);

public static class StorageBackupService
{
    public static string CreateBackup(string storageRoot, string backupRoot, IEnumerable<StorageBackupItem> items)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storageRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(backupRoot);
        ArgumentNullException.ThrowIfNull(items);

        var resolvedStorageRoot = Path.GetFullPath(storageRoot);
        var resolvedBackupRoot = Path.GetFullPath(backupRoot);
        Directory.CreateDirectory(resolvedBackupRoot);

        var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmssfff");
        var backupDirectory = Path.Combine(resolvedBackupRoot, timestamp);
        Directory.CreateDirectory(backupDirectory);

        foreach (var item in items)
        {
            if (!File.Exists(item.AbsolutePath))
            {
                continue;
            }

            var absolutePath = Path.GetFullPath(item.AbsolutePath);
            if (!absolutePath.StartsWith(resolvedStorageRoot, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Backup item '{absolutePath}' is outside the storage root.");
            }

            var target = Path.Combine(backupDirectory, item.RelativePath);
            var targetDirectory = Path.GetDirectoryName(target)!;
            Directory.CreateDirectory(targetDirectory);
            File.Copy(absolutePath, target, true);
        }

        return backupDirectory;
    }

    public static void RestoreBackup(string backupDirectory, string storageRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(backupDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(storageRoot);

        var resolvedBackupDirectory = Path.GetFullPath(backupDirectory);
        var resolvedStorageRoot = Path.GetFullPath(storageRoot);
        if (!Directory.Exists(resolvedBackupDirectory))
        {
            throw new DirectoryNotFoundException($"Backup directory '{resolvedBackupDirectory}' does not exist.");
        }

        foreach (var source in Directory.EnumerateFiles(resolvedBackupDirectory, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(resolvedBackupDirectory, source);
            var target = Path.Combine(resolvedStorageRoot, relativePath);
            var targetDirectory = Path.GetDirectoryName(target)!;
            Directory.CreateDirectory(targetDirectory);
            File.Copy(source, target, true);
        }
    }

    public static IReadOnlyList<StorageBackupItem> Discover(string storageRoot, params string[] relativePaths)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storageRoot);
        if (relativePaths is null || relativePaths.Length == 0)
        {
            return Array.Empty<StorageBackupItem>();
        }

        var resolvedStorageRoot = Path.GetFullPath(storageRoot);
        return relativePaths
            .Select(relativePath => new StorageBackupItem(
                Path.Combine(resolvedStorageRoot, relativePath),
                relativePath))
            .ToArray();
    }
}
