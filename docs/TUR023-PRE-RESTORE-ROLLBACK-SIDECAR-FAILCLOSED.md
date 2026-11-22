# TUR023 — Önceki kasayı koruyan .pre-restore.sqlite3 hedefinde yan dosya fail-closed

**Baz:** TUR022 stacked Draft PR #23 exact HEAD `6e6c19eb77f1825126c12e4cdc0bffeb884b56b7`.

## Benzersiz güvenlik kusuru
`EncryptedArticleNotes.RestoreFrom` TUR022'de `backup` ve `target` SQLite -wal/-shm/-journal yan dosyalarını ve hashleri kontrol ediyordu; ancak `File.Replace(staged,target,rollback)` için üretilen üçüncü kritik dosya `target + ".pre-restore.sqlite3"` yolunda **yalnız `File.Exists(rollback)`** kontrolü vardı. Rollback ana dosyası yokken eski `rollback-wal`, `rollback-shm`, `rollback-journal` kalıntısı fark edilmeden yeni rollback dosyası üretilebiliyor; gelecekte okunan geri alma dosyası ve kanıt bütünlüğü tehdit altında kalabiliyordu. Rollback adının dizin olması da `File.Exists == false` idi. Fiilî Windows veri kaybı kanıtı değil, kaynak düzeyinde fail-open denetim kusuru.

## Düzeltme
`VaultArtifactRiskGates.RequireUnusedRollbackDestination`: yalnızca `File.GetAttributes(rollback)` ile ana dosya/dizin/reparse işgali ve `RequireNoSqliteSidecars(rollback)` ile bütün üç yan dosya adını denetler. Var olan/belirsiz her girdi reddedilir. `RestoreFrom` yalnız var olan hedefte, evvelce onaylanan snapshot için *staging'den önce* ve `File.Replace` hemen öncesinde tekrar çağırır. Hiçbir rollback, yan dosya veya hedef kendiliğinden silinmez. Bu işlem **değişmez NTFS file-handle kilidi, symlink/hardlink veya TOCTOU güvenliği sağlamaz**.

## Sentetik testler
Beş odaklı xUnit: orphan rollback `-wal`, `-shm`, `-journal` için onaylı restore reddi, hedef/yedek/kalıntı SHA ve bayt koruması (3); rollback yolunun dizin işgali (1); yan dosyasız yolda onaylı restore + yeni rollback dosyasının AES-GCM ile bağımsız açılması (1). Önceki 140 + 5 = **145 beklenen** (kesin PASS yalnız exact-HEAD Windows CI günlüğüyle). 104 + 5 = **109** fiziksel Windows manuel UAT kalemi; kullanıcı UAT **NOT_RUN**.

## Güncel mevzuat — kanonik kaynak ve kanıt sınırları
Güncel/meri/konsolide TCK/CMK ve tüm mevzuat metni için yalnız **https://www.mevzuat.gov.tr/** kullanılır. CMK 5271 resmi PDF aday adresi https://www.mevzuat.gov.tr/MevzuatMetin/1.5.5271.pdf ; TCK 5237 resmi PDF aday adresi https://www.mevzuat.gov.tr/MevzuatMetin/1.5.5237.pdf; 22.11.2026 doğrudan erişim her üçünün de timeout vermesi nedeniyle güncel belge baytları/tarih/madde hash **NOT_VERIFIED**. TBMM 7571 ilk kabul metni değişiklik tarihini destekleyen *tarihsel* kaynaktır, bugünkü konsolide 5271/5237 değildir. AYM E.2023/128 K.2026/36 CMK134 özgün tam karar aslı JS gerektiren portal üzerinden **NOT_VERIFIED**; Danıştay 10. Daire E.2005/6392 K.2007/948, İDDK E.2007/2257 K.2012/1117, YD 2024/629 özgün tam karar/safahat **NOT_VERIFIED**. Hukukî uygulanabilirlik ya da olay tarihli onay iddiası yok.

## Korunan işlevler ve kapılar
Tam iki tarih modülü, tek fail-closed Gece Vakti, üç Sistematiği (Tutuklama/Adli Kontrol/Arama ve El Koyma), Adli Ceza/Para/YDO, E-terazi ICO/PNG, açık-koyu tema, EAL4 AES-GCM popup notlar, otomatik ham SQLite yedek gizlilik onayı, restore/legacy/HMAC korunur. SQLite LawNumber/ArticleKey/SubdivisionKey/VersionKey/timestamp/satır sayısı şifresiz; eski plaintext/WAL/journal/yedek ve HMAC eşzamanlı rollback riskleri açık. Windows gerçek masaüstü fiziksel UAT, NTFS handle-TOCTOU-reparse/hardlink, E-terazi dağıtım hakkı ve GPLv3/MIT lisans **NOT_VERIFIED/NO-GO**. Gerçek soruşturma verisi Alpha'da kullanılmaz; main merge, tag, installer/release YOK.
