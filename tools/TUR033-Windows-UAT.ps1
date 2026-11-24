# Embasic Legal TUR033 — manual, offline, non-sensitive Windows UAT evidence collector.
# No UI tests are simulated and no results are marked PASS automatically.
[CmdletBinding()]
param(
    [string]$ExePath = (Join-Path $PSScriptRoot 'Embasic.Legal.exe'),
    [string]$ResumeFrom = '',
    [ValidateRange(1,158)] [int]$BatchSize = 10
)
$ErrorActionPreference = 'Stop'
if (-not $IsWindows -and $env:OS -ne 'Windows_NT') {
    throw 'Only a physical/real Windows session can complete this UAT.'
}
if (-not (Test-Path -LiteralPath $ExePath -PathType Leaf)) {
    throw "Windows EXE not found: $ExePath"
}
$exe = (Resolve-Path -LiteralPath $ExePath).Path
$actualSha = (Get-FileHash -Algorithm SHA256 -LiteralPath $exe).Hash.ToLowerInvariant()
$manifest = Join-Path (Split-Path -Parent $exe) 'EXE_SHA256.txt'
if (-not (Test-Path -LiteralPath $manifest)) { throw 'EXE_SHA256.txt missing.' }
$expectedMatch = [regex]::Match((Get-Content -LiteralPath $manifest -Raw), '(?i)\b[a-f0-9]{64}\b')
if (-not $expectedMatch.Success -or $actualSha -ne $expectedMatch.Value.ToLowerInvariant()) {
    throw 'EXE SHA-256 mismatch. UAT blocked.'
}
# Verify every original CI-packaged member before manual observation.
$allSums = Join-Path (Split-Path -Parent $exe) 'SHA256SUMS.txt'
if (-not (Test-Path -LiteralPath $allSums -PathType Leaf)) { throw 'SHA256SUMS.txt not found; UAT blocked.' }
$verified = 0
foreach ($line in (Get-Content -LiteralPath $allSums)) {
    if ($line -notmatch '^\s*([a-fA-F0-9]{64})\s{2}(.+?)\s*$') { throw 'Invalid SHA manifest line.' }
    $expected = $Matches[1].ToLowerInvariant()
    $filename = $Matches[2]
    if ([IO.Path]::GetFileName($filename) -ne $filename) { throw 'Unsafe manifest path.' }
    $asset = Join-Path (Split-Path -Parent $exe) $filename
    if (-not (Test-Path -LiteralPath $asset -PathType Leaf)) { throw "Missing file: $filename" }
    if ((Get-FileHash -Algorithm SHA256 -LiteralPath $asset).Hash.ToLowerInvariant() -ne $expected) {
        throw "Packaged file hash mismatch: $filename"
    }
    $verified++
}
if ($verified -lt 7) { throw 'Incomplete hash manifest; UAT not authorized.' }

$provPath = Join-Path (Split-Path -Parent $exe) 'BUILD_PROVENANCE.json'
if (-not (Test-Path -LiteralPath $provPath -PathType Leaf)) {
    throw 'Missing BUILD_PROVENANCE.json. TUR033 UAT blocked.'
}
$prov = Get-Content -LiteralPath $provPath -Raw | ConvertFrom-Json
if ($prov.product -ne 'Embasic Legal Windows TUR033 Alpha' -or
    $prov.repository -ne 'ftmstr/Embasic-Legal-Build' -or
    $prov.git_head_sha -notmatch '^[a-f0-9]{40}$' -or
    $prov.legal_production -ne 'NO-GO' -or
    $prov.physical_windows_uat -ne 'NOT_RUN' -or
    $prov.official_legal_corpus -ne 'NOT_VERIFIED') {
    throw 'Invalid package provenance; UAT blocked.'
}
Write-Host "GitHub build claims HEAD $($prov.git_head_sha); this is unsigned CI metadata."

# Verify the user supplied an independently sourced exact HEAD, not merely
# echoing the unsigned JSON from inside this archive.
Write-Host 'GitHub bağlantısını ayrı kontrol edin: https://github.com/ftmstr/Embasic-Legal-Build/pulls'
Write-Host 'Aşağıdaki SHA değerini paket içindeki BUILD_PROVENANCE.json dosyasından kopyalamayın.'
$independentHead = (Read-Host 'GitHub TUR033 Draft PR sayfasından bağımsız gördüğünüz exact 40-hane HEAD SHA').Trim().ToLowerInvariant()
if ($independentHead -notmatch '^[a-f0-9]{40}$' -or $independentHead -ne $prov.git_head_sha) {
    throw 'GitHub exact HEAD ile paket iddiası uyuşmuyor; kullanıcı kabulü bloke.'
}
Write-Host 'USER-CHECKED GITHUB HEAD MATCH (self-reported, not an authenticated digital signature).'


Write-Host "EXE SHA256 verified: $actualSha"
Write-Host 'No automated GUI/PDF acceptance is claimed; respond only from real observations.'
Write-Host 'Do NOT input actual investigation notes, personal data, or passwords in this script.'

$approval = (Read-Host 'Gerçek Windows oturumunda bizzat gözlem yapacaksanız EVET yazın').Trim().ToUpperInvariant()
if ($approval -ne 'EVET') {
    Write-Host 'UAT başlatılmadı; otomatik PASS oluşturulmadı.'
    return
}

$cases = @(
    'TUR033: Mevcut menu/deep-link Tutuklama açılışında İşlem sistematiği sekmesi (index 7) seçilir; 6 Kaynak ve güvenlik sekmesi açılmaz',
    'TUR033: Adli Kontrol ve Arama ve El Koyma doğrudan girişleri kendilerine ait aynı sistematik sekmesini açar; dönüp ilgili CMK dersleri ve tekrar listesi korunur',
    'TUR033: Ayrı pencere ile açılmış sistematikte kullanıcı Tab, Enter, Narrator ve açık/koyu temayı fiziksel Windows 11 cihazında kontrol eder; ekran görüntüsü/gerçek gözlem olmadan PASS yok',
    'TUR033: Win11 gerçek 900-DIP minimum pencere ve 125%/150% ekran ölçeğinde CMK 74 Tablo sekmesindeki dört alan dikey kartta kırpılmadan, yatay sürükleme olmadan okunur',
    'TUR033: CMK 134 ve başka merkezî derste her kart ölçüt/birinci/ikinci/neden alanlarını tam verir; Geniş dört sütunlu tablo Expander açılınca aynı veriler orijinal tabloda görünür ve yatay kaydırılabilir',
    'TUR033: Alternatif Çözümler Tablo sekmesinde legacy Matrix kartları açılır; ayrı üç ders, açık/koyu tema, klavye Tab/Enter, ekran okuyucu odak adları ve ana işleme dönüş çalışır',
    'TUR033: Ceza Muhakemesi Hukuku 5 · Tekrar sekmesi başlangıçta yanlışlıkla başarısız 47 konu listesi üretmez',
    'TUR033: CMK 74 quiz ilk yanlış cevaptan sonra tekrar listesinde görünür; sonraki doğru yanıt ilk deneme sayısını değiştirmez',
    'TUR033: CMK 134 hatırlama kartında Tekrar çalışacağım seçilince CMK 134 listede belirir, Hatırladım seçilince kart tekrar niteliği kaybolur',
    'TUR033: İki konunun tekrar satırları Derse dön ve tekrar çalış ile kendi konusunu açar, 47 konu ve ayrı Alternatif Çözümler korunur',
    'TUR033: Oturumu sıfırla tüm konu/kart/ilk hata tekrar listesini temizler; NOT_VERIFIED ve fiziksel UAT durumu değişmez',
    'TUR033: CMK 74 konusunda 2 kontrol işaretle, CMK 75-77 konusuna geç, sonra dön; 2 işaret ve sayaç korunur',
    'TUR033: 47 merkez konusunun her birinde öğrenme hedefleri, madde okuma rehberi ve üç hatırlama kartı görünür',
    'TUR033: Hatırlama kartı cevabı ilk başta gizlidir; açma ve Hatırladım/Tekrar özdeğerlendirmesi çalışır, konu dönüşünde korunur',
    'TUR033: Şema adımı seçildiğinde açıklama ve vurgu değişir; Sonraki adım dört adım arasında ilerler',
    'TUR033: Senaryoda yol seçimi vurgulanır ve o seçeneğe özgü açıklama gösterilir; başka konuya gidip dönünce seçim korunur',
    'TUR033: Karşılaştırma tablosu iki kavramı üç ortak ölçüt üzerinden karşılaştırır; uzun hücreler kırpılmaz',
    'TUR033: Uygulama testinde yanlış yanıt sonrası doğru yanıt denenebilir; ilk deneme puanı artmaz, sonradan doğru sayısı ayrı görünür',
    'TUR033: Oturumu Sıfırla kartları, dal seçimlerini, eğitim puanını ve kontrol işaretlerini temizler; işlem sistematiği verileri kendi mevcut sıfırlamasıyla yönetilir',
    'TUR033: Ceza Muhakemesi Hukuku tek açılır üst menüdür; 13 kategori bunun altındadır; eski CMK Öğrenme Merkezi adı görünmez',
    'TUR033: Tutuklama, Adli Kontrol, Arama ve El Koyma ayrı üst menü değildir; ilgili konunun İşlem sistematiği sekmesinde çalışır',
    'TUR033: Alternatif Çözümler ayrı üst menüde yalnız Seri Muhakeme, Uzlaştırma ve Ön Ödeme içerir; Basit yargılama merkezde kalır',
    'TUR033: Birleşik CMK250/253 karşılaştırma başlığı görünmez; konular yalnız ayrıntılı derslerde birer kez listelenir',
    'TUR033: Bir sistematikteki kimliksiz işaret ve belge referansı konular arasında korunur; uygulama kapatılırken mevcut uyarı sürer',
    'TUR033: Ana sayfada Ceza Muhakemesi Hukuku — Aç ve güncel 13/47/94 özeti görünür; tıklama gerçek öğrenme ekranını açar',
    'TUR033: Ana sayfadaki Soruşturma, Kovuşturma ve Kanun Yolları bağlantıları doğru kategoriyi açar',
    'TUR033: Mevcut Alternatif Çözümler menüsü boş hazırlık ekranı yerine Seri Muhakeme dersini açar',
    'TUR033: Ana sayfaya dönme ve Alternatif Çözümleri yeniden açma öğrenme işaretlerini korur',
    'TUR033: Sol menüde Ceza Muhakemesi Hukuku ve 13 kategori doğrudan açılıyor',
    'TUR033: Her kategori seçildiğinde o kategorinin ilk veya korunmuş konusu gösteriliyor; kategori ile konu uyumlu',
    'TUR033: Merkez 47 konu/94 soru, ayrı Alternatif Çözümler 3 konu/6 soru gösteriyor; küresel payda 100 kalıyor',
    'TUR033: Anlatım sekmesinde kavram, birbirine karıştırma ve örnek metinleri seçili konuya göre değişiyor',
    'TUR033: Şema adımları fare ve klavyeyle seçiliyor; uzun metin yatay kırpılmıyor',
    'TUR033: İtiraz, istinaf, temyiz ve yenileme dersleri ayrı açılıyor; yeni konu testinde doğru ve yanlış geribildirimi çalışıyor',
    'TUR033: Yeni soruşturma ve kanun yolları konuları arasında gidip dönünce işaretler korunuyor; tüm oturum sıfırlama bunları siliyor',
    'TUR033: Kaynak sekmesi kullanıcının 1.5.5271.doc adresini ve doğrulanamama durumunu gösteriyor; CMK bağlantısını kopyala DOC adresini kopyalıyor',
    'TUR033: 100/150/200 DPI ve minimum pencere boyutunda sol menü, soru seçenekleri ve anlatım kaydırılarak okunabiliyor',
    'TUR033: CMK 74 ile CMK 134 kontrol listesi birbirinden bağımsızdır; işaret kaldırma diğer konuyu değiştirmez',
    'TUR033: Eğitim oturumunu sıfırla bütün 50 konunun işaretlerini ve eğitim puanlarını temizler; NOT_VERIFIED sürer',
    'TUR033: Ceza Muhakemesi Hukuku sol menüden açılır; Adlî incelemeler kategorisinde eski CMK 74 konusu seçilebilir',
    'TUR033: Eski 13 konu CMK74, muayene, genetik, adli kontrol, tutuklama, arama ve alternatif çözümleri kapsar',
    'TUR033: 4 aşamalı akış her konuda doğru seçimle değişir; hiçbir madde güncel VERIFIED olmaz',
    'TUR033: Merkez karşılaştırmalı tablosu her konuda 3 ölçüt gösterir; ayrı alternatif bölümünün eski kontrol matrisi 4 satırdır; resmî kanun lafzı diye sunulmaz',
    'TUR033: RAM kontrol listesi işaretlenir ve sayacı günceller; dosya verisi kaydı yoktur',
    'TUR033: Senaryo testinde seçim yokken puan artmaz; yanlış cevap ayrı bildirilir',
    'TUR033: Doğru eğitim cevabı dahi hukuki uygunluk PASS veya işlem kararı oluşturmaz',
    'TUR033: Sonraki soruya geçme, konu değiştirme ve oturum sıfırlama işlevleri çalışır',
    'TUR033: Resmi kaynak bağlantısını kopyalama yalnız mevzuat.gov.tr içerir; web otomatik açılmaz',
    'TUR033: Eski 2 tarih, tek gece, 3 sistematik, para/YDO, ikon, tema ve AES-GCM not korunur',
    'TUR033: Mevcut kasa restore öncesinde rollback.pre-restore.sqlite3-wal kalıntısı varsa hedef SHA korunur',
    'TUR033: Geri alma dosyasının -shm kalıntısı mevcutsa işlem durur ve kalıntı korunur',
    'TUR033: Geri alma dosyasının -journal kalıntısı mevcutsa işlem durur ve kalıntı korunur',
    'TUR033: Geri alma dosyasının adı bir DİZİN tarafından işgal edilmişse File.Exists false olsa da restore reddedilir',
    'TUR033: Yan dosyasız mevcut kasa restore onayı geri alma dosyasını üretir, eski notlar ayrı açılır; TOCTOU PASS değildir',
    'TUR033: Normal kasa Unlock -wal adlı mevcut dosyayı salt-okunur korur ve açılışı reddeder',
    'TUR033: Normal kasa Unlock -shm adlı mevcut dosyayı korur ve açılışı reddeder',
    'TUR033: Normal kasa Unlock -journal adlı mevcut dosyayı korur ve açılışı reddeder',
    'TUR033: Normal kasa Unlock -shm adlı mevcut DİZİN varsa yedeksiz/düzeltmesiz reddeder',
    'TUR033: Yeni kasa yokken -journal kalıntısı varsa yeni dosya oluşturulmaz',
    'TUR033: Yan dosyası olmayan sentetik kasada oluşturma/açma/kaydetme işlevi çalışır; TOCTOU PASS değildir',
    'TUR033: Yabancı SQLite dosyasına normal kasa açılışı denenince dosya SHA ve şema değişmez',
    'TUR033: Eski şifresiz SQLite not dosyası EAL4 kasa sanılmaz; kaynak ve not baytları değişmez',
    'TUR033: Mevcut şifreli kasada yanlış parola sonrası kaynak SHA değişmez; doğru parola ile açılır',
    'TUR033: Gerçek geçerli kasa salt-okunur ön kimlik doğrulamadan sonra not kaydedebilir',
    'TUR033: Yeni kasa oluşturma ve bozuk şifreli notta yedek reddi davranışları korunur',
    'TUR033: Salt-okunur ön kimlik doğrulama NTFS file-lock / TOCTOU / junction PASS olarak sunulmaz',
    'TUR033: Şifreli yedek hedefi -wal adına DİZİN içeriyorsa yedek korunur ve işlem reddedilir',
    'TUR033: Şifreli yedek hedefi -shm/-journal adlı DİZİN içeriyorsa fail-closed ret uygulanır',
    'TUR033: Restore hedefinin -wal adlı DİZİN içermesi ön inceleme ve geri yüklemeyi durdurur',
    'TUR033: Legacy şifresiz SQLite -shm adlı DİZİN içermesi göçü durdurur ve kaynağı korur',
    'TUR033: Reparse, junction ve TOCTOU için fiziksel Windows kanıtı yoksa PASS yoktur',
    'TUR033: Oturumun ilk otomatik ham SQLite yedeğinde üstveri açıklaması EVET/HAYIR onayı gelir',
    'TUR033: HAYIR halinde not şifreli kaydedilir ve otomatik ham SQLite kopyası oluşmaz',
    'TUR033: HAYIR ardından kullanıcı notu silmek isterse yedeksiz silme engellenir',
    'TUR033: EVET yalnız açık kasa oturumu içindir; kilitlemede/uygulamadan çıkışta sıfırlanır',
    'TUR033: Manuel export ayrı üstveri uyarısını korur; yeni kopyanın metadata gizliliği PASS iddiası yok',
    'TUR033: Şifreli SQLite yedek hedefinde -journal varsa yedek aynı kalır ve işlem durur',
    'TUR033: Şifreli SQLite yedek hedefinde -wal/-shm varsa işlem durur ve yan dosya silinmez',
    'TUR033: Eski düz metin SQLite yedek hedefinde -journal/-wal/-shm varsa yedek üzerine yazılmaz',
    'TUR033: Yan dosyasız temiz hedefe şifreli yedek yeniden alınabilir ve AES-GCM notu açılır',
    'TUR033: Yedek hedefinin yan dosyaları/snapshot durumunda otomatik checkpoint, silme veya güvenli disk temizliği iddiası yoktur',
    'TUR033: Eski düz metin SQLite kaynağının -journal yan dosyası varsa ön inceleme durur',
    'TUR033: Eski düz metin SQLite kaynağının -wal yan dosyası varsa ön inceleme durur',
    'TUR033: Eski düz metin SQLite kaynağının -shm yan dosyası varsa ön inceleme durur',
    'TUR033: Ön incelemeden sonra ortaya çıkan -journal/-wal göçü durdurur ve mevcut notlara dokunmaz',
    'TUR033: Yan dosyalar otomatik silinmez ve kanıtsız temizleme/normalleştirme önerilmez',
    'TUR033: Yabancı SQLite yedek ön incelemesinde tablo/indeks eklenmez ve kaynak SHA değişmez',
    'TUR033: Yabancı SQLite hedef ön incelemesinde tablo/indeks eklenmez ve hedef SHA değişmez',
    'TUR033: Geçerli şifreli yedek ön incelemesinden önce/sonra her iki SQLite SHA aynı kalır',
    'TUR033: Ön inceleme bulunmayan yedeği veya klasörü otomatik oluşturmaz',
    'TUR033: Açık kullanıcı onaylı geri yükleme eski geçerli kasayı korur ve yanlış parola reddedilir',
    'TUR033: HMAC makbuzunun kasa ile aynı klasöre yazılması reddedilir; kasa değişmez',
    'TUR033: Farklı klasör tek başına bağımsız güven kökü veya anti-rollback PASS sayılmaz',
    'TUR033: Şifreli SQLite yedeğinin açık kanun/madde/sürüm metadata riski için EVET/HAYIR onayı istenir',
    'TUR033: Metadata uyarısında HAYIR seçilince yeni yedek dosyası oluşturulmaz',
    'TUR033: Geri yükleme öncesi WAL/SHM/journal yan dosyası varsa geri yükleme yapılmaz',
    'TUR033: Bilinen eski düz metin SQLite ve WAL/SHM/journal dosyaları yalnız sayılarla listelenir',
    'TUR033: Dosya içeriği ve gerçek soruşturma bilgisi rapora veya panoya aktarılmaz',
    'TUR033: Şifreli V4 kasa dosyası yanlışlıkla eski şifresiz kaynak olarak sayılmaz',
    'TUR033: Eski dosyalar silinmez/ACL değişmez; dışa aktarılmış farklı klasör yedeklerine güvenlik PASS yok',

    'TUR033: Sentetik iki notla ayrı JSON bütünlük makbuzu oluşturulur; metin/madde kimliği dosyada görünmez',
    'TUR033: Haricî saklanan makbuz aynı kasa içeriğiyle eşleşir; gerçek hukuk PASS diye gösterilmez',
    'TUR033: Not silinip/düzenlenince eski makbuz uyuşmazlığı gösterir; otomatik veri silinmez',
    'TUR033: Eski geçerli kasaya dönüşte bağımsız yeni makbuz eşleşmez; birlikte değiştirilen dosyalara karşı tam anti-rollback iddiası yok',

    'TUR033: Eski yedek yeni sürüm notu geri alıyorsa WPF veri kaybı sayısı gösterir',
    'TUR033: HAYIR seçilirse geri yükleme yapılmaz ve hedef kasa korunur',
    'TUR033: Açık EVET onayından önce yedek ve hedef SHA-256 yeniden doğrulanır',
    'TUR033: Yeni not yedekte yoksa kaybolma riski açıkça bildirilir',
    'TUR033: Eski düz metin not dosyası ve açık metadata otomatik silinmez',

    'TUR033: Yedek AES-GCM doğrulaması, yalnız SQLite quick_check PASS ile karıştırılmıyor',
    'TUR033: Bozulmuş not içeriği olan yedek geri yükleme reddediliyor; hedef kasa mevcut notunu koruyor',
    'TUR033: Canlı not kasası şifreli içerik bozulduğunda yeni yedek reddediliyor; önceki sağlam yedek korunuyor',

    'TUR033: CMK134 AYM 2023/128 2026/36 karar kaydı ve 25.02.2027 tarih uyarısı gösteriliyor',
    'TUR033: Olay tarihi 24.02.2027 → 25.02.2027 değişince norm riski yenileniyor, otomatik yetki PASS yok',
    'TUR033: Olay tarihi değişince CMK127/3 önceki 24/48 saat raporu GEÇERSİZ hale geliyor',
    'TUR033: Resmî Gazete AYM karar LİSTESİ ile tam mahkeme kararı ve konsolide mevzuat eşdeğer sayılmıyor',
    'TUR033: Önceden hâkim kararı beyanı, esas maddeleri ve CMK134 iptal değişikliği karıştırılmıyor',

    'TUR033: ÖN HÂKİM KARARI BELİRSİZ — CMK127/3 24/48 mekanik hesabı bloke',
    'TUR033: ÖNCEDEN HÂKİM KARARI VAR — kararsız elkoyma için 24/48 hesap uygulanmaz',
    'TUR033: ÖNCEDEN HÂKİM KARARI YOK — yalnız mekanik saatler, hukuki PASS verilmez',
    'TUR033: Hâkim kararı dayanağı değişince önceki saat incelemesi GEÇERSİZ',
    'TUR033: BUILD_PROVENANCE.json HEAD ve NO-GO kaydı bulunur; dijital imza değildir',

    'TUR033: Ceza oranında gün/pay/payda veya indirim seçimi değişince eski sonuç GEÇERSİZ',
    'TUR033: Günlük tutar veya suç tarihi değişince önceki adlî para sonucu GEÇERSİZ',
    'TUR033: YDO, kaynak veya eski TL değişince eski sonuç kopyalanamaz',
    'TUR033: Geçersiz tarih veya kesir girildiğinde eski hesap kopyalanamaz',
    'TUR033: CMK 127/3 belge referansı/saat değişince eski süre karşılaştırması GEÇERSİZ',
    'TUR033: Danıştay resmî arama portalı ile özgün mahkeme karar sureti statüsü ayrı görünür',

    'Adli Hesaplama Merkezi ve Ceza oran hesaplama, Hapis / para hesabı, YDO / para dönüşümü sekmeleri erişilebilir',
    '100 tam gün 1/6 indirim 83, artırım 116 tam gün örneği',
    'Günlük 20-100 TL ve 100-500 TL bilgisi 1 Haziran 2024 değişikliğiyle tarihsel ayrılır',
    'YDO oranları manuel bileşik; eski TL 1000000 birim => 1 TL dönüşümü',
    'Hukuki ceza, hapis dönüşümü, lehe kanun veya YDO adli para uygulanması için otomatik PASS yok',

    'Arama ve El Koyma ekranında m8(a), m8(f), m30(1), m17(1) uyarıları ve kaynak düzeyleri görünür',
    'Yönetmelik adlî m5-17, önleme m18-26 ve icra m27-32 ayrımı görünür',
    'Olay tarihi verilse de hukuki kaynak sürüm PASS oluşturulmaz',
    'CMK127/3 elkoyma-sunma ve elkoyma-hâkim kararı 24/48 saat hesapları manuel referans sınırında',
    'Eksik belge veya saat-dakika/UTC farkı bulunmadığında süreye otomatik hukuki uygunluk verilmez',

    'Tutuklama başlığı altında Tutuklama Sistematiği açılır; öğretici şema ve dosya denetimi çalışır',
    'Adli Kontrol başlığı altında Adli Kontrol Sistematiği açılır; CMK 109-112 ve 110/A görünür',
    'Arama ve El Koyma Sistematiği: CMK 116-134 ile Yönetmelik adlî arama m.5-17 ayrımı gösterilir',
    'Arama sistematiği: CMK 118 gece, 119/4 iki tanık, 127/3 24/48 ve CMK 134 özel rejim kontrol edilir',
    'Dosya denetimi tamamı işaretlendiğinde bile uygulama hukuki uygunluk PASS üretmez',
    'Kimliksiz evrak referansları RAM dışında otomatik kayıt edilmez; kapanış ve pano uyarıları çalışır',
    'WPF starts offline on Windows 11 x64 with authentic original E-terazi icon',
    'Light and dark themes; detached native popup; no HTML browser launcher',
    'Exactly two date modules with leap day/month-end manual evidence',
    'Gece Vakti fails closed if official UYAP timetable is unavailable',
    'Synthetic WPF bubble, accessibility/focus, 100%, 150% and 200% DPI',
    'AES vault wrong password blocked and reopen retains synthetic note',
    'New vault ciphertext and legacy plaintext remain distinct (no personal data)',
    'Consent unchecked prevents migration and preview leaves source unchanged',
    'Consent + explicit confirmation imports synthetic legacy data without overwrite',
    'After migration old notes remain; rollback snapshot can restore encrypted state',
    'Corrupt/foreign backup rejected without destroying current vault',
    'Candidate PDF changed bytes rejected, still no official fulltext PASS',
    'Windows app closes cleanly while no dirty notes, unsaved notes block close',
    'No sensitive data transferred, network upload or installer/release'
)
# TUR033: the prior collector saved a JSON record only AFTER 158 manual
# prompts. Ctrl+C or power loss discarded all earlier observations.
# Every completed answer now gets its own immutable, SHA-named checkpoint.
# This is user-reported data, not an independent physical Windows attestation.
if ($cases.Count -ne 158) {
    throw "TUR033 expected exactly 158 manual cases, got $($cases.Count)."
}
$root = Join-Path $env:LOCALAPPDATA 'Embasic\Legal\UAT'
New-Item -ItemType Directory -Force -Path $root | Out-Null
$root = [IO.Path]::GetFullPath($root).TrimEnd([IO.Path]::DirectorySeparatorChar)
$results = @()
$sessionDirectory = ''
$lastCheckpoint = ''
if ($ResumeFrom -ne '') {
    $sessionDirectory = [IO.Path]::GetFullPath($ResumeFrom).TrimEnd([IO.Path]::DirectorySeparatorChar)
    if (-not $sessionDirectory.StartsWith($root + [IO.Path]::DirectorySeparatorChar,
          [StringComparison]::OrdinalIgnoreCase) -or
        -not (Test-Path -LiteralPath $sessionDirectory -PathType Container)) {
        throw 'ResumeFrom must be an existing local Embasic/Legal/UAT session directory.'
    }
    $snapshots = @(Get-ChildItem -LiteralPath $sessionDirectory -Filter 'checkpoint-*.json' -File |
        Sort-Object Name)
    if ($snapshots.Count -eq 0) { throw 'No committed UAT checkpoint to resume.' }
    $stepExpected = 0
    foreach ($snapshot in $snapshots) {
        $checkpointName = [regex]::Match($snapshot.Name,
            '^checkpoint-([0-9]{3})-([a-f0-9]{64})\.json$')
        if (-not $checkpointName.Success) { throw "Unexpected checkpoint filename: $($snapshot.Name)" }
        $step = [int]$checkpointName.Groups[1].Value
        $hashExpected = $checkpointName.Groups[2].Value
        if ($step -ne $stepExpected) { throw 'UAT checkpoint sequence gap or duplicate.' }
        if ((Get-FileHash -LiteralPath $snapshot.FullName -Algorithm SHA256).Hash.ToLowerInvariant() -ne $hashExpected) {
            throw "Checkpoint SHA-256 mismatch; do not modify evidence: $($snapshot.Name)"
        }
        $proof = Get-Content -LiteralPath $snapshot.FullName -Raw | ConvertFrom-Json
        if ($proof.application -ne 'Embasic Legal Windows TUR033 Alpha' -or
            $proof.ci_claimed_git_head_sha -ne $prov.git_head_sha -or
            $proof.exesha256 -ne $actualSha -or
            [int]$proof.manifest_members_checked -ne $verified -or
            $proof.legal_production -ne 'NO-GO' -or
            $proof.evidence_grade -ne 'USER_SELF_REPORTED_NOT_INDEPENDENTLY_VERIFIED') {
            throw 'Checkpoint identity or source differs from independently checked package.'
        }
        $prior = @($proof.results | Where-Object { $null -ne $_ })
        if ($prior.Count -ne $step) { throw 'Checkpoint number and result count differ.' }
        for ($i = 0; $i -lt $prior.Count; $i++) {
            $entry = $prior[$i]
            if ($entry.id -ne ('UAT-{0:D3}' -f ($i + 1)) -or
                $entry.check -cne $cases[$i] -or
                $entry.status -notin @('PASS','FAIL','NOT_RUN') -or
                ($entry.status -ne 'FAIL' -and $entry.observation -ne '') -or
                ($entry.status -eq 'FAIL' -and $entry.observation -notin @('UI','MATH','CRASH','ACCESS','OTHER'))) {
                throw 'Checkpoint observations invalid or not in canonical UAT order.'
            }
        }
        $results = $prior
        $lastCheckpoint = $snapshot.FullName
        $stepExpected++
    }
    Write-Host "RESUMED $($results.Count)/$($cases.Count) user-reported checks, SHA verified."
} else {
    $sessionDirectory = Join-Path $root ('TUR033-UAT-' + (Get-Date -Format 'yyyyMMdd-HHmmss') +
        '-' + [guid]::NewGuid().ToString('N').Substring(0,8))
    New-Item -ItemType Directory -Path $sessionDirectory -ErrorAction Stop | Out-Null
}
function Save-UserCheckpoint {
    param([object[]] $Observations)
    $complete = $Observations.Count -eq $cases.Count
    $proof = [ordered]@{
        application = 'Embasic Legal Windows TUR033 Alpha'
        time_local = (Get-Date).ToString('o')
        os_version = [Environment]::OSVersion.VersionString
        exesha256 = $actualSha
        ci_claimed_git_head_sha = $prov.git_head_sha
        exact_head_user_crosschecked_against_github_claim = $true
        source_head_cryptographically_signed = $false
        package_provenance_signed = $false
        manifest_members_checked = $verified
        simulated_tests = $false
        evidence_grade = 'USER_SELF_REPORTED_NOT_INDEPENDENTLY_VERIFIED'
        exact_manifest_files_verified = $verified
        physical_windows_independently_verified = $false
        physical_windows_acceptance = if (-not $complete) { 'INCOMPLETE_OR_FAIL' }
            elseif (@($Observations | Where-Object {$_.status -ne 'PASS'}).Count -eq 0) {
                'USER_REPORTED_ALL_PASS_PENDING_INDEPENDENT_REVIEW'
            } else { 'INCOMPLETE_OR_FAIL' }
        legal_production = 'NO-GO'
        results = @($Observations)
    }
    $json = $proof | ConvertTo-Json -Depth 6
    $tmp = Join-Path $sessionDirectory ('.checkpoint-' + [guid]::NewGuid().ToString('N') + '.tmp')
    try {
        [IO.File]::WriteAllText($tmp, $json, [System.Text.UTF8Encoding]::new($false))
        $hash = (Get-FileHash -LiteralPath $tmp -Algorithm SHA256).Hash.ToLowerInvariant()
        $filename = 'checkpoint-{0:D3}-{1}.json' -f $Observations.Count, $hash
        $dest = Join-Path $sessionDirectory $filename
        if (Test-Path -LiteralPath $dest) { throw 'Immutable checkpoint already exists; refusing overwrite.' }
        [IO.File]::Move($tmp, $dest)
        return $dest
    } finally {
        if (Test-Path -LiteralPath $tmp) { Remove-Item -LiteralPath $tmp -Force }
    }
}
if ($results.Count -eq 0 -and $ResumeFrom -eq '') {
    $lastCheckpoint = Save-UserCheckpoint -Observations $results
}
$limit = [Math]::Min($cases.Count, $results.Count + $BatchSize)
while ($results.Count -lt $limit) {
    $case = $cases[$results.Count]
    $caseId = 'UAT-{0:D3}' -f ($results.Count + 1)
    Write-Host ''
    Write-Host $case
    $answer = (Read-Host 'P=observed PASS; F=observed FAIL; N=NOT_RUN (default)').Trim().ToUpperInvariant()
    if ($answer -notin @('P','F','N')) { $answer = 'N' }
    $note = ''
    if ($answer -eq 'F') {
        $note = (Read-Host 'Yalnız hata türü: UI, MATH, CRASH, ACCESS, OTHER').Trim().ToUpperInvariant()
        if ($note -notin @('UI','MATH','CRASH','ACCESS','OTHER')) { $note = 'OTHER' }
    }
    $results += [pscustomobject]@{
        id = $caseId
        check = $case
        status = @{P='PASS'; F='FAIL'; N='NOT_RUN'}[$answer]
        observation = $note
    }
    $lastCheckpoint = Save-UserCheckpoint -Observations $results
    Write-Host "CHECKPOINT $($results.Count)/$($cases.Count): $lastCheckpoint"
}
Write-Host ''
Write-Host "LOCAL USER OBSERVATIONS: $($results.Count)/$($cases.Count)"
Write-Host "LAST VERIFIED-FORMAT CHECKPOINT: $lastCheckpoint"
Write-Host "SESSION FOLDER: $sessionDirectory"
Write-Host "CONTINUE: .\TUR033-Windows-UAT.ps1 -ResumeFrom \`"$sessionDirectory\`" -BatchSize 10"
Write-Host 'SHA filenames detect accidental corruption, NOT intentional tampering or digital signatures.'
Write-Host 'USER_SELF_REPORTED; independent physical Windows verification PENDING; legal/release NO-GO.'
