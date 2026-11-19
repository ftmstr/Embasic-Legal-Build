# TUR002 — Native Windows targeted quality and evidence gates

## Existing evidence and nonduplicated work
TUR001 Draft PR #1 HEAD 463057ee6d52272ded2c81741c37b36382d2e313
already passed GitHub Windows push + PR CI, 7/7 xUnit and self-contained EXE.
TUR002 begins from that exact HEAD. TUR007 historic ZIP still contains HTML, not WPF or EXE.

## New implementation
- Original E-terazi PNG + ICO imported as native assets, application window image and executable icon.
- Original source bytes checked for provenance; rights and GPL-3.0/MIT conflict remain unresolved.
- Two existing date modules retained; added explicit copy-result actions and new edge-case tests.
- Microsoft.Data.Sqlite local ArticleNoteStore with pooling disabled to ensure Windows file-handle release, version-keyed notes, parameterized queries,
  unicode, update-in-place and delete, no accidental remap across historical legal versions.
- Strict PDF byte+SHA integrity checks are deliberately NOT a certificate of official source,
  article coverage, text equivalence, legal effect or case applicability.
- Exactly ONE Gece Vakti screen remains fail-closed until authentic Adalet Bakanlığı/UYAP chart.

## Human approval / test boundaries
SQLite note storage is backend infrastructure only; the expanding note balloon and sticker
UI are NOT yet implemented. 15 law metadata labels are NOT official verified text.
The notes database is NOT encrypted; do not store sensitive case content in Alpha.
No official TCK/CMK source PDF raw bytes, verified source matching, historic temporal-effect
attestation or 7571 case-applicability proof has been obtained.
GitHub Windows CI != physical Windows end-user acceptance.

## Distribution
No main merge, installer, code signing or release. ALPHA / LEGAL PRODUCTION NO-GO.
