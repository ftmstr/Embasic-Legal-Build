# Embasic Legal TUR009 — Elkoyma karar dayanağı, 24/48 saat ve Windows Alpha UAT

20 Kasım 2026, stacked Draft. **Legal production NO-GO, physical Windows UAT NOT_RUN.**

## Kod tasarımı

`SeizureAuthorizationGate`, CMK 127/3'ün yalnız önceden hâkim kararı **olmaksızın** yapılan elkoymaya ilişkin mekanik 24/48 saat incelemesine geçiş sağlar.

| Kullanıcı seçimi | Yazılım incelemesi | Hukuki/evrak onayı |
|---|---|---|
| Dayanak belirsiz (varsayılan) | Saat karşılaştırması **yapılmaz**, kaynak istenir | NO-GO |
| Önceden hâkim kararı var (kullanıcı beyanı) | Kararsız elkoymaya özgü 24/48 aralığı otomatik uygulanmaz | Asıl karar kapsam/geçerlilik/onay **yok** |
| Önceden hâkim kararı yok (kullanıcı beyanı) | CMK 127/3 24/48 referans hesabına geçilir | Emrin yetkisi/olay tarihi/evrak/güncel hukuk/karar açıklanması **doğrulanmaz** |

**CMK 127/3 süreler:** İlk ölçüm elkoyma anı → hâkime sunma; 24 saat. İkinci ölçüm elkoyma anı → hâkim kararının açıklanması; 48 saat. İkincisinin başlangıcı hâkime sunma değildir. İki aralık sadece açık zaman dilimi farklılıkları hesaba katılarak hesaplanır. Kararsız elkoymada kanunda öngörülen kendiliğinden kalkma sonucunu **uygulama otomatik ilan etmez**; somut olayda yürürlük ve hukuki sonuç insan tarafından belirlenir.

Kaynak (Adalet Bakanlığı/UYAP m.127): https://mevzuat.adalet.gov.tr/mevzuat/102946?query=Madde+127

Uygulamada seçilen dayanak **yalnız kullanıcı beyanıdır**. `SeizureAuthorizationReview`'ın `BasisDocumentAuthenticityVerified`, `NormVersionVerified`, `CourtDecisionCertified` ve `LegalConsequenceApproved` alanları hep false'dur. Tanımlanmayan enum değeri de fail-closed kalır.

Bütün önceki TUR005 51 sistematik kontrolü, TUR006 arama kaynak riskleri ve TUR007/008 belge-saat geçersizleşmesi korunur. İlgili WPF ComboBox yalnız Arama ve El Koyma Sistematiği içinde yer alır; ayrı tarih hesaplayıcısı eklenmez.

## Danıştay hüküm/kaynak derecesi

1. Danıştay 10. Daire 13.03.2007 E.2005/6392 K.2007/948: İstanbul Barosu Dergisi'nde yayımlanan **karar yeniden üretimi**, mahkemece onaylı asıl değil.
2. Danıştay İDDK 14.09.2012 E.2007/2257 K.2012/1117: Bakanlık Yönetmelik dipnotu ve HSK kaynaklarında iptal onaması; **mahkeme aslı edinilmedi**.
3. Danıştay İDDK 23.01.2025 YD İtiraz 2024/629: Yönetmelik m.17/1 hakkında ikincil konsolidasyon kaydı; **tam karar aslı ve güncel safahat edinilmedi**.

Danıştay resmî karar arama ve dosya sorgu adresleri: https://karararama.danistay.gov.tr/ ; https://dosyasorgulama.danistay.gov.tr/ . Bu portalların varlığı, ilgili kararın asıl metnine erişildiği anlamına gelmez. **Normun olay tarihindeki yürürlük ve geçiş etkisi otomatik onaylanamaz.** Olay tarihli yürürlükleri tam/doğrulanmış TCK/CMK/Yönetmelik corpus'u yoktur.

## TUR009 Windows UAT ve CI köken dosyası

- `tools/TUR009-Windows-UAT.ps1` — TUR008'in 36 maddelik kabul listesine önceden hâkim kararı / kararsız elkoyma ve kaynak kökeni için 5 odaklı kontrol ekler; toplam 41 maddelik **manuel insan gözlem listesi**.
- Başlangıçta EXE SHA256 ve kaynak ZIP'in **bütün SHA256SUMS** listesi doğrulanır. `BUILD_PROVENANCE.json` eksikse/bozuksa veya kayıt ürün/repo/HEAD/NO-GO koşullarıyla tutarsızsa kabul başlatılmaz.
- Kullanıcı fiziksel Windows oturumunda bizzat gözlem yapacağını `EVET` ile onaylamazsa test çalıştırılmaz.
- Tüm 41 gözlem N=NOT_RUN varsayımıyla, yalnız gerçek kişinin P/F/N seçimi olarak kaydedilir; veri otomatik Drive/UYAP'a aktarılmaz. Hata notu UI/MATH/CRASH/ACCESS/OTHER'dır, serbest soruşturma metni tutulmaz.
- Yerel JSON gözlem raporu ve SHA256 makbuzu, **bağımsız fiziksel kabul imzası veya sahici insan testi kanıtı değildir**.
- CI'nin yazdığı `BUILD_PROVENANCE.json` SOURCE HEAD, checkout SHA, run ID, build UTC ve NO-GO durumlarını içerir. Manifest hash'i kendi içinde bozulmayı saptar; **kaynak imzalı değildir** ve bir saldırganın dosyaları/manifesti birlikte değiştirmesini önleyemez. Resmî mevzuat veya üçüncü taraf lisans sertifikası değildir.

## Güvenlik ve yayın kapıları

- Gerçek fiziksel Windows bilgisayarında GUI, DPI, klavye/odak, popup, şifreli kasa/backup, yazdırma: **NOT_RUN**.
- TCK/CMK/Yönetmelik tam güncel ve olay tarihli resmi kaynak byte/sayfa-madde/yürürlük/7571 geçiş: **NOT_VERIFIED**.
- UYAP Güneş Doğuş–Batış asıl tablo ve yeniden dağıtım hakkı: **NOT_ACQUIRED**.
- Eski plaintext notlar ve şifreli yeni kasa metadata gizlilik açıkları çözülmedi.
- Logo E-terazi yeniden dağıtım hakkı ve GitHub GPLv3 vs Master Prompt MIT lisans çelişkisi açık.
- İki tarih aracı, tek Gece Vakti, 3 Sistematiği, Ceza/Para 3 sekme, E-terazi/temalar korunmuştur.
- **Main merge, installer, tag veya release yapılmadı.**
