# TUR004 — Encrypted note vault, backup/restore and migration gate

## Actual implementation
- New local vault file: LocalAppData/Embasic/Legal/article-notes-v4-encrypted.sqlite3.
- Saved note **text** is encrypted with AES-256-GCM (fresh 12-byte nonce, 16-byte authentication tag per write). The record AAD binds law number, article identifier, subdivision and text-version key, preventing ciphertext transfer across different article versions.
- Per-vault random 16-byte salt; PBKDF2-HMAC-SHA256 with 310000 iterations derives 256-bit key from a >=12-character user passphrase. Encrypted sentinel authenticates the passphrase before editing. The key is zeroed on vault Dispose; passphrase comes directly from a WPF PasswordBox (which is cleared after the command). Managed string remnants may remain in memory; this is not a password manager security certification.
- The demo keeps the existing clear-text SQLite file from TUR003 **untouched** and separate; it is not automatically migrated, erased or claimed to be encrypted. No silent migration of unrelated or older plaintext DB files. Backups from the older file also remain plaintext.
- Article metadata, note IDs and timestamps remain **unencrypted in SQLite**. Full disk/database encryption is NOT claimed. No real investigation data must be entered.
- Demo only: SANDBOX/SYNTHETIC-ARTICLE/TUR003-EXPLICIT-DEMO. Real statutory article annotation stays disabled pending independent corpus verification.
- On save: encrypted SQLite BackupDatabase => quick_check => temporary file => atomic replacement. Manual encrypted export available.
- On restore: source vault authentication and integrity, current target authentication and same salt identity, staged recheck, File.Replace with one non-overwritten .pre-restore.sqlite3 rollback. Locked vault required. If prior rollback exists, restore aborts rather than overwriting the last recovery point.
- Wrong password, malformed data, foreign vault and pre-existing cleartext target are rejected. There is **no password recovery**.
- Remaining gaps: real Windows UI and permission acceptance, rollback policy/rotation, power-loss injection, platform secure erase, secure migration with explicit consent, hardened Windows ACLs, metadata encryption, password memory controls and independent security audit.

## Runtime safety
Passwords are not persisted to repository, Drive, SQLite or logs. Tests use synthetic test-only passphrases. The encrypted vault is for Alpha behavior testing only, not sensitive real case data. Crypto tests demonstrate mechanical reversibility/tamper rejection, not full-security certification.
