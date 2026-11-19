# TUR004 — Physical Windows user-acceptance checklist (NOT_RUN)

Use a Windows 11 x64 PC with actual local credentials, no real case content.

1. Inspect logo, icon, popup scaling, accessible contrast and two themes at 100/150/200% DPI.
2. On synthetic note screen, unlock fresh vault with >=12-character test password and ensure wrong password cannot open it later.
3. Save only mock text; restart app; reenter password and confirm text and 📝 sticker persist.
4. Verify new vault DB stores ciphertext (metadata is readable); old TUR003 cleartext file remains untouched.
5. Export encrypted backup and restore while locked. Confirm old state preserved in .pre-restore.sqlite3 rollback; mismatched or corrupted archive fails.
6. Simulate canceled backup, unwritable directory and invalid password; no success claim or accidental overwrite.
7. Confirm editor closes with unsaved changes only after save, and Lock clears draft display.
8. Verify PDF candidate receipt recheck rejects byte edits; even valid local PDF never becomes official full-text proof.
9. Confirm only two date modules and one Gece Vakti; official solar data unavailable => no computed times.
10. Check offline launch, low memory, keyboard, focus, window close, missing dependency fallback.

Physical user acceptance must be logged with actual screen recordings or written measurements; GitHub Actions is not a substitute.
