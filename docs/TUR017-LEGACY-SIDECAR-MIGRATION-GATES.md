# TUR017 — Eski şifresiz SQLite göçünde -journal/WAL/SHM fail-closed kapısı

## Sürüm ve kapsam
Kaynak TUR016 exact HEAD `ba888357394c4801aaf3adb0ba7284e75848ea31`, Draft PR #17. TUR017 yalnızca eski SQLite dosyasından şifreli kasaya *açık onaylı* göç kapısındaki eksik yan dosya denetimini ele alır. Şifreli not formatını, kullanıcı ekranlarını veya kanun içeriklerini değiştirmez.

## Somut benzersiz P0 kusuru
`LegacyNoteMigration.ValidPath` yalnız `-wal` ve `-shm` dosyalarını reddediyor, `-journal` rollback dosyasını ise görmezden geliyordu. Ana SQLite dosyasının SHA-256'sı değişmese de ayrı rollback journal kaydedilmemiş veya önceki hassas sayfaları içerebilir. Bu nedenle canlı/yarım kalmış kaynak dosyanın göçü veya journal'ın doğrulama dışında kalması, veri bütünlüğü ve gizlilik riskidir.

## Düzeltme
`LegacyNoteMigration`, TUR015'te restore için kullanılan `VaultArtifactRiskGates.RequireNoSqliteSidecars` denetimini göçte de kullanır; `-journal`, `-wal`, `-shm` varlığını kaynak yolu doğrulamasında, ön inceleme sonunda, Apply öncesinde, yedek öncesinde ve yedek sonrasında denetler. Son dosya SHA da yeniden karşılaştırılır. SQLite bağlantısı salt-okunur + `PRAGMA query_only=ON` açılır. **Hiçbir yan dosya otomatik silinmez veya checkpoint edilmez.** Dış süreç TOCTOU/reparse/snapshot ve dosya kilitleri garanti edilmez.

Beş yeni sentetik xUnit vakası: journal/wal/shm ön inceleme reddi (3) ve ön incelemeden sonra journal/wal eklenmesi halinde göç, şifreli notlar ve rollback korunması (2). Mevcut göç başarı/uyumsuzluk testleri korunur. Fiziksel UAT scripti 72+5=77 manuel kontrol. Bu script üretim PASS üretmez.

## Hukukî kanıt düzeyi
7571 Kanun m.22 CMK128/A, m.26 CMK Geçici8 ve m.39 TBMM resmî tam kanun metni: https://cdn.tbmm.gov.tr/KKBSPublicFile/D28/Y3/KanunMetni/5189159c-4dec-44bf-87e0-9b23a810c791.htm . Olay tarihli bütün norm sürümleri/konsolide mevzuat doğrulanmadığından otomatik hüküm veya usul PASS yok. AYM E.2023/128 K.2026/36 ve 25.05.2026/33264 RG listesi resmî kayıtlarda görüldü; tam karar orijinal bayt/fıkra eşlemesi doğrulanamadı. Danıştay 10. Daire E.2005/6392 K.2007/948, İDDK E.2007/2257 K.2012/1117, YD itiraz 2024/629 özgün Danıştay karar asılları ve sonraki safahat **NOT_VERIFIED**. Kaynak kapısı **NO-GO**.

## Korunan işlevler / üretim sınırı
Tam iki tarih aracı; tek Gece Vakti (resmî çizelge yoksa fail-closed); Tutuklama, Adli Kontrol, Arama ve El Koyma üç Sistematiği; Adli Ceza/Para/YDO; E-terazi ICO/PNG; açık/koyu tema; popup ve AES-GCM EAL4 not. Metadata law/article/version/timestamps plaintext kalır; eski şifresiz dosya/WAL/backuplar kullanıcı verisi olarak otomatik kaldırılmaz. HMAC witness birlikte rollback'e karşı güvenilir monotonic kanıt değildir. **Fiziksel Windows UAT NOT_RUN**; lisans, mahkeme aslı, tamamlanmış hukuk corpus'u, stable/production **NO-GO**.
