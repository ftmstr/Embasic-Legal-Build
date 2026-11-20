# TUR011 — Şifreli kasa tüm not doğrulama ve veri kaybını önleyen yedek

## Hedeflenen kusur
TUR010 sürümünde `EncryptedArticleNotes.RestoreFrom()` yedeğin AES-GCM parola-sentinel kaydını ve SQLite quick_check sonucunu kontrol ediyor, ancak veritabanındaki **diğer notların şifreli içeriğini tek tek kimlik doğrulamadan** yedeğin hedef kasanın üzerine yazılmasına izin verebiliyordu. SQLite yapısını bozmadan yalnız bir `NoteText` alanını değiştirmek mümkün; bu durumda başka bir not silindikten/bozulduktan sonra geri yükleme işlemi veri kaybına yol açabilirdi.

## TUR011 uygulaması
- `ArticleNoteStore.InspectAllRows` yalnız aynı assembly'deki şifreleme katmanına açık, salt-okunur denetim API'sidir.
- `EncryptedArticleNotes.CheckIntegrity`: SQLite quick_check + tüm mevcut notların AES-GCM deşifresi (AAD madde/sürüm bağını da korur), salt ve parola-sentinel kayıtlarının ayrıca doğrulanması. Sadece mevcut satırları kontrol eder; **silinen veya daha önce geri alınmış kaydın eksiksizliğini kanıtlamaz**.
- `BackupTo`: mevcut kasa bütünü doğrulanmadan yedek alınmaz. Aynı hedef dizinde staging kopyası alınır, snapshot'taki bütün şifreli notlar doğrulanır, ardından son yedek dosyası değiştirilir. Hata durumunda **eski sağlam yedek korunur**.
- `RestoreFrom`: kaynak backup, hedef mevcut kasa ve geçici restore kopyasında bütün şifreli notlar doğrulanmadan `File.Replace` çalışmaz. Var olan salt/aidiyet/rollback ve kullanıcı açık onayı kuralı korunur.
- **3 yeni odaklı xUnit:** SQLite yapısı bozulmadan değiştirilmiş AES-GCM metniyle geri yükleme engeli; bozuk canlı kasada önceki backup'ın korunması; madde sürüm AAD saldırısı. Sahte hukuk/Windows sertifikası üretilmez.

## Kaynak/tür kontrolü
**AYM E.2023/128 K.2026/36** için Anayasa Mahkemesi Norm Denetimi Bilgi Bankası listesinde kayıt bulundu: https://normkararlarbilgibankasi.anayasa.gov.tr/kbb/pages/search/NormDenetimi?items_per_page=100&size=100. Resmî karar kaydı kaynak kimliğini destekler; kararın bütün baytları, Resmî Gazete metni hash'i, kanunun olay tarihli konsolide sürümü ve sonradan yapılan değişiklikler uygulamada **NOT_VERIFIED**.

Danıştay 2007/948 yayımlı karar yeniden üretimi, 2012/1117 onama ve 2025 YD 2024/629 kaynakları için mahkemece doğrulanmış tam asıl ve güncel safahat hâlâ **NOT_VERIFIED**. CMK127/3 24/48 saat, CMK134 tarihsel iptal ve Yönetmelik m.5–17 iptal/YD fail-closed korunur.

## Fiziksel Windows kullanıcı kabulü
`tools/TUR011-Windows-UAT.ps1` mevcut 46 gözlem + 3 veri bütünlüğü kontrolü = **49 manuel gözlem**. Tam kaynak HEAD, SHA manifest ve user report kontrolleri korunur. Hiçbir gerçek Windows PC gözlemi olmadığı için **UAT NOT_RUN**. Testlere yalnız sentetik not yazılabilir. Gerçek dosya/mağdur/şüpheli verileri kullanılmamalıdır.

## Bilinçli açık güvenlik engelleri
* SQLite kimlik/sürüm ve zaman damgası **şifresiz metadata**. Anahtar yönetimi, tam veritabanı şifrelemesi ve kalıcı silinme/rollback saldırısı güvenliği PASS değil.
* AAD not kimliğini bütünüyle veya tüm veritabanı kapsamını bir Merkle root/manifest altında bağlamaz: satır silinmesi, önceki backup'ın değiştirilmesi ve aynı kasaya ait eski bir sürümün replay edilmesi uygulama seviyesinde engellenmez.
* Eski plaintext SQLite, yedekler ve Windows klasör ACL güvenliği doğrulanmadı.
* GPLv3/MIT ve E-terazi dağıtım hakkı, resmi CMK/TCK/Yönetmelik tamlık, UYAP güneş verisi, fiziksel Windows UAT kapalı.
* Main merge, installer, tag, release YOK; **hukuki üretim NO-GO**.
