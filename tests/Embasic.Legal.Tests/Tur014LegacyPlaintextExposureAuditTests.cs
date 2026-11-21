using System.Security.Cryptography;
using System.Text;
using Embasic.Legal.Infrastructure;
using Xunit;

namespace Embasic.Legal.Tests;

public sealed class Tur014LegacyPlaintextExposureAuditTests
{
    private static string CreateDir() =>
        Path.Combine(Path.GetTempPath(), "embasic-tur014-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public void MissingFolderDoesNotClaimWholeDiskPlaintextSafety()
    {
        string folder = CreateDir();
        var report = LegacyPlaintextExposureAudit.ScanDirectory(folder);
        Assert.Equal(0, report.KnownCandidates);
        Assert.False(report.NoPlaintextAnywhereVerified);
        Assert.True(report.LimitedToKnownDirectoryAndNames);
    }

    [Fact]
    public void FindsKnownDatabaseAndWalShmJournalWithoutReadingNoteText()
    {
        string folder = CreateDir();
        Directory.CreateDirectory(folder);
        try
        {
            string data = "SQLite format 3\0SECRET-CASE-SHOULD-NOT-LEAK";
            string primary = Path.Combine(folder, "article-notes.sqlite3");
            File.WriteAllBytes(primary, Encoding.ASCII.GetBytes(data));
            File.WriteAllText(primary + "-wal", "SYNTHETIC-WAL");
            File.WriteAllText(primary + "-shm", "SYNTHETIC-SHM");
            File.WriteAllText(primary + "-journal", "SYNTHETIC-JOURNAL");
            string hashBefore = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(primary)));

            var report = LegacyPlaintextExposureAudit.ScanDirectory(folder);

            Assert.Equal(4, report.KnownCandidates);
            Assert.Equal(1, report.PrimaryCandidates);
            Assert.Equal(3, report.SidecarCandidates);
            Assert.Equal(1, report.SQLiteHeaderMatches);
            Assert.True(report.RequiresManualReview);
            Assert.Equal(hashBefore,
                Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(primary))));
            string serialized = System.Text.Json.JsonSerializer.Serialize(report);
            Assert.DoesNotContain("SECRET-CASE", serialized);
            Assert.DoesNotContain(folder, serialized);
        }
        finally { Directory.Delete(folder, true); }
    }

    [Fact]
    public void ExcludesEncryptedVaultAndUnrelatedFilesButFlagsKnownLegacyBackupName()
    {
        string folder = CreateDir();
        Directory.CreateDirectory(folder);
        try
        {
            File.WriteAllText(Path.Combine(folder,
                "article-notes-v4-encrypted.sqlite3"), "ENCRYPTED-V4");
            File.WriteAllText(Path.Combine(folder, "another.sqlite3"), "ANOTHER");
            File.WriteAllText(Path.Combine(folder, "article-notes.sqlite3.pre-copy"),
                "POTENTIAL PLAINTEXT BACKUP");
            File.WriteAllText(Path.Combine(folder, "article-notes-legacy-export.sqlite3"),
                "POTENTIAL PLAINTEXT LEGACY EXPORT");

            var report = LegacyPlaintextExposureAudit.ScanDirectory(folder);

            Assert.Equal(2, report.KnownCandidates);
            Assert.Equal(2, report.PotentialBackups);
            Assert.Equal(0, report.SQLiteHeaderMatches);
            Assert.True(report.RequiresManualReview);
            Assert.False(report.NoPlaintextAnywhereVerified);
        }
        finally { Directory.Delete(folder, true); }
    }

    [Fact]
    public void SuspiciousNameWithInvalidHeaderIsStillRiskNotCertifiedPlaintext()
    {
        string folder = CreateDir();
        Directory.CreateDirectory(folder);
        try
        {
            File.WriteAllText(Path.Combine(folder, "article-notes.sqlite3"), "NOT-A-SQLITE");
            var report = LegacyPlaintextExposureAudit.ScanDirectory(folder);
            Assert.Equal(1, report.KnownCandidates);
            Assert.Equal(0, report.SQLiteHeaderMatches);
            Assert.True(report.RequiresManualReview);
            Assert.False(report.NoPlaintextAnywhereVerified);
        }
        finally { Directory.Delete(folder, true); }
    }
}
