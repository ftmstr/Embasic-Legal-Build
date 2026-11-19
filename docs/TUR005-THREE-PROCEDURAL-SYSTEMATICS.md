# TUR005 — Tutuklama, Adli Kontrol, Arama ve El Koyma Sistematikleri

Tarih: 20.11.2026. Kullanıcının TUR005 geliştirmesine eklediği üç ayrı bölüm-içi denetim ekranı için kaynak/uygulama haritası.

## Arayüz

- Tutuklama başlığı içinde: Tutuklama Sistematiği düğmesi.
- Adli Kontrol başlığı içinde: Adli Kontrol Sistematiği düğmesi.
- Arama ve El Koyma başlığı içinde: Arama ve El Koyma Sistematiği düğmesi.

Buton ilgili başlığın kendi ekranında açılır/kapanır. İki görünüm bulunur: Öğretici Şema ve Dosya Üzerinde Denetim. Şema aşama → soru → aranacak delil/evrak → madde dayanağı çizgisini izler. Denetim her soru için İncelenmedi, Belge ile kontrol edildi, Eksik/çelişkili, Gerekçeli uygulanmaz durumlarından birini ve kimliksiz evrak referansını toplar. Kaynak belgesi/gerekçesi yazılmayan “doğrulandı/uygulanmaz” durumları kanıtsız sayılır. Metin yalnızca kullanıcı onayıyla panoya kopyalanır; otomatik UYAP, Google Drive, SQLite ya da ağ kaydı yapılmaz. Kapanışta geçici RAM verisi kaybı konusunda uyarı vardır. Bütün kontroller tam olsa bile hukuki uygunluk sertifikası verilmez; otomatik tutuklama/arama/el koyma kararı oluşturulmaz.

## Tutuklama sistematiği: 12 kontrol

CMK 100 somut delile dayalı kuvvetli suç şüphesi, tutuklama nedeni, ölçülülük ve yasakların dosyada kontrolü; CMK 101 istemin ve kararın somut gerekçesi, özellikle adli kontrolün niçin yetersiz kaldığı; müdafi ve savunma koşulları; CMK 102 azami süre ve özel durumlar; CMK 103–105 tahliye/alternatif; CMK 108 dönemsel inceleme. Katalog suç listesi tutuklamanın otomatik sonucu değildir.

## Adli kontrol sistematiği: 12 kontrol

CMK 109 koşulları ve somut yükümlülük türü; CMK 110 karar mercii ve değiştirme/yükümlülükten muafiyet; CMK 110/4 periyodik devam kontrolü; CMK 110/A sürenin başlangıcı, uzatma ve çocuk rejimi; CMK 111 başvuru/itiraz; CMK 112 ihlal iddiasında kasıt, tebligat ve mazeret. Hiçbir işaret otomatik tutuklama sonucuna dönüştürülmez.

## Arama ve El Koyma Sistematiği: 27 kontrol

| Kontrol noktası | CMK dayanağı | Adlî ve Önleme Aramaları Yönetmeliği | Dosya karşılığı |
| --- | --- | --- | --- |
| Adlî arama / önleme ayrımı | 116–117 | 5; önleme bölümü 18–27 değil | Delil/yakalama amacı, soruşturma konusu |
| Makul şüphe | 116–117 | 6 | Olayın kişi/yer/eşyaya bağlı emareleri |
| Karar merciinin yetkisi | 119/1 | 7 | Hâkim kararı, gecikmede savcı yazılı emri |
| Konut ve kapalı alan | 119/1 | 7 | Kolluk amirinin kapalı yer emri verememesi |
| Savcıya ulaşılamaması | 119/1 | 7 | Ulaşma denemeleri ve yazılı tutanak |
| Karar kapsamı | 119/2 | 7 | Fiil, aranan kişi/yer, geçerlilik süresi |
| Kararsız arama iddiası | 116–119 | 8 | İstisnanın somut dayanağı, iptal edilen ibareler |
| Gece araması | 118; TCK 6/1-e | 4 | Kapalı yerde gece yasağı ve istisnalar; UYAP resmî çizelgesi doğrulanmadan kesin saat yok |
| Olay yeri inceleme | 116–119 | 9 | Açık/kamuya açık yer ve özel alan ayrımı |
| Arama tanığı / hazır olanlar | 119/4, 120 | 7, 11 | Savcı yokken iki tanık, zilyet ve müdafi hakkı |
| İcra ve tutanak | 119/3, 121 | 11, 12 | İşlem zamanı, numara, imza, eşya listesi, belge teslimi |
| Kâğıt/belge | 122, 126 | 16 | İnceleme ve mühürleme yetkisi |
| Elkoyma | 123–127 | 10, 15 | Ayrı elkoyma hukuki dayanağı, envanter |
| Hâkim onayı | 127/3 | 10, 15 | 24 saat sunma, 48 saat karar, gerçek işlem saatlerinin belgeli kontrolü |
| Tesadüfî delil | 138/1 | 10 | Koruma, bildirim, yeni işlem |
| İade | 131–132 | 12, 15 | Koruma/iade gereği |
| Dijital materyal | 134 | 17 | CMK 134 özgü rejim; m.17 23.01.2025 yürütmeyi durdurma bilgisinin asıl kararla teyidi |
| Avukat bürosu | 130 | 13 | Mahkeme kararı, savcı/baro ve meslek sırrı |
| Askerî mahal | 119/5 | 14 | Yetkili makam ve katılım |
| Norm versiyonu | 116–138 | 5–17 | Olay tarihi, norm değişikliği, Danıştay iptal/YD, özel kanun |

**Önemli iptal/YD güvenlik kapısı:** Yönetmeliğin 8. maddesindeki belirli ibarelerin Danıştay tarafından iptal edilmiş olduğu; m.17/1 hakkında 23.01.2025 tarihli Danıştay İDDK yürütmeyi durdurma bilgisinin konsolide kaynakta yer aldığı görülmüştür. Bu bilgiler birincil Danıştay kararları, hüküm kapsamı ve olay tarihiyle ayrıca teyit edilmeden kesin uygulanabilir mevzuat diye gösterilmez.

### Kamuya açık başvuru kaynakları
- UYAP CMK: https://mevzuat.adalet.gov.tr/mevzuat/102946
- Bakanlık CMK PDF adayı: https://alternatifcozumler.adalet.gov.tr/Resimler/Mevzuat/CMK%205271.pdf
- Adalet Bakanlığı Yönetmelik PDF adayı: https://inhak.adalet.gov.tr/Resimler/Dokuman/12122019120126um_adliveonlemearamalari.pdf
- Değişiklik/iptal dipnotları bulunan konsolide metin: https://www.lexpera.com.tr/mevzuat/yonetmelikler/adli-ve-onleme-aramalari-yonetmeligi-1
- UYAP Güneş Doğuş-Batış giriş: https://bigm-uyap.adalet.gov.tr/portal/zaman/zaman.html

Bu kaynaklar incelenen açık web referanslarıdır. Resmî CMK/Yönetmelik tam güncel ham metin baytları uygulamanın onaylanmış offline hukuk korpusuna alınmış ve bütün değişiklikler tarihe göre kanıtlanmış değildir. Maddelerin programda gösterilmesi kanun metni doğruluğu, delil hukuka uygunluğu veya somut dosyada geçerliliği onaylamaz. HUKUKİ ÜRETİM NO-GO.
