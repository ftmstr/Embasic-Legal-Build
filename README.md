# Embasic Legal 2.0 — Windows Alpha TUR005

Genuine Windows C#/.NET 10 WPF EXE, NOT a browser launcher. **Alpha / legal production NO-GO.**

Features:
- Two native date screens and one unified Gece Vakti screen, official UYAP timetable unavailable => no invented night times.
- Original E-terazi assets and light/dark themes. Artwork redistribution rights unverified.
- 15-law metadata directory and local candidate PDF hashes only; no verified complete official statute corpus.
- Synthetic-only WPF article note popup, 📝 sticker, AES-256-GCM encrypted note texts, protected backup/restore. Note metadata remains plaintext.
- TUR005: read-only legacy plaintext note preview, explicit checkbox AND second yes/no consent for atomic migration into encrypted vault. Original legacy plaintext data retained.
- TUR005: each Tutuklama, Adli Kontrol and Arama ve El Koyma menu heading has its own “… Sistematiği” button, opening a pedagogical diagram and human-operated case-file audit. 12 + 12 + 27 cited steps. No automatic legal compliance decision; audit data stays in RAM unless user explicitly copies it.
- Manual offline Windows UAT PowerShell script included in CI Alpha artifact. No physical Windows UAT PASS claim.

Windows .NET 10 commands:
dotnet restore Embasic.Legal.slnx
dotnet build Embasic.Legal.slnx -c Release
dotnet test tests/Embasic.Legal.Tests/Embasic.Legal.Tests.csproj -c Release
dotnet publish src/Embasic.Legal.Desktop/Embasic.Legal.Desktop.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false

Never enter real investigation secrets in this Alpha. No main merge, tag, installer or release. LICENSE is GPL-3.0 while master prompt requests MIT; rights resolution remains pending.

See docs/TUR005-THREE-PROCEDURAL-SYSTEMATICS.md and docs/TUR005-MIGRATION-AND-WINDOWS-UAT.md.
