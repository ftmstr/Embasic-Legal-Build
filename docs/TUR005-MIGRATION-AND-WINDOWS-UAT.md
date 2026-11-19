# TUR005 — Kullanıcı Onaylı Göç ve Gerçek Windows UAT

## Eski şifresiz notların korunarak aktarımı
- Eski ArticleNoteStore salt okunur açılır; 25 MiB dosya, 1000 not, 50 KB/not ve 12 MiB toplam UTF-8 metin üst sınırları, SQLite quick_check ve SHA-256 kontrolü vardır.
- Ön inceleme not metnini değil yalnız adet, boyut ve hash özetini gösterir.
- Kullanıcı şifreli kasayı açmadan, ön inceleme yapmadan, açık onay kutusunu işaretlemeden ve ayrıca Evet demeden aktarım gerçekleşmez.
- İşlemden önce kaynak SHA yeniden denetlenir; WAL yan dosyaları ve kaynak değişikliği reddedilir.
- Şifreli hedefte çakışan madde/sürüm kimliği varsa overwrite yoktur; kayıtlar tek SQLite transaction ile şifrelenmiş olarak içeri alınır.
- Hedefin şifreli rollback kopyası önceden yazılır. Orijinal not ID, tarihleri ve metni şifreli hedefte geri okunarak karşılaştırılır.
- Eski şifresiz veritabanı ve eski yedekler silinmez, taşınmaz ve şifrelendi diye sunulmaz. Tam disk şifrelemesi yoktur; kullanıcı güvenli saklama ve ayrı açık-onaylı silme konusunu ayrıca değerlendirmelidir.

## Fiziksel Windows kabulü
İnteraktif powershell aracı tools/TUR005-Windows-UAT.ps1; EXE_SHA256 manifestiyle gerçek EXE'yi doğrular, kullanıcıya her testte PASS/FAIL/NOT_RUN seçimi sunar. NOT_RUN varsayılandır, sahte başarı üretmez. JSON çıktı kullanıcının yerel AppData klasörüne gider.

Açık test başlıkları: Windows 11 offline açılış; E-terazi/logo; tema ve popup; DPI/klavye; tarih/Gece Vakti; şifreli kasa kilit/parola/yeniden açma; kaydedilmemiş taslağın koruması; eski not ön incelemesi ve iki aşamalı onay; çakışma ve geri alma; bozuk PDF adayı; üç yeni Sistematiği düğmesi; her şema; 51 maddelik dosya denetiminde kimliksiz evrak atfı; hiçbir koşulda otomatik hukuk uygunluğu sonucu oluşmaması.

**Fiziksel Windows kabulü bu turda NOT_RUN.** Otomatik Windows CI, gerçek kullanıcı kabulünün yerine geçmez.
