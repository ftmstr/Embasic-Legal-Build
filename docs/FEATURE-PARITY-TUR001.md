# TUR 001 — HTML to WPF migration matrix

Evidence: inspected original v1.5 HTML and Alpha Tur 07 ZIP (19 November 2026).
The Tur 07 archive contains no Windows .exe, only HTML, CMD, PowerShell, Python, JS and test evidence.

| Original HTML feature | Target Windows behavior | TUR001 status |
|---|---|---|
| Age and date difference | One 'Belirli Tarih / Olay Tarihi' module with offset | WPF + C# implemented; on-device not verified |
| Date add/subtract days | One date shift tool accepting years/months/days | WPF + C# implemented; on-device not verified |
| Sunrise and sunset + TCK night | **One** screen named Gece Vakti, official UYAP chart only | WPF fail-closed placeholder; official chart unavailable |
| Calculator and sentence/monetary tools | Native C# calculations | Backlog, not implemented |
| Adli Hesap Makinası | Retain nonduplicated calculations | Backlog |
| 15 laws | Source-bound text + FTS + historical versions | 15-entry metadata directory only; NO VERIFIED TEXT |
| Article note bubble + sticker | Expandable WPF popup with local SQLite storage | Design accepted, implementation pending |
| Alternative solutions | Source-backed human-supervised workflows | Placeholder only |
| Detention / judicial control | Source-backed human-supervised workflows | Placeholder only |
| Search and seizure | Source-backed human-supervised workflows | Placeholder only |
| Light/dark theme | WPF resource dictionaries and local preference | Implemented, Windows acceptance pending |
| Independent popups | WPF detached window | Implemented, Windows acceptance pending |
| Text editor | Remove from current scope | Removed |
| 7571 evidence | Preserve transition and effective-date proof limits | Source ZIP retained externally, no automatic legal conclusion |

No legal source integrity PASS or physical Windows PASS is claimed.
