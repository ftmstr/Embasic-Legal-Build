# TUR010 — CMK 134 AYM iptal süreci, CMK 127/3 olay-tarihi güven kapısı ve Windows UAT

Tarih: 20 Kasım 2026. Ürün: .NET 10/WPF native Windows Alpha. Release/gerçek hukuk üretimi **NO-GO**.

## Kanıtın kaynağı ve gücü

**Resmî kurumsal karar yayımlama kaydı:** 25 Mayıs 2026 tarihli 33264 sayılı Resmî Gazete içerik listesinde, AYM'nin 12 Şubat 2026 tarihli **E.2023/128 K.2026/36** kararı açıkça listelenir: https://www.resmigazete.gov.tr/25.05.2026 .

**İkincil tam karar yeniden üretimi:** https://www.lexpera.com.tr/resmi-gazete/metin/anayasa-mahkemesinin-12-2-2026-tarihli-ve-e-2023-128-k-2026-36-sayili-karari-33264/70 . Haber ve hukuk incelemelerine göre CMK m.134'ün dijital arama/kopyalama/elkoyma hükümleri için iptal ve yayım tarihinden **dokuz ay sonra, 25.02.2027** yürürlük tarihi öngörülüyor. Özgün AYM Norm Kararlar Bilgi Bankası karar tam metninin belge baytları, resmi gazete imzalı ham karar, hash/sayfa/fıkra eşleme ve **sonraki kanun değişiklikleri** bu turda tamlık sertifikası olarak edinilmedi.

**Önemli hukukî ayrım:** 20.11.2026 itibarıyla henüz ileriye bırakılmış iptal hükmünü, CMK134'ün bugün otomatik yürürlükten kalktığı biçiminde sunmak hatalıdır. 25.02.2027 sonrası için de yeni kanun/değişiklikleri, Anayasa 153 uygulamasını ve somut olayın zamansal bağını görmeden geçerli norm ve işlem yetkisi hükmü verilmez.

Mevcut `CourtDecisionEvidenceRegister` Danıştay 2007 kararının İstanbul Barosu Dergisi'ndeki yeniden üretimi, 2012 onama ve 2025 Yönetmelik m.17/1 YD hakkında ikincil kayıtlar için yine **NOT_VERIFIED**. Resmî Danıştay karar arama / dosya sorgu portallarından bu üç kararın tam imzalı aslı doğrulanmadı.

## Somut kod farkı

- `Cmk134ConstitutionalCourtGate` aday kaynağı ve üç tarih sınırını (yayım öncesi, yayım–ileri yürürlük arası, sonrası) izler; eksik olay tarihi halinde de fail-closed. Tüm durumlar `LegalStatusAutomaticallyCertified=false`, `EnablesDigitalSearchAuthorization=false`.
- `Arama ve El Koyma Sistematiği` içinde AYM/CMK134 kaynak ve ileri yürürlük uyarısı ayrıca görünür; S24 dijital inceleme adımında bu tarihsel kaynak araştırması hatırlatılır.
- Olay tarihi değişirse hem AYM134/Court kaynak uyarısı hem de daha önce hesaplanmış CMK127/3 saat karşılaştırması eski/GEÇERSİZ olur; kullanıcı yeni inceleme yapmalıdır.
- **Mevcut** önceden hâkim kararı var/yok/belirsiz seçimi; 24/48 saat süre ayrımı ve otomatik hukuki karar yasağı aynen korunur.
- Kullanıcı metnindeki olay tarihi suç tarihi veya elkoyma tarihiyle otomatik özdeş sayılmaz; insan dosya üzerinde mevzuat yürürlük ilişkisini kurar.

## Windows UAT delil zinciri

`tools/TUR010-Windows-UAT.ps1` — TUR009 41 test + 5 odaklı CMK134 / olay tarihi denetimi = **46 manuel gözlem**. Windows'taki EXE/hash/BUILD_PROVENANCE doğrulamasına ek olarak kullanıcıdan GitHub Draft PR #11 sayfasındaki kaynak HEAD'i **paketten bağımsız kopyalamasını** ister. Bu da kullanıcı beyanı olup dijital imza değildir; dosyanın geçerlilik veya fiziksel GUI PASS sertifikası olamaz.

Bireysel kullanıcı gözlemleri P/F/N varsayılan N=NOT_RUN, kimliksiz ve yerel JSON+SHA makbuzudur; Drive/UYAP'a otomatik aktarım yok. Bu turda fiziksel Windows kullanıcı kabulü **NOT_RUN**. GitHub windows-2022 runner gerçek kullanıcı bilgisayarı gözlemi değildir.

**Korunanlar:** 2 tarih aracı, tek UYAP verisi gelmedikçe fail-closed Gece Vakti, 3 Sistematiği, ceza oran/hapis–para/YDO, E-terazi ICO/PNG, popup ve 2 tema, sentetik şifreli not sistemi.

**Açık kanıt/dağıtım kapıları:** TCK/CMK/Yönetmelik tam güncel ve geçmiş sürümler; 7571 geçişi; Danıştay onaylı karar asılları ve AYM134 eksiksiz tam metin; UYAP resmî güneş çizelgeleri; GPLv3/MIT çelişkisi, E-terazi yeniden dağıtım hakkı; gerçek Windows UAT. `main` merge, installer, tag, Stable release YASAK.
