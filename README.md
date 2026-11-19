# Embasic Legal 2.0 — Windows Alpha TUR004

Native .NET 10 WPF desktop EXE. **ALPHA / LEGAL PRODUCTION NO-GO**. GitHub changes remain stacked Draft PRs; no main merge or release.

## Features
- Exactly two date modules (year/month/day difference and shift), one unified Gece Vakti page with source-dependent fail-closed output.
- 15-law metadata directory only; no certified official full texts or article binding.
- Local TCK/CMK PDF *candidate* import with SHA receipt, exact-copy recheck and provisional article-page coverage auditing; none certifies law authenticity.
- Synthetic WPF expanding note popup, 📝 sticker, autosave and password-protected AES-256-GCM text storage. Encrypted SQLite backups support authenticated restore and prior-state rollback. Passphrase required; no password recovery.
- Legacy TUR003 plaintext file and backups are **retained untouched**, NOT automatically migrated or encrypted. Article metadata remains plaintext.
- Original E-terazi icon and light/dark themes retained. Rights/redistribution permission is not proven.

## Build
On Windows with .NET 10 SDK:
- `dotnet restore Embasic.Legal.slnx`
- `dotnet build Embasic.Legal.slnx -c Release`
- `dotnet test tests/Embasic.Legal.Tests/Embasic.Legal.Tests.csproj -c Release`
- `dotnet publish src/Embasic.Legal.Desktop/Embasic.Legal.Desktop.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false`

**No real investigation or confidential notes**: physical Windows UAT, crypto threat-model review, versioned official corpus, rights and dependencies remain unverified.

Documentation: docs/TUR004-NOTE-ENCRYPTION-RESTORE.md; TUR004-SOURCE-PROVENANCE-AND-COVERAGE.md; TUR004-RELEASE-DECISIONS.md; TUR004-WINDOWS-UAT.md.
