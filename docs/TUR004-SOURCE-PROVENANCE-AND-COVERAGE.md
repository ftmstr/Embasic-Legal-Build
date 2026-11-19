# TUR004 — Official source verification: candidate-to-article gate

19 November 2026 research, based on public ministry and Official Gazette sites:
- TCK official UYAP page: https://mevzuat.adalet.gov.tr/mevzuat/103228
- CMK official UYAP page: https://mevzuat.adalet.gov.tr/mevzuat/102946
- Ministry older TCK PDF candidate: https://alternatifcozumler.adalet.gov.tr/Resimler/Mevzuat/TCK%205237.pdf
- Ministry older CMK PDF candidate: https://alternatifcozumler.adalet.gov.tr/Resimler/Mevzuat/CMK%205271.pdf
- 7571 original act at Official Gazette: https://www.resmigazete.gov.tr/eskiler/2025/12/20251225-33.htm (25 December 2025 issue 33118).
- UYAP sunrise/sunset feature existence is directly confirmed in the official UYAP service list: https://www.uyap.gov.tr/Hizmetler
- An Adalet Bakanlığı courthouse site links its **Güneş Doğuş Batış Saatleri** menu directly to https://bigm-uyap.adalet.gov.tr/portal/zaman/zaman.html : https://duzce.adalet.gov.tr/ . Direct web fetch of that portal route returned a cache miss. This is a **discovered official entrypoint**, not an authenticated/redistributable schedule.
- UYAP night chart cited in jurisprudence: https://mevzuat.adalet.gov.tr/ictihat/858039400

## What changed
- LocalPdfEvidenceStore.Recheck cross-verifies a saved candidate's identity, exact local copy, JSON receipt and SHA-256. A changed/missing file fails closed. A correct result is byte integrity only.
- ArticleCoverageLedger accepts separately generated article-page fragments and expected article IDs, finds missing/unlisted/duplicate page entries and returns only provisional structural consistency.
- Even structurally complete **synthetic** data explicitly has OfficialFullTextVerified=false, HistoricalEffectiveDateVerified=false and NoteBindingAuthorized=false.
- The WPF local PDF source-admission screen now immediately rechecks the receipt after the copy before displaying mechanical success.
- ArticlePageFragment SHA fingerprints are locally computed for bookkeeping and do NOT certify accurate PDF extraction or original official source.

## Source attempts and blockers
The Ministry TCK PDF was indexed as parsable web text, but the actual source PDF request could not be retrieved as complete raw bytes in this turn (download failure, and web PDF screenshot HTTP 404). It was NOT accepted as an official current TCK 5237 text or bundled into EXE.
Official CMK PDF source bytes also remain unavailable. No independent 5237/5271 full article and pagination, amendment effective-date reconciliation, original SHA from authenticated bytes, or complete cross-check has passed.
7571 is officially published in RG 33118, but coverage and all exception/transition provisions cannot be reduced to a blanket case-specific legal result. CMK Geçici 8 and effective-date facts require human review.
The authentic complete Adalet Bakanlığı/UYAP sunrise/sunset chart still has no verified downloadable original dataset, year/area coverage, timezone specification or distributable license. Exactly one Gece Vakti screen remains fail-closed.

## Real-law article binding gate
No official-source signed/reviewed article-version manifest has been created, and the app will **not** bind the note bubble to actual law rows or claim full text official PASS. It only renders the clearly synthetic demonstration card.
To activate: verify source institutional origin, source bytes+SHA and date, article spans and page positions against source, historical amendments/repeals, 7571 entry/transition, independent legal review and licensed redistribution, then implement a trusted version-to-note binding. Do not infer an approval from a checksum or user-supplied evidence flags.
