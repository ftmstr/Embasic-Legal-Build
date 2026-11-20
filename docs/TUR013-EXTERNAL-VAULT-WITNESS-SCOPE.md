# TUR013 — Şifreli kasa haricî bütünlük makbuzu ve açık güven sınırı

Tarih: 21 Kasım 2026. Ürün: **Windows .NET 10/WPF Alpha**; gerçek hukuk üretimi ve dağıtım **NO-GO**.

## Problem ve kanıta dayalı tasarım
TUR012; AES-GCM yanlış şifreli notları, şifresiz değişiklikleri ve geri yükleme ön incelemesinde eski/eksik not riskini kontrol ediyor. Ancak dosyadaki bir not tamamen silinirse diğer notların AES-GCM etiketleri hâlâ geçerli olabilir. Ayrıca kasanın tamamı eski ve kriptografik açıdan geçerli bir dosyayla yer değiştirirse, daha yeni durumun varlığı yalnız SQLite/AES-GCM üzerinden anlaşılamaz.

TUR013 **isteğe bağlı** `EncryptedArticleNotes.ExportIntegrityWitness(path)` ve `VerifyIntegrityWitness(path)` ekler. Şifreli kasa açılıp tam içerik doğrulandıktan sonra SHA-256-HMAC alt anahtarı (mevcut kasa şifreleme anahtarından alan ayrıştırılarak) oluşturulur. Sıralı tüm SQLite satırlarının kanun/madde/sürüm AAD kimlikleri, not ID, şifreli not metni, oluşturma/güncelleme zamanları ve sistem salt/sentinel kayıtları **HMAC girdisi** olur. Haricî JSON yalnız format sürümü, kullanıcı not adedi, HMAC ve kullanıcı kontrollü zaman beyanını içerir; hiçbir düz metin not, madde kimliği veya parola içermez.

Kullanıcı bu JSON dosyasını **şifreli veritabanı, otomatik yedek ve geri alma yedeğinden bağımsız** bir yere kaydeder. Sonraki kayıt veya silme işlemleri JSON'u otomatik yenilemez. Yedekte kalan eski bir veritabanı ya da sessizce silinmiş bir satır, eski bağımsız makbuzla karşılaştırılırsa HMAC uyuşmazlığı saptanabilir. Aynı kasanın doğru parolasını bilen saldırgan, hem eski veritabanını hem eski makbuzu birlikte koyarsa veya kullanıcının haricî makbuzunu silebilirse bu yöntem saldırıya karşı güvenilir değildir. **Bu, işletim sistemi güven köküne bağlı monotonic anti-rollback sayacı veya dijital imza değildir**.

Makbuz kayıtlarının tarih alanları güvenilir sunucu zamanı değildir. V4 eski kasalar ve yedekler yeni makbuz olmadan çalışmaya devam eder; makbuz bulunmaması **doğrulandı** anlamına gelmez. Yedekleme/göç/restore şifreleme şemasına dokunulmaz.

## Arayüz ve testler
Sentetik Madde Notu ekranında iki native WPF düğmesi: **Bağımsız bütünlük makbuzu oluştur** ve **Önceki bütünlük makbuzunu doğrula**. İlkinde ayrıca saklama uyarısı; ikincisinde uyuşmazlık halinde hiçbir veri değiştirmeme ve kullanıcı incelemesi yönlendirmesi.

4 odaklı xUnit: yalnız içerik dışa çıkmadan makbuz ve yeniden açılış; SQLite/AES-GCM sağlamken not satırı silinmesi tespiti; geçerli eski tam SQLite'ya geri dönme tespiti; zaman damgası veya JSON HMAC/metadata değişimi tespiti. **Windows GUI fiziksel kabulü hâlâ NOT_RUN**. TUR013 UAT listesi mevcut 54 + 4 = **58 manuel gözlem**; otomatik PASS yok.

## Hukuk kaynakları ve devam eden NO-GO
AYM resmî Norm Denetimi Bilgi Bankası E.2023/128, K.2026/36 kaydı yayımlı: https://normkararlarbilgibankasi.anayasa.gov.tr/kbb/pages/search/NormDenetimi?items_per_page=100&size=100 ; Resmî Gazete 25.05.2026/33264. Bu, **hüküm fıkralarının ham tam metin baytlarının, sonraki yasa sürümlerinin ve CMK 134 olay tarihi uygulanabilirliğinin** doğrulanmış tamlık PASS olduğu anlamına gelmez. Danıştay 2007/948 İstanbul Barosu yayımı, İDDK 2012/1117 ve 2025 YD 2024/629 konusunda mahkeme kaynaklı tam asıl, sonraki safahat ve olay tarihli geçerlilik NOT_VERIFIED.

CMK 127/3 için hâkim kararı var/yok/belirsiz ve 24/48 saat kaynak–evrak kontrolleri matematiksel/öğretici; otomatik hukuki karar yok. TCK/CMK/Yönetmelik güncel/geçmiş tam korpus, 7571 geçiş kanıtları, UYAP resmî güneş çizelgesi NOT_ACQUIRED/NOT_VERIFIED.

**Kalan riskler:** SQLite law/article/version, not kimliği ve zaman damgaları açık metadata; eski plaintext SQLite yedekleri otomatik silinmez; aynı anda kasa+makbuz rollback, silinmiş tüm dosyalar, Windows ACL ve kaynak hakları garanti edilmez. E-terazi yeniden dağıtım hakkı ve repo GPLv3/Master Prompt MIT uyuşmazlığı açık. İki tarih aracı, tek Gece Vakti, üç Sistematiği, Adli Ceza/Para/YDO, E-terazi ICO/PNG, popup ve iki tema korunur.

**Üretim: NO-GO.** Main merge, tag, installer, release YOK.
