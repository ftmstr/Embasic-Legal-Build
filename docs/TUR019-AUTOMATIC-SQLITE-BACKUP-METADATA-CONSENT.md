# TUR019 — Otomatik ham SQLite yedeğinde açık üstveri onayı

Baz TUR018 stacked Draft PR #19 exact SHA fb779eff5365c46f598d1319638a80756f3e881f.

## Yeni, farklı P0 gizlilik açığı
WPF ExportBackup, SQLite law/article/version/timestamp alanlarının açık kaldığını EVET/HAYIR ile bildiriyor. Fakat NoteSandboxViewModel.Save ve Delete, hiçbir ek üstveri onayı olmadan aynı biçimde açık alanlar taşıyan otomatik .backup.sqlite3 yazıyor. Yeni sentetik risk düzeltmesi: RawMetadataAutoBackupConsent varsayılan ret; Save'de ilk otomatik yedek öncesi kullanıcıya EVET/HAYIR; HAYIR notu AES-GCM kasaya kaydeder ancak kopya üretmez, kurtarma eksikliği bildirilir. Delete'de rıza ve başarılı yedek olmadan hiçbir silme olmaz; reddedildikten sonra Delete ile tekrar açık onay mümkündür. Yetki yalnız açık kasa oturumunda; Unlock/Lock/SecureClose sıfırlar. Manuel dışa aktarma onayı korunur.

5 sentetik test: izin yoksa dosya yok; HAYIR'da şifreli not ama kopya yok; EVET'te SQLite metadata halen açık (gizlilik PASS değil) ve not içeriği AES-GCM; Reset eski yedeği korur; Delete onayı zorunlu. CI'da WPF işaret kontrolü, 82+5=87 fiziksel UAT maddesi. Testlerin kanıtı ancak exact HEAD Windows CI ile elde edilir.

Açık risk: var olan SQLite plaintext metadata, eski düz metin veritabanları/WAL/journallar, dışa taşınmış yedekler şifrelenmez ya da güvenli silinmez; anti-rollback için monotonic güven kökü bulunmaz. NTFS locking/TOCTOU/reparse/hardlink ve gerçek Windows UAT NOT_RUN. Gerçek soruşturma verileri Alpha'ya girilmemelidir.

7571 m.22 CMK128/A, m.26 Geçici8, m.39 resmî kanun: https://tbmm.gov.tr/Yasama/Kanun/3a5fb0fa-a80a-4569-8801-019ac5230ca6 ve https://cdn.tbmm.gov.tr/KKBSPublicFile/D28/Y3/KanunMetni/5189159c-4dec-44bf-87e0-9b23a810c791.htm . Olay tarihli tüm konsolidasyon ve belgeye özgü yetki NOT_VERIFIED. AYM E.2023/128 K.2026/36 RG 25.05.2026/33264 karar listesi resmî: https://www.resmigazete.gov.tr/25.05.2026 ; tam hüküm-fıkra/karar aslı hash NOT_VERIFIED. Danıştay 10.D E.2005/6392 K.2007/948, İDDK E.2007/2257 K.2012/1117 ve YD itiraz 2024/629 özgün kurum kararları NOT_VERIFIED.

Korunan: iki tarih aracı, tek fail-closed Gece Vakti, üç Sistematiği, Adli Ceza/Para/YDO, E-terazi ICO/PNG, iki tema, AES-GCM popup notları. E-terazi yeniden dağıtım, GPLv3/MIT lisans, hukuk tamlığı, fiziksel UAT ve stable/production NO-GO; main merge/tag/installer/release yok.
