# TUR032 — Ceza Muhakemesi Hukuku WPF işlem sistematiği sekme kayması

**Önceki kanıt:** TUR031 stacked Draft PR #32 exact HEAD `484ba7402c81cc000cc6fcd7764e84fcee104c30`; Windows Push #45158830901 ve PR #45158885511 SUCCESS, xUnit 198/198, STA 42/42. Özgün ZIP Drive ID `1qCSb7OPB_2ZG26QMSo1m1QRv_ZOoKvur`, rapor ID `1js8iCkz3W5-iQRVsMJtfZw-zhPmdlka9`. İleri TUR032 dalı/PR yoktu.

## Tek kaynak-kod düzeyinde doğrulanan gerçek navigasyon regresyonu
`MainViewModel.OpenProcedure` `_learningHub.SelectedTabIndex = 6;` atıyordu. Oysa `App.xaml` içindeki CMK merkezinin `TabControl` bölümünde önceki TUR030 yeni `5 · Tekrar` sekmesi eklenince `6 · Kaynak ve güvenlik` sekmesinin indeks değeri **6**, asıl **İşlem sistematiği** sekmesinin indeks değeri **7** oldu. Kod tab-index **6** seçtiği için Tutuklama, Adli Kontrol ve Arama/El Koyma derin bağlantıları kullanıcıyı uygulanacak işlem yerine kaynak ekranına yönlendiriyordu. Kaynak ve sabit sekme sıralaması bu kusuru kanıtlar; **gerçek Windows GUI gözlemi yapıldı iddiası değildir**.

## Tekil ve minimal onarım
`CmkLearningHubViewModel.ProcedureTabIndex = 7` tek anlamlı referans olarak tanımlandı. `MainViewModel.OpenProcedure` bu sabiti kullanır. Mevcut **Windows STA NavigationSmoke** testlerinde Tutuklama, Adlî Kontrol ve Arama/El Koyma doğrudan girişlerinin gerçek indeks **7** seçmesi kontrol edilir (önceki 42 kontrolün niteliği güçlendirilir; sahte test çoğaltılmaz). CI PowerShell aşamasında gerçek WPF `App.xaml` TabItem başlık sırası çıkarılır ve `İşlem sistematiği` konumu `ProcedureTabIndex` ile eşleştirilir; kaynak sekmesi ile aynı indeks ise **fail closed**. Böylece gelecekte yeni sekme eklenmesi aynı yanlış yönlendirmeyi sessizce geri getiremez. Mevcut TUR031 kart/tablo görünümleri ve sesli kullanıcı erişimi iddiaları değişmez.

Fiziksel Windows 11 için mevcut 155 maddelik manuel UAT'ye 3 yeni gözlem eklenir (Tutuklama, Adlî Kontrol/Arama, ayrı pencere+klavye/Narrator). Linux container ortamında `dotnet`/Windows WPF ve gerçek GUI bulunmadığından **fiziksel UAT = NOT_RUN**. Windows GitHub Actions STA/derleme veya CI görsel kanıt yerine geçmez.

## Koruma ve hukuk kapıları
47 merkezî Ceza Muhakemesi Hukuku dersi, 94 soru, 141 hatırlama kartı, TUR030 tekrar gündemi, TUR031 karşılaştırma kartları, ayrı Alternatif Çözümler, Tutuklama/Adlî Kontrol/Arama ve El Koyma üç gerçek sistematiği, iki tarih, tek fail-closed Gece Vakti, Adli Ceza/Para/YDO, E-terazi PNG/ICO, açık/koyu tema ve EAL4 AES-GCM not/backup/restore koduna müdahale edilmedi.

Güncel/meri/konsolide mevzuatta **yalnız mevzuat.gov.tr** kanoniktir. Bu turda https://www.mevzuat.gov.tr/ adresi web erişiminde timeout verdi. Bugünkü CMK 5271/TCK 5237 resmî tam metin/vade/fıkra/olay tarihi belgeleri **NOT_VERIFIED**; TBMM ilk kabul metni güncel konsolide normun yerine geçmez. AYM ND/2026/36 resmî portal JS shell döndürdüğü için kararın tam metni **NOT_VERIFIED**, Danıştay resmî karar arama ekranı erişilebilir ancak ilgili kararların özgün tam metni/safahat **NOT_VERIFIED**. Kanun maddeleri ve karar hükümleri uydurulmadı.

Fiziksel Windows 11 (900 DIP, %125/%150 DPI, klavye, Narrator, ayrı pencere) UAT **NOT_RUN**; lisans/E-terazi yeniden dağıtımı **NOT_VERIFIED**, production **NO-GO**. Eski plaintext SQLite metadata/yedekler, anti-rollback ve TOCTOU sınırları açık. Gerçek soruşturma/kimlik verisi kullanılmaz. `main` merge/tag/installer/release/canlı yayın yok.
