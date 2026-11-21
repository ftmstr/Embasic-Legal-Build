using System.Text;

namespace Embasic.Legal.Infrastructure;

/// <summary>
/// Bounded, read-only inventory of known plaintext-vault filenames in ONE
/// explicit directory. Never opens the SQLite schema, reads note content,
/// traverses directories, enumerates arbitrary disks, or removes files.
/// Not a guarantee that plaintext copies do not exist elsewhere.
/// </summary>
public static class LegacyPlaintextExposureAudit
{
    private const int MaxEntries = 2048;
    private static readonly byte[] SqliteSignature =
        Encoding.ASCII.GetBytes("SQLite format 3\0");

    public sealed record ExposureSummary(
        int KnownCandidates,
        int PrimaryCandidates,
        int SidecarCandidates,
        int PotentialBackups,
        int SQLiteHeaderMatches,
        long TotalCandidateBytes,
        int UnexaminedEntries)
    {
        public bool RequiresManualReview => KnownCandidates > 0 || UnexaminedEntries > 0;
        public bool LimitedToKnownDirectoryAndNames => true;
        public bool NoPlaintextAnywhereVerified => false;
    }

    public static ExposureSummary ScanKnownLocalDirectory() =>
        ScanDirectory(Path.GetDirectoryName(ArticleNoteStore.DefaultDatabasePath)!);

    /// <summary>Designed for a known, caller-selected folder and unit tests.</summary>
    public static ExposureSummary ScanDirectory(string directory)
    {
        if (string.IsNullOrWhiteSpace(directory))
            throw new ArgumentException("Klasör yolu gerekli.", nameof(directory));
        string root = Path.GetFullPath(directory);
        if (!Directory.Exists(root))
            return new ExposureSummary(0, 0, 0, 0, 0, 0, 0);

        // A redirected directory could escape the nominated local scope.
        try
        {
            if ((File.GetAttributes(root) & FileAttributes.ReparsePoint) != 0)
                return new ExposureSummary(0, 0, 0, 0, 0, 0, 1);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return new ExposureSummary(0, 0, 0, 0, 0, 0, 1);
        }

        int candidate = 0, primary = 0, sidecars = 0,
            backups = 0, headers = 0, incomplete = 0;
        long bytes = 0;
        int examined = 0;
        try
        {
            foreach (string path in Directory.EnumerateFileSystemEntries(root,
                         "*", SearchOption.TopDirectoryOnly))
            {
                if (++examined > MaxEntries)
                {
                    incomplete++;
                    break;
                }
                var kind = Classify(Path.GetFileName(path));
                if (kind == Kind.Ignore) continue;
                try
                {
                    FileAttributes attributes = File.GetAttributes(path);
                    if ((attributes & (FileAttributes.Directory | FileAttributes.ReparsePoint)) != 0)
                    {
                        incomplete++;
                        continue;
                    }
                    // Fail closed: a plausible name is a risk even if signature
                    // cannot be read. The count does not certify plaintext.
                    candidate++;
                    if (kind == Kind.Primary) primary++;
                    else if (kind == Kind.Sidecar) sidecars++;
                    else backups++;

                    FileInfo info = new(path);
                    bytes = checked(bytes + info.Length);
                    if (kind != Kind.Sidecar && HasSqliteHeader(path))
                        headers++;
                }
                catch (Exception ex) when (ex is IOException or
                    UnauthorizedAccessException or OverflowException)
                {
                    incomplete++;
                }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            incomplete++;
        }
        return new ExposureSummary(candidate, primary, sidecars, backups,
            headers, bytes, incomplete);
    }

    private enum Kind { Ignore, Primary, Sidecar, Backup }

    private static Kind Classify(string name)
    {
        if (string.Equals(name, "article-notes.sqlite3",
                StringComparison.OrdinalIgnoreCase))
            return Kind.Primary;
        foreach (string suffix in new[] { "-wal", "-shm", "-journal" })
        {
            if (string.Equals(name, "article-notes.sqlite3" + suffix,
                    StringComparison.OrdinalIgnoreCase))
                return Kind.Sidecar;
        }
        if (name.StartsWith("article-notes.sqlite3.",
                StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith("article-notes-legacy-",
                StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith("article-notes-plaintext-",
                StringComparison.OrdinalIgnoreCase))
            return Kind.Backup;
        return Kind.Ignore;
    }

    private static bool HasSqliteHeader(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete, bufferSize: 16);
        Span<byte> signature = stackalloc byte[16];
        return stream.Read(signature) == signature.Length &&
            signature.SequenceEqual(SqliteSignature);
    }
}
