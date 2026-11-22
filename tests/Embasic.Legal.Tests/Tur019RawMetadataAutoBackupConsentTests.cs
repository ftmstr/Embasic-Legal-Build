using System.Security.Cryptography;
using System.Text;
using Embasic.Legal.Infrastructure;
using Xunit;
namespace Embasic.Legal.Tests;
public sealed class Tur019RawMetadataAutoBackupConsentTests
{
    private const string Pass="TUR019-ONLY-SYNTHETIC-PASSPHRASE";
    private static readonly ArticleNoteKey Key=new("5271","134","1","TUR019-SYNTHETIC");
    private static string Root()=>Path.Combine(Path.GetTempPath(),"embasic-tur019-"+Guid.NewGuid().ToString("N"));
    private static string Sha(string p)=>Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(p)));
    [Fact] public void DefaultDenialDoesNotCreateRawBackup()
    {
        string d=Root();Directory.CreateDirectory(d);
        try { using var v=new EncryptedArticleNotes(Path.Combine(d,"vault.sqlite3"),Pass);
            v.Save(Key,"SYNTHETIC");var g=new RawMetadataAutoBackupConsent();
            string output=Path.Combine(d,"backup.sqlite3");
            Assert.False(g.HasDecision);Assert.False(g.BackupIfApproved(v,output));Assert.False(File.Exists(output));
        } finally { Directory.Delete(d,true); }
    }
    [Fact] public void ExplicitDenialAllowsEncryptedSaveButNotBackup()
    {
        string d=Root();Directory.CreateDirectory(d);
        try { using var v=new EncryptedArticleNotes(Path.Combine(d,"vault.sqlite3"),Pass);
            var g=new RawMetadataAutoBackupConsent();g.Decide(false);v.Save(Key,"SYNTHETIC NOTE");
            Assert.True(g.HasDecision);Assert.False(g.BackupIfApproved(v,Path.Combine(d,"backup.sqlite3")));
            Assert.False(File.Exists(Path.Combine(d,"backup.sqlite3")));Assert.Equal("SYNTHETIC NOTE",v.Get(Key)?.Text);
        } finally { Directory.Delete(d,true); }
    }
    [Fact] public void ConsentAllowsBackupButClearlyDoesNotEncryptMetadata()
    {
        string d=Root();Directory.CreateDirectory(d);
        try { string b=Path.Combine(d,"backup.sqlite3");
            using var v=new EncryptedArticleNotes(Path.Combine(d,"vault.sqlite3"),Pass);
            v.Save(Key,"SYNTHETIC-NOTE-BODY-PRIVATE");var g=new RawMetadataAutoBackupConsent();g.Decide(true);
            Assert.True(g.BackupIfApproved(v,b));
            string pages=Encoding.Latin1.GetString(File.ReadAllBytes(b));
            Assert.Contains("TUR019-SYNTHETIC",pages);
            Assert.DoesNotContain("SYNTHETIC-NOTE-BODY-PRIVATE",pages);
            using var reopened=new EncryptedArticleNotes(b,Pass);
            Assert.Equal("SYNTHETIC-NOTE-BODY-PRIVATE",reopened.Get(Key)?.Text);
        } finally { Directory.Delete(d,true); }
    }
    [Fact] public void LockResetPreventsReplacementOfOlderBackup()
    {
        string d=Root();Directory.CreateDirectory(d);
        try { string b=Path.Combine(d,"backup.sqlite3");
            using var v=new EncryptedArticleNotes(Path.Combine(d,"vault.sqlite3"),Pass);
            v.Save(Key,"OLD");var g=new RawMetadataAutoBackupConsent();g.Decide(true);
            Assert.True(g.BackupIfApproved(v,b));string prior=Sha(b);
            g.Reset();v.Save(Key,"NEW");Assert.False(g.HasDecision);
            Assert.False(g.BackupIfApproved(v,b));Assert.Equal(prior,Sha(b));
        } finally { Directory.Delete(d,true); }
    }
    [Fact] public void DeletionNeedsFreshApprovedBackupPermission()
    {
        var g=new RawMetadataAutoBackupConsent();
        Assert.Throws<InvalidOperationException>(()=>g.RequireApprovalForDeletion());
        g.Decide(false);Assert.Throws<InvalidOperationException>(()=>g.RequireApprovalForDeletion());
        g.Decide(true);g.RequireApprovalForDeletion();g.Reset();
        Assert.Throws<InvalidOperationException>(()=>g.RequireApprovalForDeletion());
    }
}
