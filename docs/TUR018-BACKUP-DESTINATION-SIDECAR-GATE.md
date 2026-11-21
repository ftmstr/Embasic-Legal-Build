# TUR018 — SQLite yedek hedefinde WAL/SHM/journal fail-closed, benzersiz P0

**Baz:** TUR017 Draft PR #18 exact HEAD `81eac7c4c433067dc7683e520c7dede0b3f5fb79`. TUR018 stacked Draft PR, main NO-GO.

## Kanıtlanan kusur

`EncryptedArticleNotes.BackupTo` ve `ArticleNoteStore.BackupTo`, hedef yedek ana SQLite dosyasını atomik `File.Move(...,overwrite:true)` ile değiştirirken **hedefe ait mevcut** `-wal`, `-shm`, `-journal` yan dosyalarını denetlemiyordu. Eski bir yedeğin yan dosyaları yeni ana dosyayla aynı ad altında kalabilir ve ana dosyanın tekil SHA-256'sı bunu kapsamaz. Bu risk kaynak SQLite'ın canlı WAL durumundan ayrıdır: SQLite online backup kaynakta tutarlı snapshot sağlayabilir; hedefte kalmış yan dosyayı temizlemez.

## TUR018 kod ve test

Her iki yedekleme yönteminde de mevcut `VaultArtifactRiskGates.RequireNoSqliteSidecars(destination)` çağrısı hem işlem **öncesinde**, hem de staged snapshot doğrulamasından sonra **hedefin üzerine yazılmadan hemen önce** uygulanır. Ret halinde kaynak ve mevcut hedef yedek korunur; yan dosyalar asla silinmez veya checkpoint edilmez. Şifreli yedek için tam AES-GCM not doğrulaması değişmez. Daha önce alınmış onaylı veri yedeğinin sessiz üzerine yazılması kullanıcı işlemine bağlıdır; bu tur ayrı izin akışı eklemez.

**7 sentetik test:** şifreli hedefte journal/WAL/SHM ret + dosya SHA/yan dosya baytları korunması (3); eski düz metin yedekte aynı üç ret (3); temiz şifreli hedefte sorunsuz yeniden yedek ve not decrypt (1). Mevcut 109 testle beklenen toplam 116. Fiziksel Windows TUR018 kabul listesi 77+5=82 maddelik kullanıcı gözlem aracıdır, CI simülasyonu değildir.

## Yüksek öncelikli açık sorunlar

- EAL4 not gövdesi AES-GCM şifrelidir, ancak kanun/madde/sürüm/timestamp ve kayıt sayısı SQLite'ta **plaintext**; tüm SQLite tam şifreli bir şemaya taşınmadı
- Eski düz metin ana dosyalar, journallar, WAL ve farklı klasörlerdeki yedekler otomatik aranıp yok edilmez
- EAL4 HMAC bütünlük makbuzu bağımsız güven kökü/monotonic sayaç değildir; kasa+makbuz birlikte geri döndürülebilir
- TOCTOU, junction/reparse, NTFS lock, hardlink ve fiziksel dosya sistemi yarışı tam çözümlenmemiştir
- Fiziksel Windows 11 masaüstü UAT **NOT_RUN**, gerçek soruşturma verisi Alpha'ya girilmez

## Olay tarihli resmî hukuk sınırı

- 7571 sayılı Kanun TBMM **resmî tam metni** m.22 CMK128/A ve m.26 CMK Geçici8, m.39 yürürlük: https://cdn.tbmm.gov.tr/KKBSPublicFile/D28/Y3/KanunMetni/5189159c-4dec-44bf-87e0-9b23a810c791.htm . Olay tarihli CMK/TCK, ara değişiklikler, özel suç varyantları, uzlaşma/geçiş evrakı tam **NOT_VERIFIED**
- AYM E.2023/128 K.2026/36 12.02.2026, 25.05.2026/33264 Resmî Gazete kaydı: https://www.resmigazete.gov.tr/25.05.2026 . AYM kurum açıklaması belirli CMK134 cümlelerinin iptalini ve 9 aylık ileri yürürlüğü doğrulasa da tam kararın resmî indirilebilir bytes/hash ve tam hüküm-fıkra eşlemesi bu turda **NOT_VERIFIED**; otomatik hukuk PASS yok
- Danıştay 10. Daire E.2005/6392 K.2007/948; İDDK E.2007/2257 K.2012/1117; YD itiraz 2024/629: https://karararama.danistay.gov.tr/ — belirtilen kararların mahkeme asılları ve sonraki safahat **NOT_VERIFIED**

## Uygulama / üretim kapıları

Tam 2 tarih, tek fail-closed Gece Vakti, üç Sistematiği (Tutuklama / Adli Kontrol / Arama ve El Koyma), Adli Ceza/Para/YDO, E-terazi ICO/PNG, iki tema, popup ve EAL4 AES-GCM not/yedek/restore korunur. GPLv3/MIT uyuşmazlığı ve E-terazi dağıtım hakkı **NO-GO**. Ana dala merge/tag/release/installer yapılmaz, stable/production **NO-GO**.
