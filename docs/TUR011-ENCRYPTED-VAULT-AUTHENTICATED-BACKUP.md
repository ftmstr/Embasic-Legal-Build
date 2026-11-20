# TUR011 — AES-GCM bütünlük ve atomik yedekleme kapısı

Bu turda SQLite quick_check sonucunun yalnız SQLite sayfa/B-tree bütünlüğünü kontrol ettiği, AES-GCM ile şifrelenmiş her bir notun kimlik doğrulamasını garanti etmediği tespit edildi. Yedek alınmadan ve geri yüklemede kasanın yerini almadan önce artık **salt ve parola doğrulama sentinel'i dahil tüm mevcut not kayıtları** kimlik doğrulamasından geçiriliyor. Sürüm bilgisine bağlı AAD değiştirildiğinde veya şifreli metin değiştirilmişse ret. SQLite yapısı bozulmasa bile işlem reddediliyor.

`EncryptedArticleNotes.CheckIntegrity`: SQLite quick_check + var olan kayıtların AES-GCM çözümü + metadata sayım/doğrulama. Silinmiş notu tespit edemez; metadata alanları şifreli değildir; bu bir eksiksizlik veya gizlilik sertifikası değildir.

`EncryptedArticleNotes.BackupTo`: canlı kasa autentikasyonu, sonra aynı dizinde staging SQLite snapshot, snapshot'ın yeniden autentikasyonu ve **ancak ondan sonra** hedef yedek dosyasının atomik taşınarak değiştirilmesi. Doğrulama hata verirse eski yedek olduğu gibi kalır. Parola disk/yedek içine aktarılmaz.

`EncryptedArticleNotes.RestoreFrom`: kaynaktaki tüm şifreli notlar ve parola doğrulanmadan eski kasa değiştirilemez. Mevcut kasa da kontrol edilir; staging kopyası da bütün AES-GCM kayıtlarını kontrol eder. Orijinal kasa ve geri alma yedeği saldırı/bozulma durumunda korunur.

**Kanıt sınırı:** kötü niyetli dosya silinmesi, eski bir yedeğin yeniden oynatılması, dosya ACL'leri/Windows gerçek PC UAT ve note identity/timestamp plaintext metadata gizliliği bu düzeltmenin kapsamında değildir. Şifreli not anahtarı PBKDF2-SHA256 310.000 iterasyon + AES-256-GCM, gerçek hak/lisans veya legal-data doğrulaması yerine geçmez.

**Yargısal üretim:** CMK127/3 ön hâkim kararı ve 24/48 referansı, CMK134 AYM E.2023/128 K.2026/36 ileri yürürlük uyarısı, Danıştay karar asılları, UYAP resmî güneş verileri ve olay tarihli TCK/CMK/Yönetmelik korpusu için insan/kurumsal kanıt kapıları KAPALI. Fiziksel Windows NOT_RUN, release NO-GO.
