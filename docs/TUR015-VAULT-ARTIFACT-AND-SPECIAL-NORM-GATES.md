# EMBASIC LEGAL Windows 2.0 — TUR015 güvenlik ve hukuk kanıt kapıları

**Stack:** TUR014 exact HEAD `c644e0d376839c0469c74a3e1a31bb4b29fe2243`, Draft PR #15. Çalışma TUR015 stacked Draft PR; production NO-GO.

## Güvenlik: somut ve sınırlı düzeltmeler

1. **HMAC makbuzunu aynı klasöre yazmayı reddet.** `VaultArtifactRiskGates.RequireWitnessOutsideVaultDirectory` yalnız normalleştirilmiş klasör yolunu denetler. Diğer klasör/sürücü tek başına güvenilir harici zaman damgası, imza veya monotonic counter değildir; kasa ile makbuz birlikte geri alınırsa HMAC uyuşması yine mümkündür.
2. **SQLite restore'da WAL/SHM/journal ön koşulu.** Yedek ve hedef SQLite yan dosyaları varsa açılış ön incelemesi ve geri yükleme reddedilir. Kaynak ve hedefin disk üzerinde değişimi TOCTOU ile devam edebilir: canlı başka proses, symlink veya gerçek Windows atomik dosya kilidi kanıtlanmadı. Mevcut sidecar otomatik silinmez.
3. **Dışa aktarımda bilinçli metadata onayı.** WPF, şifreli not gövdesine rağmen SQLite law/article/version IDs, note count ve timestamps alanlarının açık olduğunu, yedek dosyası oluşturulmadan önce EVET/HAYIR ile bildirir. Bu, şifreli üstveri veya güvenli yedek şifrelemesi **değildir**.
4. **Odaklı sentetik testler:** makbuz klasör sınırı, WAL ve journal ret, gerçek metadata sızıntısının kanıtı, çift-dosya rollback'in HMAC tarafından tespit edilmediği karşı-örnek. Sonuncu test **güvenliğin kanıtı değil**, bilinen sınırın testidir.

## 7571 özel norm ve olay tarihi

Birincil kimlik: TBMM Kanun No 7571, kabul 24.12.2025; RG 25.12.2025/33118. Resmî Kanun metni https://www.resmigazete.gov.tr/eskiler/2025/12/20251225-33.htm .

- **m.22 CMK128/A:** yalnız TCK 142/2-e, 158/1-f,l ve 245 kapsamı; bankanın, ödeme kuruluşunun veya kripto varlık hizmet sağlayıcısının 48 saate kadar askıya alma işlemi ile savcılık/hâkim müdahalesi farklı aşamalardır. 24/48 saatli özel elkoyma süreci **CMK127/3 ile otomatik eşitlenmez**. Tarih, suç vasfı, somut evrak, karar yetkisi ve kapsam ayrı doğrulanmadıkça **NOT_VERIFIED**.
- **m.26 CMK Geçici 8:** yürürlük tarihinde soruşturma/kovuşturma aşamasında olup uzlaşması sağlanmış dosyaların 253/3 ve TCK75/6 geçişi korunur. Uzlaşma sağlama günü ve evre belgesizse **NOT_VERIFIED**.
- **m.39:** m.11 26.12.2025; diğer maddeler, 22 ve 26 dâhil, RG'de yayımlandığı 25.12.2025 günü yürürlük. Bu yalnız 7571 değişikliğinin tarih eşiğidir; değişikliğin geçmiş olaylara lehe yasa etkisi, uygulama koşulları ve sonraki değişiklikler otomatik çözümlenmez.

## AYM ve Danıştay birincil kaynağa erişim sınırları

AYM E.2023/128 K.2026/36 12.02.2026; RG 25.05.2026/33264 listesinde yer alıyor. Norm Kararlar Bilgi Bankası kaydı ve RG liste kimliği birincil doğrulandı; resmî karar tam metni PDF/HTML baytları, hüküm-fıkra ve bütün daha sonraki normlar exact hash ile doğrulanmadı. 9 aylık ileri yürürlük uyarısı 25.02.2027'dir; geçiş sonrası sonuç için otomatik PASS yok.

Danıştay 10. Daire E.2005/6392 K.2007/948; İDDK E.2007/2257 K.2012/1117 ve İDDK 23.01.2025 YD İtiraz 2024/629 kararları için Danıştay karararama portalı birincil karar aslına eşdeğer belge sağlamış değil: **original full decision NOT_VERIFIED**. Sonraki safahat ve yürürlük ayrıca açık.

Eski notlar ve önceki iki tarih aracı / tek Gece Vakti / üç Sistematiği / Adli Ceza-Para-YDO / E-terazi / açık-koyu tema korunur. Hukuk veri tamlığı, lisans, fiziksel Windows UAT ve release **NO-GO**.

## Kod düzeyinde olay tarihli özel norm incelemesi

`src/Embasic.Legal.Core/Cmk7571SpecialNormGate.cs` m.22 için yalnız tam suç varyantları 142/2-e, 158/1-f, 158/1-l ve 245 ile **işlem tarihi** / olay tarihi ayrımını, m.26 için 25.12.2025 itibarıyla uzlaşma ve o tarihteki derdestlik belge durumunu salt mekanik inceler. Yayım günü uzlaşmasının gün içi kanıtı belirsiz tutulur. Ne CMK128/A izni ne Geçici 8 geçiş uygulanabilirliği otomatik PASS olabilir. `tests/Embasic.Legal.Tests/Tur015Cmk7571SpecialNormTests.cs` odaklı sentetik testler içerir; WPF hukuk ekranında gerçek mevzuat belgesi gibi sunulmaz.
