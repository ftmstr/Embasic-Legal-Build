# TUR007 — Adli Ceza ve Para Cezası WPF hesaplama eklemesi

**20 Kasım 2026 | Önceki TUR006 HEAD:** b6e9eb68e7f327d88243eb147a7b56169d554a57.

## Kaynak ve kapsam

Google Drive özgün `Embasic_Legal_v1_5.html` (ID 1FSJdP6M2r-6h7a2XxegXqRLA5KwORN_g) incelendi. Eski HTML içindeki `#sentence`, `#convert`, `#revalue` bölümleri sırasıyla ceza oranı, hapis/adlî para matematiği, manuel bileşik YDO ve eski TL dönüşümü içeriyor. Kullanıcının ekran görüntüsünde bunların üç navigasyon başlığı bulunuyor. TUR006 WPF CalculatorHub yalnız açıklama gösterdiğinden gerçek eksiklik doğrulandı.

## Geliştirme

Adli Hesaplama Merkezi altına doğrudan erişilebilir **Adli Ceza ve Para Cezası** ekranı; ceza oran, hapis/para ve YDO para dönüşümü olmak üzere 3 bağımsız sekme ve sol menü bağlantıları eklendi. C# net10.0 `PenaltyMoneyArithmetic` çekirdeği ile:
- **Oran**: pozitif tam gün, pay/payda, indirim/artırım, nihai tam gün sonucunda kesri bırakma; 100 gün için 1/6 indirim = 83, artırım = 116.
- **Para**: tam gün × manuel günlük TL, isteğe bağlı başka para bileşeni; TCK 61/6 tam TL bileşeni ayrıca görünür. Başka para eklenmesinin hukuki gerekçesi veya hapis cezasının TCK 50 uyarınca dönüşümü doğrulanmaz.
- **YDO**: kullanıcının seçip elle yazdığı yıllık oranlar `; ` ile ayrılıp çarpılır; kaynak metni sadece manuel açıklama olarak gösterilir. Adli para cezası tutarı YDO ile otomatik artırılmaz.
- **Eski TL**: 1.000.000 eski TL = 1 TL birim dönüşümü. Güncel satın alma gücü hesabı değildir.

Önceki HTML 1 yıl=12 ay, 1 ay=30 gün basitleştirmesini uyguluyordu. Yeni araç **yıl/ay cezasını otomatik sabit gün sayısına çevirmeyerek** TCK 61/6 yıl hesabı yanlışlığını önler. Kullanıcı tam gün miktarını kendisi teyit eder.

## Mevzuat ve fail-closed

Resmî Gazete 12 Mart 2024 tarih, 32487 sayılı nüsha 7499 sayılı Kanun **m.9**, TCK 52/2 günlük miktarını 20–100 TL'den 100–500 TL'ye çıkardı; **m.40/1-a** bu hükmün 01.06.2024 tarihinde yürürlüğe girdiğini belirtti:
https://www.resmigazete.gov.tr/eskiler/2024/03/20240312-1.htm

UYAP TCK madde 7, 50, 52, 61:
https://mevzuat.adalet.gov.tr/mevzuat/103228

Bu bilgiler yalnız tarihsel kanun değişikliği uyarısıdır. Her olayda TCK 7 lehe kanun, özel suç maddesi, alt/üst gün sınırı, mahkeme takdiri ve değişiklik zinciri ayrıca hukuken incelenmelidir. `HistoricalRateInfo.FavorableLawReviewed=false`. TCK 52'nin sonraki özel norm istisnaları otomatik uygulanmaz.

## Kanıt / sınırlar

Core xUnit: 8 odaklı vaka; .NET 10 WPF Windows CI ve paket UAT listesi. Gerçek fiziksel Windows kullanıcı kabulü **NOT_RUN**. Diğer iki tarih ekranı, tek Gece Vakti, E-terazi, üç sistematik butonu, popup/temalar korunur. Ana repo lisansı GPL-3.0 / Master Prompt MIT kararı ve logo dağıtım izni hâlâ açık. Tam resmî güncel/tarihsel hukuk korpusu onaylanmadı. Gizli soruşturma verisi Alpha'ya girilmez. Main merge, installer, tag, Stable release yok.
