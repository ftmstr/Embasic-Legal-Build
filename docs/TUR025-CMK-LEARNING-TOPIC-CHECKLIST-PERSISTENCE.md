# TUR025 — CMK 74+ öğreticide konu değişince kontrol işaretlerinin kaybolması

**Baz:** TUR024 stacked Draft PR #25 exact HEAD `ceb9e08f52d444b779325a12647268e0410e50fb`.

## Somut tekrar edilmeyen WPF kullanıcı deneyimi kusuru
`CmkLearningHubViewModel.SelectedTopic` setter'ı her seçimi `RebuildChecklist()` ile yeniden inşa ediyordu. Eski `new CmkLearningChecklistRow(step)` constructor başlangıçta `Reviewed=false` tutuyordu. Örneğin CMK74'te iki işaret konulup CMK134'e geçildiğinde ve geri dönüldüğünde ilk iki işaret yok oluyor; quizin oturum genelindeki puanı ise korunuyordu. Bu **öğrenme ilerlemesinde kanıtlı kayıp/tutarsızlık**; hukukî veya mahkeme onayı değildir.

## Yeni tekil düzeltme
`CmkLearningChecklistSession` (Core .NET10) RAM-only `Dictionary<string,HashSet<int>>` ile **konu anahtarı / kontrol adım numarası** bazında `SetReviewed`, `IsReviewed`, `ReviewedCount`, `Reset` sunar. Bilinmeyen konu, geçersiz adım reddedilir. ViewModel'de `RebuildChecklist()` konuya dönüldüğünde önceki işaretleri yükler, `PropertyChanged` değişiklikleri anında Core hafızaya yazar; görünür konu sayacı güncellenir. Genel eğitim puanını tutan mevcut `_answered/_correct` sözlükleri ve 26 senaryo **değişmez**. Kullanıcının açıkça onayladığı `ResetSession` hem puanı hem **13 konudaki bütün işaretleri** temizler; konu değişimi silmez. Disk/ağ/Cloud/UYAP aktarımı yok.

**8 odaklı xUnit test metodu, teori kombinasyonlarıyla**: CMK74-CMK134 konu gidiş/dönüşü ve ayrıklık; işaret kaldırma; 13 konu reset; tüm kontroller işaretlense de `LegalComplianceCertified=false` ve `NOT_VERIFIED`; tekrar idempotent; geçersiz adım; bilinmeyen konu; 26 senaryo ve quiz kapısının etkilenmemesi. Eski 156 test korunacak; kesin sayı yalnız exact-HEAD Windows CI.

`tools/TUR025-Windows-UAT.ps1` üç yeni fiziksel gözlem ekler: CMK74 iki işaret gidiş-dönüşü; CMK134 ayırımı; tüm konular ve quiz reset. **Fiziksel Windows UAT NOT_RUN**. UAT gerçek maddeleri paket üzerinde yeniden sayılmalı; eski tahminler doğrulanmış sayı değildir.

## Korunan kanon ve işlevler
**Güncel/meri/konsolide CMK/TCK ve diğer mevzuat yalnız `https://www.mevzuat.gov.tr/`.** Önceki 22.11.2026 doğrudan site/PDF timeout; resmî corpus byte/version/tarih/fıkra `NOT_VERIFIED` kalır; kullanıcı talimatına aykırı biçimde TBMM ilk kabul metni ikame edilmez. AYM/Danıştay karar asılları ayrı resmî kurum kaynaklarında doğrulanmadı: `NOT_VERIFIED`. Quiz ve kontrol listesi hiçbir koşulda karar veya adlî işlem için hukukî `PASS` üretmez.

**Korundu:** 13 konu, 26 senaryo, 5 WPF sekme, iki tarih aracı, tek fail-closed Gece Vakti, üç Sistematiği, Adli Ceza/Para/YDO, E-terazi kaynak asset, açık/koyu tema, AES-GCM şifreli notlar/backup/restore. Mevcut SQLite plaintext üstveri/legacy WAL/journal/HMAC rollback ve NTFS TOCTOU riskleri devam eder. Fiziksel Windows UAT, lisans/E-terazi dağıtım, güncel hukuk ve production **NO-GO**. Main merge/tag/installer/release yok.
