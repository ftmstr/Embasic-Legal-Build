# TUR001 — Native Windows architecture

## Decision
Use a real WPF/.NET 10 window and independent C# date arithmetic instead of an HTML launcher.
The core is independent of Windows; the WPF shell depends on core, not vice versa.
UI input normalization is currently in the desktop view model module; statutory deadlines are explicitly out of scope.

## Seams and depth
- DateEngine: small Shift/Between interface hides Gregorian, leap-day and month-end behavior.
- LegislationCatalog: metadata-only directory; never an authoritative law text source.
- WPF view models: display-only adaptors; all legal content modules default to fail-closed.
- Future official-source adapter: import and verify authentic source bytes, store hash and page/article evidence. A directory URL is not a verified source.
- Future notes store: SQLite keyed to stable law/article and version identifiers. Never cross-map a renumbered article automatically.

## Legal gates
No original 5237/5271 raw PDF SHA-256 or verified complete text is bundled. 7571 act-level promulgation and effect records in Alpha Tur 07 are not case applicability proofs. Official UYAP solar chart not present. No outputs for sun/night time until verified. Production NO-GO.

## Security, distribution and testing
No external telemetry, no credentials, no real case data. The current repo already contains a GNU GPL v3 LICENSE; the v2.1 master prompt says MIT. Do not change existing license without explicit owner decision. The prior E-terazi PNG/ICO remains in the Tur 07 ZIP; binary asset has not yet been imported into this source branch. UI displays a temporary Unicode scales glyph, which is not the approved logo.

GitHub Actions Windows build/test/publish evidence is the first technical gate. An artifact built by GitHub Actions is NOT physical Windows user acceptance. Do not merge main, create a release, or assert official-law parity.
