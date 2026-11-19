# Embasic Legal 2.0 — Windows Alpha (TUR 001)

A genuine WPF/.NET 10 Windows desktop application under development. This is not the legacy browser/HTML launcher. Source status: **ALPHA, LEGAL PRODUCTION NO-GO**.

## Current scope
- Two native date tools: event/reference date difference with optional year/month/day shift; one-date year/month/day shift.
- Exactly one Gece Vakti screen, which refuses to claim official sunrise/sunset or TCK night calculations until the Adalet Bakanlığı/UYAP chart source is verified.
- WPF theme switching with local preference, detached Windows work window.
- Metadata-only directory of 15 regulations. No official full law text, full-text search or notes yet.
- No text editor.

## Build (Windows, .NET 10 SDK)
Run: dotnet restore Embasic.Legal.slnx
Then: dotnet build Embasic.Legal.slnx -c Release
Test: dotnet test tests/Embasic.Legal.Tests/Embasic.Legal.Tests.csproj -c Release
Publish: dotnet publish src/Embasic.Legal.Desktop/Embasic.Legal.Desktop.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false

The GitHub Windows workflow will publish an **alpha artifact only**, not a signed installer, not a release. The existing LICENSE file is GPL-3.0, whereas the master prompt requests MIT; license reconciliation is pending.

See docs/FEATURE-PARITY-TUR001.md and docs/ARCHITECTURE-TUR001.md. Do not upload judicial case data to this public repository.
