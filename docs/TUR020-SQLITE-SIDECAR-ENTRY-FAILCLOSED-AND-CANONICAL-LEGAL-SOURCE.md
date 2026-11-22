# TUR020 — SQLite sidecar adı olarak dizin/girdi türü fail-closed kapısı

**Devralma:** TUR019 stacked Draft PR #20 exact SHA `b3af19f25a709a6c75d99f13fbc5e6e7b068b50d`; TUR020 stacked Draft.

## Somut farklı güvenlik açığı
`VaultArtifactRiskGates.RequireNoSqliteSidecars` TUR019 sürümünde yalnızca `File.Exists(sqlitePath+"-wal/-shm/-journal")` yapıyordu. `File.Exists` bir **dizin** üzerinde false döndürür; aynı sidecar adı bir dizin tarafından işgal edildiğinde "yan dosya yok" sanılıyordu. Bu yol aynı guard'ı kullanan SQLite backup, restore, legacy not göçü işlemlerini kapsar. Gerçek kişi verisiyle kullanılmadı; zarar görmüş canlı Windows soruşturma dosyası kanıtı değildir.

## Düzeltilen yöntem
`File.GetAttributes(candidate)` mevcut her dosya sistemi girdisini (dosya/dizin/ulaşılabilir reparse) fail-closed reddeder. Yalnız `FileNotFoundException` ve `DirectoryNotFoundException` yokluk kabul edilir, diğer IOException/UnauthorizedAccessException belirsizliğinde fail-closed. Hiçbir yan dosya/dizin silinmez, checkpoint veya normalleştirme yapılmaz. NTFS file-lock, junction/hardlink, race ve symlink/broken reparse sistem testi **kanıtlanmadı**.

**7 odaklı sentetik xUnit:** -wal/-shm/-journal dizin ret (3), encrypted backup mevcut hedef SHA korunması (1), şifreli restore preview/restore hedef korunması (1), plaintext legacy göç kaynak SHA korunması (1), olmayan klasör/yan dosyanın yanlış ret edilmemesi (1). Önceki 121 test korunarak toplam 128 beklenir; kesin sayı yalnız exact-HEAD GitHub Actions job loguyla PASS ilan edilir. Manual Windows UAT: 87 + 5 = 92, fiziksel kabul NOT_RUN.

## Mevzuat kanonik kaynak politikası (22.11.2026)
**Güncel/meri ve konsolide hükümler için yalnız `mevzuat.gov.tr` norm kanonik metin kaynağıdır.** TBMM kanun metinleri değişiklik kanununun kabul edildiği tarihteki özgün lafzını ve tarihsel yürürlük ihtimallerini göstermek için tarihsel referanstır; **güncel CMK/TCK sayılmaz**. Kullanıcı uyarısı üzerine TUR020'den itibaren eski turlardaki TBMM metninin 'güncel hukuk' olarak sunulması yasaklanmıştır.

Resmî meri/konsolide doğrulama adayları:
- CMK 5271: https://www.mevzuat.gov.tr/MevzuatMetin/1.5.5271.pdf
- TCK 5237: https://www.mevzuat.gov.tr/MevzuatMetin/1.5.5237.pdf
- e-Mevzuat arama: https://www.mevzuat.gov.tr/
Bu oturumda mevzuat.gov.tr web isteği timeout/502 döndürdüğü için **güncel tam metin BAYT, SHA-256, madde/fıkra, son değişiklik tarihi NOT_VERIFIED**; mevcut kanun maddesi otomatik PASS yapılmaz. Resmî değişiklik kanunu kimliği için TBMM 7571 m.22 CMK128/A, m.26 CMK Geçici8, m.39 yürürlük *yalnız tarihsel asıl*: https://cdn.tbmm.gov.tr/KKBSPublicFile/D28/Y3/KanunMetni/5189159c-4dec-44bf-87e0-9b23a810c791.htm . Sonraki değişiklikler ve olay tarihi ayrıca resmî e-Mevzuat / RG üzerinden belgelenir.

AYM E.2023/128 K.2026/36 (12.02.2026, RG 25.05.2026/33264) için AYM özgün karar metni/hüküm fıkrası henüz doğrulanmadı; ayrıca güncel 5271 CMK134'ün e-Mevzuat konsolidasyonu doğrulanmadan 'meri metin PASS' yok. Danıştay 10.D E.2005/6392 K.2007/948, İDDK E.2007/2257 K.2012/1117 ve YD itiraz 2024/629 için Danıştay karararama kurumsal karar asılları henüz NOT_VERIFIED. Mevzuat ve mahkeme kararları farklı kaynak kategorileridir; mahkeme karar aslı için AYM/Danıştay kurum kaynağı gereklidir.

## Korunan özellikler ve NO-GO
Tam iki tarih aracı, tek fail-closed Gece Vakti, üç Sistematiği (Tutuklama / Adli Kontrol / Arama ve El Koyma), Adli Ceza/Para/YDO, E-terazi ICO/PNG, açık-koyu tema, EAL4 AES-GCM şifreli popup notlar. SQLite açık kanun/madde/sürüm/zaman üstverisi, legacy plaintext/WAL/journal/yedekler, HMAC makbuzunun eşzamanlı rollback'i, NTFS TOCTOU/hardlink, fiziki Windows kullanıcı UAT **hala açık**. Gerçek soruşturma verisi Alpha'ya girilmez. E-terazi dağıtım hakkı, GPLv3/MIT lisans uyuşması, hukuk tamlığı ve production/stable **NO-GO**; main merge/tag/installer/release yapılmaz.
