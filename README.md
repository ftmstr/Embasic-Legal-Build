# Embasic Legal 2.0 — Native Windows Alpha TUR003

A real C#/.NET 10 WPF Windows executable; not an HTML/browser launcher.

**ALPHA / LEGAL PRODUCTION NO-GO.** Development is in stacked Draft PRs; main remains unchanged.

## Available
- Only two date modules with calendar arithmetic and copy-to-clipboard.
- Only one Gece Vakti page; no invented official UYAP sunrise/sunset times.
- 15-law metadata directory, no verified full law articles.
- Local 5237/5271 PDF candidate importer, with source byte hash receipt but no legal-text PASS.
- Favoriler ve Notlar: labeled SYNTHETIC-only expandable WPF note popup, 📝 sticker, 900 ms autosave, SQLite storage and atomic integrity-checked backup. No real law article can be annotated without authenticated source/version coverage.
- Original E-terazi images and two themes.

## Build on Windows
Install .NET 10 SDK.
dotnet restore Embasic.Legal.slnx
dotnet build Embasic.Legal.slnx -c Release
dotnet test tests/Embasic.Legal.Tests/Embasic.Legal.Tests.csproj -c Release
dotnet publish src/Embasic.Legal.Desktop/Embasic.Legal.Desktop.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false

**SQLite notes/backups are unencrypted**; do not input sensitive case data. See docs/TUR003-OFFICIAL-SOURCE-EVIDENCE.md, docs/TUR003-NOTE-AND-WINDOWS-UAT.md, docs/TUR003-LICENSE-RELEASE-GATES.md.

Current GitHub LICENSE is GPL-3.0; master v2.1 requests MIT. Logo redistribution permission remains undocumented.