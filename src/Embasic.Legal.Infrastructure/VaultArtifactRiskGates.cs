namespace Embasic.Legal.Infrastructure;

/// <summary>
/// Strict, non-destructive guard for *known* SQLite sidecar hazards and a
/// mistaken 'independent' HMAC receipt location. No anti-rollback claim:
/// different directories may still be on the same volume or rolled back
/// together. Paths/sidecars can still change concurrently (TOCTOU); an
/// OS-backed locked snapshot or trusted external counter is not provided.
/// </summary>
public static class VaultArtifactRiskGates
{
    private static readonly string[] SidecarSuffixes = ["-wal", "-shm", "-journal"];

    public static void RequireNoSqliteSidecars(string sqlitePath)
    {
        if (string.IsNullOrWhiteSpace(sqlitePath))
            throw new ArgumentException("SQLite yolu zorunludur.", nameof(sqlitePath));
        string absolute = Path.GetFullPath(sqlitePath);
        foreach (string suffix in SidecarSuffixes)
        {
            string candidate = absolute + suffix;
            // TUR020: File.Exists returns false on directories and may return
            // false when an entry is inaccessible. A reserved sidecar NAME
            // occupied by any filesystem entry must be rejected fail-closed.
            try
            {
                _ = File.GetAttributes(candidate);
            }
            catch (FileNotFoundException)
            {
                continue; // Exact candidate does not exist.
            }
            catch (DirectoryNotFoundException)
            {
                continue; // Destination parent does not exist yet.
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                throw new IOException(
                    "SQLite yan dosyasının varlığı/türü güvenle denetlenemedi; işlem reddedildi.", ex);
            }
            // Includes regular files, directories and accessible reparse entries.
            // Do not delete or checkpoint a potentially evidentiary artifact.
            throw new IOException(
                "Kasa/yedek WAL, SHM veya journal yan dosya adı dolu " +
                "(dosya/dizin/yeniden yönlendirme); işlem reddedildi. " +
                "Yan dosya silinmez, otomatik checkpoint yapılmaz.");
        }
    }

    /// <summary>
    /// TUR023: The path reserved for a pre-restore rollback must not be
    /// occupied by a main file, directory, or ANY SQLite sidecar entry.
    /// File.Exists alone misses directories and orphan WAL/SHM/journal files.
    /// No file is deleted or repaired, and no OS-level atomic reservation,
    /// reparse/hardlink safety or anti-TOCTOU guarantee is claimed.
    /// </summary>
    public static void RequireUnusedRollbackDestination(string rollbackPath)
    {
        if (string.IsNullOrWhiteSpace(rollbackPath))
            throw new ArgumentException("Geri alma yedek yolu zorunludur.", nameof(rollbackPath));
        string absolute = Path.GetFullPath(rollbackPath);
        RequireNoSqliteSidecars(absolute);
        try
        {
            _ = File.GetAttributes(absolute);
        }
        catch (FileNotFoundException)
        {
            return;
        }
        catch (DirectoryNotFoundException)
        {
            return;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new IOException(
                "Geri alma hedefinin dosya sistemi durumu doğrulanamadı; işlem reddedildi.", ex);
        }
        throw new IOException(
            "Önceki geri alma yedeğinin dosya/dizin yolu dolu; üzerine yazılmayacak.");
    }

    public static void RequireWitnessOutsideVaultDirectory(
        string sqlitePath, string witnessPath)
    {
        if (string.IsNullOrWhiteSpace(sqlitePath) ||
            string.IsNullOrWhiteSpace(witnessPath))
            throw new ArgumentException("Kasa ve makbuz yolu zorunludur.");
        string vaultFolder = Path.TrimEndingDirectorySeparator(
            Path.GetDirectoryName(Path.GetFullPath(sqlitePath))!);
        string witnessFolder = Path.TrimEndingDirectorySeparator(
            Path.GetDirectoryName(Path.GetFullPath(witnessPath))!);
        if (string.Equals(vaultFolder, witnessFolder,
                StringComparison.OrdinalIgnoreCase))
            throw new IOException(
                "Makbuz kasa ile aynı klasörde saklanamaz. " +
                "Bağımsız bir klasör/ortam seçin; farklı klasör tek başına " +
                "güvenilir anti-rollback kanıtı DEĞİLDİR.");
    }
}
