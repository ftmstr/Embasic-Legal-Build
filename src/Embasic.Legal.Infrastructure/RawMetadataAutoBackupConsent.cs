namespace Embasic.Legal.Infrastructure;

/// <summary>Default-deny, per-unlocked-session consent for raw SQLite backups.
/// AES-GCM encrypts the note text but law/article/version/timestamps remain
/// readable SQLite metadata. This is not full-database encryption or erasure.
/// </summary>
public sealed class RawMetadataAutoBackupConsent
{
    public bool HasDecision { get; private set; }
    public bool Approved { get; private set; }
    public void Decide(bool approved) { HasDecision = true; Approved = approved; }
    public void Reset() { HasDecision = false; Approved = false; }
    public void RequireApprovalForDeletion()
    {
        if (!Approved) throw new InvalidOperationException(
            "Açık SQLite üstverili yedek onaylanmadan not silinemez.");
    }
    public bool BackupIfApproved(EncryptedArticleNotes vault, string destination)
    {
        ArgumentNullException.ThrowIfNull(vault);
        if (string.IsNullOrWhiteSpace(destination))
            throw new ArgumentException("Yedek hedefi gereklidir.", nameof(destination));
        if (!Approved) return false;
        vault.BackupTo(destination);
        return true;
    }
}
