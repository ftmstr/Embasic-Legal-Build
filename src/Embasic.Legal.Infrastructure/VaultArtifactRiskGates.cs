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
            if (File.Exists(absolute + suffix))
                throw new IOException(
                    "Kasa/yedek için WAL, SHM veya journal yan dosyası tespit edildi. " +
                    "Veriyi kaybetmemek için geri yükleme reddedildi; canlı SQLite " +
                    "oturumunu güvenli kapatıp ayrı kanıtla yeniden inceleyin.");
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
