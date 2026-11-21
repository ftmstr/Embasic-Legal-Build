# Embasic Legal TUR017 — manual, offline, non-sensitive Windows UAT evidence collector.
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
    throw 'Missing BUILD_PROVENANCE.json. TUR017 UAT blocked.'
}
$prov = Get-Content -LiteralPath $provPath -Raw | ConvertFrom-Json
if ($prov.product -ne 'Embasic Legal Windows TUR017 Alpha' -or
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
$independentHead = (Read-Host 'GitHub TUR017 Draft PR sayfasından bağımsız gördüğünüz exact 40-hane HEAD SHA').Trim().ToLowerInvariant()
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
    'TUR017: Eski düz metin SQLite kaynağının -journal yan dosyası varsa ön inceleme durur',
    'TUR017: Eski düz metin SQLite kaynağının -wal yan dosyası varsa ön inceleme durur',
    'TUR017: Eski düz metin SQLite kaynağının -shm yan dosyası varsa ön inceleme durur',
    'TUR017: Ön incelemeden sonra ortaya çıkan -journal/-wal göçü durdurur ve mevcut notlara dokunmaz',
    'TUR017: Yan dosyalar otomatik silinmez ve kanıtsız temizleme/normalleştirme önerilmez',
    'TUR017: Yabancı SQLite yedek ön incelemesinde tablo/indeks eklenmez ve kaynak SHA değişmez',
    'TUR017: Yabancı SQLite hedef ön incelemesinde tablo/indeks eklenmez ve hedef SHA değişmez',
    'TUR017: Geçerli şifreli yedek ön incelemesinden önce/sonra her iki SQLite SHA aynı kalır',
    'TUR017: Ön inceleme bulunmayan yedeği veya klasörü otomatik oluşturmaz',
    'TUR017: Açık kullanıcı onaylı geri yükleme eski geçerli kasayı korur ve yanlış parola reddedilir',
    'TUR017: HMAC makbuzunun kasa ile aynı klasöre yazılması reddedilir; kasa değişmez',
    'TUR017: Farklı klasör tek başına bağımsız güven kökü veya anti-rollback PASS sayılmaz',
    'TUR017: Şifreli SQLite yedeğinin açık kanun/madde/sürüm metadata riski için EVET/HAYIR onayı istenir',
    'TUR017: Metadata uyarısında HAYIR seçilince yeni yedek dosyası oluşturulmaz',
    'TUR017: Geri yükleme öncesi WAL/SHM/journal yan dosyası varsa geri yükleme yapılmaz',
    'TUR017: Bilinen eski düz metin SQLite ve WAL/SHM/journal dosyaları yalnız sayılarla listelenir',
    'TUR017: Dosya içeriği ve gerçek soruşturma bilgisi rapora veya panoya aktarılmaz',
    'TUR017: Şifreli V4 kasa dosyası yanlışlıkla eski şifresiz kaynak olarak sayılmaz',
    'TUR017: Eski dosyalar silinmez/ACL değişmez; dışa aktarılmış farklı klasör yedeklerine güvenlik PASS yok',

    'TUR017: Sentetik iki notla ayrı JSON bütünlük makbuzu oluşturulur; metin/madde kimliği dosyada görünmez',
    'TUR017: Haricî saklanan makbuz aynı kasa içeriğiyle eşleşir; gerçek hukuk PASS diye gösterilmez',
    'TUR017: Not silinip/düzenlenince eski makbuz uyuşmazlığı gösterir; otomatik veri silinmez',
    'TUR017: Eski geçerli kasaya dönüşte bağımsız yeni makbuz eşleşmez; birlikte değiştirilen dosyalara karşı tam anti-rollback iddiası yok',

    'TUR017: Eski yedek yeni sürüm notu geri alıyorsa WPF veri kaybı sayısı gösterir',
    'TUR017: HAYIR seçilirse geri yükleme yapılmaz ve hedef kasa korunur',
    'TUR017: Açık EVET onayından önce yedek ve hedef SHA-256 yeniden doğrulanır',
    'TUR017: Yeni not yedekte yoksa kaybolma riski açıkça bildirilir',
    'TUR017: Eski düz metin not dosyası ve açık metadata otomatik silinmez',

    'TUR017: Yedek AES-GCM doğrulaması, yalnız SQLite quick_check PASS ile karıştırılmıyor',
    'TUR017: Bozulmuş not içeriği olan yedek geri yükleme reddediliyor; hedef kasa mevcut notunu koruyor',
    'TUR017: Canlı not kasası şifreli içerik bozulduğunda yeni yedek reddediliyor; önceki sağlam yedek korunuyor',

    'TUR017: CMK134 AYM 2023/128 2026/36 karar kaydı ve 25.02.2027 tarih uyarısı gösteriliyor',
    'TUR017: Olay tarihi 24.02.2027 → 25.02.2027 değişince norm riski yenileniyor, otomatik yetki PASS yok',
    'TUR017: Olay tarihi değişince CMK127/3 önceki 24/48 saat raporu GEÇERSİZ hale geliyor',
    'TUR017: Resmî Gazete AYM karar LİSTESİ ile tam mahkeme kararı ve konsolide mevzuat eşdeğer sayılmıyor',
    'TUR017: Önceden hâkim kararı beyanı, esas maddeleri ve CMK134 iptal değişikliği karıştırılmıyor',

    'TUR017: ÖN HÂKİM KARARI BELİRSİZ — CMK127/3 24/48 mekanik hesabı bloke',
    'TUR017: ÖNCEDEN HÂKİM KARARI VAR — kararsız elkoyma için 24/48 hesap uygulanmaz',
    'TUR017: ÖNCEDEN HÂKİM KARARI YOK — yalnız mekanik saatler, hukuki PASS verilmez',
    'TUR017: Hâkim kararı dayanağı değişince önceki saat incelemesi GEÇERSİZ',
    'TUR017: BUILD_PROVENANCE.json HEAD ve NO-GO kaydı bulunur; dijital imza değildir',

    'TUR017: Ceza oranında gün/pay/payda veya indirim seçimi değişince eski sonuç GEÇERSİZ',
    'TUR017: Günlük tutar veya suç tarihi değişince önceki adlî para sonucu GEÇERSİZ',
    'TUR017: YDO, kaynak veya eski TL değişince eski sonuç kopyalanamaz',
    'TUR017: Geçersiz tarih veya kesir girildiğinde eski hesap kopyalanamaz',
    'TUR017: CMK 127/3 belge referansı/saat değişince eski süre karşılaştırması GEÇERSİZ',
    'TUR017: Danıştay resmî arama portalı ile özgün mahkeme karar sureti statüsü ayrı görünür',

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
    application = 'Embasic Legal Windows TUR017 Alpha'
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
$dest = Join-Path $location ('TUR017-UAT-' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '.json')
$proof | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $dest -Encoding utf8
$recordSha = (Get-FileHash -Algorithm SHA256 -LiteralPath $dest).Hash.ToLowerInvariant()
"$recordSha  $(Split-Path -Leaf $dest)" | Set-Content -LiteralPath ($dest + '.sha256') -Encoding utf8
Write-Host ''
Write-Host "LOCAL USER OBSERVATIONS RECORDED: $dest"
Write-Host "LOCAL RECORD SHA-256: $recordSha"
Write-Host 'Bu kayıt bir kriptografik imza veya bağımsız Windows GUI testi DEĞİLDİR.'
Write-Host 'All evidence requires independent review. Official-law and release NO-GO remain.'
