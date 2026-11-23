# Embasic Legal TUR031 — manual, offline, non-sensitive Windows UAT evidence collector.
# No UI tests are simulated and no results are marked PASS automatically.
[CmdletBinding()]
param([string]$ExePath = (Join-Path $PSScriptRoot 'Embasic.Legal.exe'))
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
    throw 'Missing BUILD_PROVENANCE.json. TUR031 UAT blocked.'
}
$prov = Get-Content -LiteralPath $provPath -Raw | ConvertFrom-Json
if ($prov.product -ne 'Embasic Legal Windows TUR031 Alpha' -or
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
$independentHead = (Read-Host 'GitHub TUR031 Draft PR sayfasından bağımsız gördüğünüz exact 40-hane HEAD SHA').Trim().ToLowerInvariant()
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
    'TUR031: Win11 gerçek 900-DIP minimum pencere ve 125%/150% ekran ölçeğinde CMK 74 Tablo sekmesindeki dört alan dikey kartta kırpılmadan, yatay sürükleme olmadan okunur',
    'TUR031: CMK 134 ve başka merkezî derste her kart ölçüt/birinci/ikinci/neden alanlarını tam verir; Geniş dört sütunlu tablo Expander açılınca aynı veriler orijinal tabloda görünür ve yatay kaydırılabilir',
    'TUR031: Alternatif Çözümler Tablo sekmesinde legacy Matrix kartları açılır; ayrı üç ders, açık/koyu tema, klavye Tab/Enter, ekran okuyucu odak adları ve ana işleme dönüş çalışır',
    'TUR031: Ceza Muhakemesi Hukuku 5 · Tekrar sekmesi başlangıçta yanlışlıkla başarısız 47 konu listesi üretmez',
    'TUR031: CMK 74 quiz ilk yanlış cevaptan sonra tekrar listesinde görünür; sonraki doğru yanıt ilk deneme sayısını değiştirmez',
    'TUR031: CMK 134 hatırlama kartında Tekrar çalışacağım seçilince CMK 134 listede belirir, Hatırladım seçilince kart tekrar niteliği kaybolur',
    'TUR031: İki konunun tekrar satırları Derse dön ve tekrar çalış ile kendi konusunu açar, 47 konu ve ayrı Alternatif Çözümler korunur',
    'TUR031: Oturumu sıfırla tüm konu/kart/ilk hata tekrar listesini temizler; NOT_VERIFIED ve fiziksel UAT durumu değişmez',
    'TUR031: CMK 74 konusunda 2 kontrol işaretle, CMK 75-77 konusuna geç, sonra dön; 2 işaret ve sayaç korunur',
    'TUR031: 47 merkez konusunun her birinde öğrenme hedefleri, madde okuma rehberi ve üç hatırlama kartı görünür',
    'TUR031: Hatırlama kartı cevabı ilk başta gizlidir; açma ve Hatırladım/Tekrar özdeğerlendirmesi çalışır, konu dönüşünde korunur',
    'TUR031: Şema adımı seçildiğinde açıklama ve vurgu değişir; Sonraki adım dört adım arasında ilerler',
    'TUR031: Senaryoda yol seçimi vurgulanır ve o seçeneğe özgü açıklama gösterilir; başka konuya gidip dönünce seçim korunur',
    'TUR031: Karşılaştırma tablosu iki kavramı üç ortak ölçüt üzerinden karşılaştırır; uzun hücreler kırpılmaz',
    'TUR031: Uygulama testinde yanlış yanıt sonrası doğru yanıt denenebilir; ilk deneme puanı artmaz, sonradan doğru sayısı ayrı görünür',
    'TUR031: Oturumu Sıfırla kartları, dal seçimlerini, eğitim puanını ve kontrol işaretlerini temizler; işlem sistematiği verileri kendi mevcut sıfırlamasıyla yönetilir',
    'TUR031: Ceza Muhakemesi Hukuku tek açılır üst menüdür; 13 kategori bunun altındadır; eski CMK Öğrenme Merkezi adı görünmez',
    'TUR031: Tutuklama, Adli Kontrol, Arama ve El Koyma ayrı üst menü değildir; ilgili konunun İşlem sistematiği sekmesinde çalışır',
    'TUR031: Alternatif Çözümler ayrı üst menüde yalnız Seri Muhakeme, Uzlaştırma ve Ön Ödeme içerir; Basit yargılama merkezde kalır',
    'TUR031: Birleşik CMK250/253 karşılaştırma başlığı görünmez; konular yalnız ayrıntılı derslerde birer kez listelenir',
    'TUR031: Bir sistematikteki kimliksiz işaret ve belge referansı konular arasında korunur; uygulama kapatılırken mevcut uyarı sürer',
    'TUR031: Ana sayfada Ceza Muhakemesi Hukuku — Aç ve güncel 13/47/94 özeti görünür; tıklama gerçek öğrenme ekranını açar',
    'TUR031: Ana sayfadaki Soruşturma, Kovuşturma ve Kanun Yolları bağlantıları doğru kategoriyi açar',
    'TUR031: Mevcut Alternatif Çözümler menüsü boş hazırlık ekranı yerine Seri Muhakeme dersini açar',
    'TUR031: Ana sayfaya dönme ve Alternatif Çözümleri yeniden açma öğrenme işaretlerini korur',
    'TUR031: Sol menüde Ceza Muhakemesi Hukuku ve 13 kategori doğrudan açılıyor',
    'TUR031: Her kategori seçildiğinde o kategorinin ilk veya korunmuş konusu gösteriliyor; kategori ile konu uyumlu',
    'TUR031: Merkez 47 konu/94 soru, ayrı Alternatif Çözümler 3 konu/6 soru gösteriyor; küresel payda 100 kalıyor',
    'TUR031: Anlatım sekmesinde kavram, birbirine karıştırma ve örnek metinleri seçili konuya göre değişiyor',
    'TUR031: Şema adımları fare ve klavyeyle seçiliyor; uzun metin yatay kırpılmıyor',
    'TUR031: İtiraz, istinaf, temyiz ve yenileme dersleri ayrı açılıyor; yeni konu testinde doğru ve yanlış geribildirimi çalışıyor',
    'TUR031: Yeni soruşturma ve kanun yolları konuları arasında gidip dönünce işaretler korunuyor; tüm oturum sıfırlama bunları siliyor',
    'TUR031: Kaynak sekmesi kullanıcının 1.5.5271.doc adresini ve doğrulanamama durumunu gösteriyor; CMK bağlantısını kopyala DOC adresini kopyalıyor',
    'TUR031: 100/150/200 DPI ve minimum pencere boyutunda sol menü, soru seçenekleri ve anlatım kaydırılarak okunabiliyor',
    'TUR031: CMK 74 ile CMK 134 kontrol listesi birbirinden bağımsızdır; işaret kaldırma diğer konuyu değiştirmez',
    'TUR031: Eğitim oturumunu sıfırla bütün 50 konunun işaretlerini ve eğitim puanlarını temizler; NOT_VERIFIED sürer',
    'TUR031: Ceza Muhakemesi Hukuku sol menüden açılır; Adlî incelemeler kategorisinde eski CMK 74 konusu seçilebilir',
    'TUR031: Eski 13 konu CMK74, muayene, genetik, adli kontrol, tutuklama, arama ve alternatif çözümleri kapsar',
    'TUR031: 4 aşamalı akış her konuda doğru seçimle değişir; hiçbir madde güncel VERIFIED olmaz',
    'TUR031: Merkez karşılaştırmalı tablosu her konuda 3 ölçüt gösterir; ayrı alternatif bölümünün eski kontrol matrisi 4 satırdır; resmî kanun lafzı diye sunulmaz',
    'TUR031: RAM kontrol listesi işaretlenir ve sayacı günceller; dosya verisi kaydı yoktur',
    'TUR031: Senaryo testinde seçim yokken puan artmaz; yanlış cevap ayrı bildirilir',
    'TUR031: Doğru eğitim cevabı dahi hukuki uygunluk PASS veya işlem kararı oluşturmaz',
    'TUR031: Sonraki soruya geçme, konu değiştirme ve oturum sıfırlama işlevleri çalışır',
    'TUR031: Resmi kaynak bağlantısını kopyalama yalnız mevzuat.gov.tr içerir; web otomatik açılmaz',
    'TUR031: Eski 2 tarih, tek gece, 3 sistematik, para/YDO, ikon, tema ve AES-GCM not korunur',
    'TUR031: Mevcut kasa restore öncesinde rollback.pre-restore.sqlite3-wal kalıntısı varsa hedef SHA korunur',
    'TUR031: Geri alma dosyasının -shm kalıntısı mevcutsa işlem durur ve kalıntı korunur',
    'TUR031: Geri alma dosyasının -journal kalıntısı mevcutsa işlem durur ve kalıntı korunur',
    'TUR031: Geri alma dosyasının adı bir DİZİN tarafından işgal edilmişse File.Exists false olsa da restore reddedilir',
    'TUR031: Yan dosyasız mevcut kasa restore onayı geri alma dosyasını üretir, eski notlar ayrı açılır; TOCTOU PASS değildir',
    'TUR031: Normal kasa Unlock -wal adlı mevcut dosyayı salt-okunur korur ve açılışı reddeder',
    'TUR031: Normal kasa Unlock -shm adlı mevcut dosyayı korur ve açılışı reddeder',
    'TUR031: Normal kasa Unlock -journal adlı mevcut dosyayı korur ve açılışı reddeder',
    'TUR031: Normal kasa Unlock -shm adlı mevcut DİZİN varsa yedeksiz/düzeltmesiz reddeder',
    'TUR031: Yeni kasa yokken -journal kalıntısı varsa yeni dosya oluşturulmaz',
    'TUR031: Yan dosyası olmayan sentetik kasada oluşturma/açma/kaydetme işlevi çalışır; TOCTOU PASS değildir',
    'TUR031: Yabancı SQLite dosyasına normal kasa açılışı denenince dosya SHA ve şema değişmez',
    'TUR031: Eski şifresiz SQLite not dosyası EAL4 kasa sanılmaz; kaynak ve not baytları değişmez',
    'TUR031: Mevcut şifreli kasada yanlış parola sonrası kaynak SHA değişmez; doğru parola ile açılır',
    'TUR031: Gerçek geçerli kasa salt-okunur ön kimlik doğrulamadan sonra not kaydedebilir',
    'TUR031: Yeni kasa oluşturma ve bozuk şifreli notta yedek reddi davranışları korunur',
    'TUR031: Salt-okunur ön kimlik doğrulama NTFS file-lock / TOCTOU / junction PASS olarak sunulmaz',
    'TUR031: Şifreli yedek hedefi -wal adına DİZİN içeriyorsa yedek korunur ve işlem reddedilir',
    'TUR031: Şifreli yedek hedefi -shm/-journal adlı DİZİN içeriyorsa fail-closed ret uygulanır',
    'TUR031: Restore hedefinin -wal adlı DİZİN içermesi ön inceleme ve geri yüklemeyi durdurur',
    'TUR031: Legacy şifresiz SQLite -shm adlı DİZİN içermesi göçü durdurur ve kaynağı korur',
    'TUR031: Reparse, junction ve TOCTOU için fiziksel Windows kanıtı yoksa PASS yoktur',
    'TUR031: Oturumun ilk otomatik ham SQLite yedeğinde üstveri açıklaması EVET/HAYIR onayı gelir',
    'TUR031: HAYIR halinde not şifreli kaydedilir ve otomatik ham SQLite kopyası oluşmaz',
    'TUR031: HAYIR ardından kullanıcı notu silmek isterse yedeksiz silme engellenir',
    'TUR031: EVET yalnız açık kasa oturumu içindir; kilitlemede/uygulamadan çıkışta sıfırlanır',
    'TUR031: Manuel export ayrı üstveri uyarısını korur; yeni kopyanın metadata gizliliği PASS iddiası yok',
    'TUR031: Şifreli SQLite yedek hedefinde -journal varsa yedek aynı kalır ve işlem durur',
    'TUR031: Şifreli SQLite yedek hedefinde -wal/-shm varsa işlem durur ve yan dosya silinmez',
    'TUR031: Eski düz metin SQLite yedek hedefinde -journal/-wal/-shm varsa yedek üzerine yazılmaz',
    'TUR031: Yan dosyasız temiz hedefe şifreli yedek yeniden alınabilir ve AES-GCM notu açılır',
    'TUR031: Yedek hedefinin yan dosyaları/snapshot durumunda otomatik checkpoint, silme veya güvenli disk temizliği iddiası yoktur',
    'TUR031: Eski düz metin SQLite kaynağının -journal yan dosyası varsa ön inceleme durur',
    'TUR031: Eski düz metin SQLite kaynağının -wal yan dosyası varsa ön inceleme durur',
    'TUR031: Eski düz metin SQLite kaynağının -shm yan dosyası varsa ön inceleme durur',
    'TUR031: Ön incelemeden sonra ortaya çıkan -journal/-wal göçü durdurur ve mevcut notlara dokunmaz',
    'TUR031: Yan dosyalar otomatik silinmez ve kanıtsız temizleme/normalleştirme önerilmez',
    'TUR031: Yabancı SQLite yedek ön incelemesinde tablo/indeks eklenmez ve kaynak SHA değişmez',
    'TUR031: Yabancı SQLite hedef ön incelemesinde tablo/indeks eklenmez ve hedef SHA değişmez',
    'TUR031: Geçerli şifreli yedek ön incelemesinden önce/sonra her iki SQLite SHA aynı kalır',
    'TUR031: Ön inceleme bulunmayan yedeği veya klasörü otomatik oluşturmaz',
    'TUR031: Açık kullanıcı onaylı geri yükleme eski geçerli kasayı korur ve yanlış parola reddedilir',
    'TUR031: HMAC makbuzunun kasa ile aynı klasöre yazılması reddedilir; kasa değişmez',
    'TUR031: Farklı klasör tek başına bağımsız güven kökü veya anti-rollback PASS sayılmaz',
    'TUR031: Şifreli SQLite yedeğinin açık kanun/madde/sürüm metadata riski için EVET/HAYIR onayı istenir',
    'TUR031: Metadata uyarısında HAYIR seçilince yeni yedek dosyası oluşturulmaz',
    'TUR031: Geri yükleme öncesi WAL/SHM/journal yan dosyası varsa geri yükleme yapılmaz',
    'TUR031: Bilinen eski düz metin SQLite ve WAL/SHM/journal dosyaları yalnız sayılarla listelenir',
    'TUR031: Dosya içeriği ve gerçek soruşturma bilgisi rapora veya panoya aktarılmaz',
    'TUR031: Şifreli V4 kasa dosyası yanlışlıkla eski şifresiz kaynak olarak sayılmaz',
    'TUR031: Eski dosyalar silinmez/ACL değişmez; dışa aktarılmış farklı klasör yedeklerine güvenlik PASS yok',

    'TUR031: Sentetik iki notla ayrı JSON bütünlük makbuzu oluşturulur; metin/madde kimliği dosyada görünmez',
    'TUR031: Haricî saklanan makbuz aynı kasa içeriğiyle eşleşir; gerçek hukuk PASS diye gösterilmez',
    'TUR031: Not silinip/düzenlenince eski makbuz uyuşmazlığı gösterir; otomatik veri silinmez',
    'TUR031: Eski geçerli kasaya dönüşte bağımsız yeni makbuz eşleşmez; birlikte değiştirilen dosyalara karşı tam anti-rollback iddiası yok',

    'TUR031: Eski yedek yeni sürüm notu geri alıyorsa WPF veri kaybı sayısı gösterir',
    'TUR031: HAYIR seçilirse geri yükleme yapılmaz ve hedef kasa korunur',
    'TUR031: Açık EVET onayından önce yedek ve hedef SHA-256 yeniden doğrulanır',
    'TUR031: Yeni not yedekte yoksa kaybolma riski açıkça bildirilir',
    'TUR031: Eski düz metin not dosyası ve açık metadata otomatik silinmez',

    'TUR031: Yedek AES-GCM doğrulaması, yalnız SQLite quick_check PASS ile karıştırılmıyor',
    'TUR031: Bozulmuş not içeriği olan yedek geri yükleme reddediliyor; hedef kasa mevcut notunu koruyor',
    'TUR031: Canlı not kasası şifreli içerik bozulduğunda yeni yedek reddediliyor; önceki sağlam yedek korunuyor',

    'TUR031: CMK134 AYM 2023/128 2026/36 karar kaydı ve 25.02.2027 tarih uyarısı gösteriliyor',
    'TUR031: Olay tarihi 24.02.2027 → 25.02.2027 değişince norm riski yenileniyor, otomatik yetki PASS yok',
    'TUR031: Olay tarihi değişince CMK127/3 önceki 24/48 saat raporu GEÇERSİZ hale geliyor',
    'TUR031: Resmî Gazete AYM karar LİSTESİ ile tam mahkeme kararı ve konsolide mevzuat eşdeğer sayılmıyor',
    'TUR031: Önceden hâkim kararı beyanı, esas maddeleri ve CMK134 iptal değişikliği karıştırılmıyor',

    'TUR031: ÖN HÂKİM KARARI BELİRSİZ — CMK127/3 24/48 mekanik hesabı bloke',
    'TUR031: ÖNCEDEN HÂKİM KARARI VAR — kararsız elkoyma için 24/48 hesap uygulanmaz',
    'TUR031: ÖNCEDEN HÂKİM KARARI YOK — yalnız mekanik saatler, hukuki PASS verilmez',
    'TUR031: Hâkim kararı dayanağı değişince önceki saat incelemesi GEÇERSİZ',
    'TUR031: BUILD_PROVENANCE.json HEAD ve NO-GO kaydı bulunur; dijital imza değildir',

    'TUR031: Ceza oranında gün/pay/payda veya indirim seçimi değişince eski sonuç GEÇERSİZ',
    'TUR031: Günlük tutar veya suç tarihi değişince önceki adlî para sonucu GEÇERSİZ',
    'TUR031: YDO, kaynak veya eski TL değişince eski sonuç kopyalanamaz',
    'TUR031: Geçersiz tarih veya kesir girildiğinde eski hesap kopyalanamaz',
    'TUR031: CMK 127/3 belge referansı/saat değişince eski süre karşılaştırması GEÇERSİZ',
    'TUR031: Danıştay resmî arama portalı ile özgün mahkeme karar sureti statüsü ayrı görünür',

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
$results = @()
foreach ($case in $cases) {
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
        status = @{P='PASS';F='FAIL';N='NOT_RUN'}[$answer]
        observation = $note
    }
}
$proof = [ordered]@{
    application = 'Embasic Legal Windows TUR031 Alpha'
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
    physical_windows_acceptance = if (($results | Where-Object {$_.status -ne 'PASS'}).Count -eq 0) {'USER_REPORTED_ALL_PASS_PENDING_INDEPENDENT_REVIEW'} else {'INCOMPLETE_OR_FAIL'}
    legal_production = 'NO-GO'
    results = $results
}
$location = Join-Path $env:LOCALAPPDATA 'Embasic\Legal\UAT'
New-Item -ItemType Directory -Force -Path $location | Out-Null
$dest = Join-Path $location ('TUR031-UAT-' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '.json')
$proof | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $dest -Encoding utf8
$recordSha = (Get-FileHash -Algorithm SHA256 -LiteralPath $dest).Hash.ToLowerInvariant()
"$recordSha  $(Split-Path -Leaf $dest)" | Set-Content -LiteralPath ($dest + '.sha256') -Encoding utf8
Write-Host ''
Write-Host "LOCAL USER OBSERVATIONS RECORDED: $dest"
Write-Host "LOCAL RECORD SHA-256: $recordSha"
Write-Host 'Bu kayıt bir kriptografik imza veya bağımsız Windows GUI testi DEĞİLDİR.'
Write-Host 'All evidence requires independent review. Official-law and release NO-GO remain.'
