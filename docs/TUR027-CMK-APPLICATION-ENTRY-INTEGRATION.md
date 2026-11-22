# TUR027 — CMK öğrenme yapısının uygulama girişlerine bağlanması

## Somut eksik ve değişiklik

TUR026/PR #27, CMK öğrenme ekranını sol menüye ekliyordu. Ancak uygulama ana sayfası hâlâ ilk turu anlatıyor ve öğrenme merkezine giriş sunmuyordu. Mevcut `Alternatif Çözümler` menüsü ise gerçek dersler bulunmasına rağmen `PlaceholderViewModel` açıyordu.

TUR027 aynı WPF uygulamasında ana sayfaya güncel katalog sayılarından beslenen CMK kartını, merkezi açma düğmesini ve Soruşturma/Kovuşturma/Kanun Yolları/Alternatif Çözümler kısayollarını ekler. Mevcut Alternatif Çözümler menüsü doğrudan `cmk250-253` karşılaştırma dersini açar; ayrıntılı seri muhakeme, basit yargılama, uzlaştırma ve bağlantılı TCK 75 dersleri aynı kategoride seçilebilir.

`CmkLearningHubViewModel.OpenTopic` ilgili kategori ve konuyu birlikte seçer. Bütün yollar MainViewModel içindeki mevcut tek `_learningHub` nesnesine gider; RAM işaretleri ve test puanları ana sayfa veya diğer menülerden dönüşte korunur. 13 kategori, 51 konu, 102 soru ve önceki ders içeriği değişmez. Tutuklama, Adli Kontrol ve Arama/El Koyma işlem sistematikleri korunur. README geliştirme dalını ve uygulamadaki erişim yolunu açıklar.

## Doğrulama sınırı

`tools/Embasic.Legal.NavigationSmoke` mevcut Desktop projesini referans alan, yeni NuGet bağımlılığı olmayan küçük Windows STA yürütücüsüdür. Gerçek `MainViewModel.NavigateCommand` üzerinden açılış sayfasını, 13 kategoriyi, mevcut Alternatif Çözümler menüsünü, aynı öğrenme nesnesinin ve işaretlerinin korunmasını, 102 soru paydasını ve önceki üç sistematik ekranını denetler. Kasa açmaz; disk/ağ veya gerçek dosya verisi kullanmaz. Bu bir fare/klavye veya fiziksel Windows GUI kabulü değildir. Başarılı çalışırsa 23 kontrol çıktısı beklenir.

Mevcut 171 xUnit testi ayrıca Windows CI'da çalıştırılır. `TUR027-Windows-UAT.ps1` ana sayfa ve mevcut menü bağlantıları için dört yeni manuel vaka içerir. Fiziksel UAT **NOT_RUN**. Güncel mevzuat kaynağı ve yürürlük **NOT_VERIFIED**; CI bu durumu değiştirmez. Önceki production/stable **NO-GO** korunur. `main` merge, tag, release ve installer yoktur.
