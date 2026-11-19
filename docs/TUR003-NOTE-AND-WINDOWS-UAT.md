# TUR003 — Madde baloncuğu, sticker ve fiziksel Windows kabul planı

## Yapılanlar
Favoriler ve Notlar ekranında SENTETİK ÖRNEK olarak ayrılmış, gerçek mevzuat maddesi olmayan kart bulunur.
Bu karttaki WPF Popup başlangıçta kompakt açılır; Genişlet/Küçült düğmesi boyutu değiştirir. Çok satırlı not, 900 ms gecikmeli otomatik kayıt, Kaydet/Kapat/Sil seçenekleri ve yalnızca kaydedilmiş not için 📝 sticker bulunur.
Yerel SQLite kaydı her başarılı yazmadan sonra geçici dosyada bütünlük kontrolü yapılan atomik yedek almayı dener.
Silmeden önce eski kaydı yedekleme şartı vardır. Hatalı kayıt sırasında taslak açık tutulur ve uygulama kapatma engellenir.
Mevcut not kimliği LawNumber + ArticleKey + SubdivisionKey + VersionKey olarak korunur. Sentetik test kimliği SANDBOX / SYNTHETIC-ARTICLE / TUR003-EXPLICIT-DEMO'dur.
Gerçek TCK/CMK maddeleri, doğrulanmış kaynak madde indeksi olmadığından bu turda not düzenleyicisine bağlanmamıştır.

## Kritik uyarılar
Veritabanı ve yedek ŞİFRELİ DEĞİLDİR; gerçek soruşturma bilgileri veya kişisel veriler kesinlikle girilmemelidir.
SQLite yedeğinin geri yüklenmesi için kullanıcı arayüzü henüz yapılmamıştır. Artan yedek zinciri ve güvenlik denetimleri devam etmelidir.
Bağımsız fiziksel Windows kullanıcı kabulü NOT_RUN'dır.

## Kullanıcı kabul adımları
1. Windows 11 x64'te ZIP'teki EXE'yi aç, E-terazi logosunu ve açık/koyu temayı denetle.
2. Favoriler ve Notlar > Örnek madde > aç/kapat, küçük ve geniş baloncuğu dene.
3. SENTETİK not yaz, 900 ms sonra sticker beliriyor mu kontrol et, uygulamayı tekrar aç ve kalıcılığı denetle.
4. Sil komutundan önce yedek alma ve silme sonrası sticker yok oluşunu kontrol et.
5. Yanlış/erişilemeyen yerel disk senaryosunda kaydedildi uyarısı verilmediğini ve kaydedilmemiş taslağın korunduğunu sınayarak belgele.
6. Mevzuat listesindeki 5237/5271 PDF aday alımının yalnız BAYT BÜTÜNLÜĞÜ etiketi döndürdüğünü kontrol et.
7. Güneş/TCK resmî çizelgesi yokken saat sonucu üretilmemeli; tam olarak 2 tarih ekranı korunmalı.
8. DPI 100/150/200%, yüksek karşıtlık, ekran okuyucu, pencere DPI, veri kurtarma ve internetsiz kullanım gözlemlenmeli.

CI xUnit testlerinin başarılı olması fiziksel Windows GUI kabulü değildir.