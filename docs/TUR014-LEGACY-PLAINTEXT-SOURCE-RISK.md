# TUR014 — Salt-okunur eski düz metin SQLite riski envanteri ve hukuki kaynak kanıtları

Tarih 21.11.2026, Turkey. TUR013 exact HEAD `8064947bae2a9ab6e493a979eae5b6d44f9666b8`, Windows CI 82/82 PASS.
**Fiziksel Windows NOT_RUN. Hukuki veri tamlığı/Stable release NO-GO.**

## Tespit
TUR005'in onaylı düz metin not göçü, veri kaybını önlemek amacıyla eski SQLite dosyasını ve yedeklerini silmiyor. Kullanıcı, göçten sonra `article-notes.sqlite3` ana kaynağının yanında kalan WAL/SHM/journal ve olası bilinen yedek dosyalarının şifrelenmemiş olabileceğini kolayca göremiyordu. TUR013 haricî HMAC makbuzu silinen satırların kontrolüne yardımcı olur; **SQLite madde/sürüm/id/tarih metadata açıklığını kapatmaz**.

## TUR014 kod
`LegacyPlaintextExposureAudit.ScanKnownLocalDirectory`, `ArticleNoteStore.DefaultDatabasePath` ile aynı **tek yerel klasörde** yalnız bilinen eski düz metin kaynak adları, eski DB yan dosyaları ve açıkça eski sayılan yedek adlarını sayar. Her adayın boyutunu ve ilk 16 baytındaki `SQLite format 3\0` başlık izini değerlendirir. Dosyanın içindeki not metnini, madde numarasını, tablo verisini, kullanıcı parolasını veya tam yolu UI/rapora yazmaz. İçeriği değiştirmez, SQLite'ı yazma modunda açmaz, silmez, izin değiştirmez; alt klasör/bağlantı takip etmez ve taramayı 2048 girdiyle sınırlar. Okunamayan girdiler ayrıca sayılır ve NOT_VERIFIED kalır. Bilinen adlar dışında kalan kopyalar veya haricî diskler taranmaz. **Aday sayısı sıfır olsa da bütün bilgisayar temiz kabul edilmez.** SQLite başlığının varlığı da kayıtların şifresiz olduğunu kesin kanıtlamaz.

Yeni native WPF düğmesi: **Şifresiz eski dosya riskini salt-okunur denetle**. Bilinen kaynak/yan dosya/olasılı yedek sayıları, toplam bayt ve incelenemeyen girdi sayısı ile kayıp/gizlilik uyarısı verir; hiçbir kişisel içerik/şifreyi istemez. Eski `LegacyNoteMigration` şifreleme, kullanıcı onayı ve geri alma yedeği algoritmaları değiştirilmez.

**Dört odaklı sentetik xUnit:** klasör yokluğu tam disk PASS olmasın; ana SQLite/yan dosya sınıflaması ve hiç plaintext metin yaymama, v4 şifreli dosyayı yanlış işaretlememe ve aday yedekleri fark etme, sahte SQLite başlıklı dosyayı yine risk sayıp sertifikalandırmama. SHA dosya içeriği tarama sonrasında aynı.

## Hukuki araştırma kanıtları ve eksik tamlık
**7571 sayılı Kanun:** 25.12.2025 tarihli ve 33118 sayılı **Resmî Gazete birincil HTML tam kanun metni** https://www.resmigazete.gov.tr/eskiler/2025/12/20251225-33.htm . Metnin m.22'siyle CMK **128/A** eklenmiş; bu maddedeki hesap askıya alma ile hâkim kararı olmadan elkoyma için özel 24/48 saat ve evrak mekanizması CMK 127/3 genel denetiminden ayrı tarihleme gerektirir. Aynı Kanun m.26, **CMK Geçici 8** hükümlerini getirir; bu hüküm uzlaştırma/önödeme geçişi bakımından suç/işlem tarihi ve 7571 yürürlük beyanlarının ayrıştırılmasını gerektirir. Bu tur **ilk defa doğrudan birincil kanun HTML kaynağına ulaştı**; ancak sayfa kaynak SHA-256, tüm madde/fıkra tam eşlemesi, güncel konsolide CMK karşılaştırması ve olay tarihi hukuki sonuç PASS değil. `SeizureAuthorizationGate` genel CMK127/3 uyarısı değişmedi; 128/A özel suç kapsamı otomatik uygulanamaz.
**AYM E.2023/128 K.2026/36:** AYM resmî karar arama kaydı https://normkararlarbilgibankasi.anayasa.gov.tr/kbb/pages/search/NormDenetimi?items_per_page=100&size=100 ; 25.05.2026 Resmî Gazete karar listesi. Kurumsal basın duyurusu CMK134 kısmi iptal ve dokuz ay ertelenen yürürlük hakkında açıklama. İmzalı tam hüküm, sonradan mevzuat değişikliği ve olay tarihli konsolide CMK134 **NOT_VERIFIED**.
**Danıştay** 13.03.2007 E.2005/6392 K.2007/948 yayımlı İstanbul Barosu yeniden üretim; İDDK 14.09.2012 E.2007/2257 K.2012/1117 HSK/Bakanlık dipnot aktarması; 23.01.2025 YD itiraz 2024/629 için ikincil safahat. Her üç mahkeme kaynaklı tam asıl ve olay tarihli iptal/YD etkisi **NOT_VERIFIED**.
**UYAP** güneş doğuş/batış asıl çizelge baytları ve yeniden dağıtım hakkı **NOT_ACQUIRED**. Resmî TCK/CMK/Yönetmelik tam tarihsel korpus tamlığı ve sayfa/madde/hash manifesti **NO-GO**.

## Güvenlik sınırı ve kullanıcı kabulü
Sadece bilinen dosya adlarını aynı klasörde bulur; yeniden adlandırılmış ve dışa taşınmış plaintext kopya kalabilir. Eski dosyaların güvenli silinmesi veya Windows ACL değiştirilmesi **bilinçli olarak yapılmadı**. Veri kaybını ve kanıt zincirini korumak için kullanıcı gerçek soruşturma içeriğiyle Alpha kullanmamalı. TUR013 haricî HMAC makbuzu korunur; güvenilir monotonic anti-rollback mekanizması değildir. 
58 eski + 4 yeni = **62 manuel Windows UAT**. Physical UAT NOT_RUN, CI yalnız Windows runner build, xUnit, statik WPF ve UAT betik sözdizimi.
Modüller: iki tarih ekranı, tek fail-closed Gece Vakti, üç Sistematiği, Adli Ceza/Para/YDO, E-terazi ICO/PNG, iki tema, gerçek WPF popup ve sentetik şifreli notlar **korundu**.
Dağıtım: repo GPLv3 ile istenen MIT uyuşmazlığı, E-terazi yeniden dağıtım izni, üçüncü taraf LICENSE/NOTICE açık. Main merge, tag, installer ve release **YOK**.
