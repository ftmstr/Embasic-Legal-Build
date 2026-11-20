# Embasic Legal TUR008 — manual, offline, non-sensitive Windows UAT evidence collector.
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
# Verify every packaged file against the GitHub CI SHA manifest before any human acceptance.
$allSums = Join-Path (Split-Path -Parent $exe) 'SHA256SUMS.txt'
if (-not (Test-Path -LiteralPath $allSums -PathType Leaf)) { throw 'SHA256SUMS.txt not found; UAT blocked.' }
$verified = 0
foreach ($line in (Get-Content -LiteralPath $allSums)) {
    if ($line -notmatch '^\s*([a-fA-F0-9]{64})\s{2}(.+?)\s*
Write-Host 'No automated GUI/PDF acceptance is claimed; respond only from real observations.'
Write-Host 'Do NOT input actual investigation notes, personal data, or passwords in this script.'

$approval = (Read-Host 'Bu testleri gercek Windows bilgisayarinda BIZZAT gözlemleyecekseniz EVET yazin').Trim().ToUpperInvariant()
if ($approval -ne 'EVET') {
    Write-Host 'UAT başlatılmadı; hiçbir gözlem PASS sayılmadı.'
    return
}

$cases = @(
    'TUR008: Ceza oranında gün, pay, payda veya indirim seçimi değiştiğinde eski sonuç derhal GEÇERSİZ',
    'TUR008: Günlük TL ve suç tarihi değişince Hapis / para sonucu ve tarihsel 7499 notu eski PASS kalmaz',
    'TUR008: YDO oranı, kaynak veya eski TL değişince sonucun eski hali kopyalanamaz',
    'TUR008: Yanlış ceza oranı ve geçersiz tarih girilince önceki hesap sonucu kopyalanamaz',
    'TUR008: CMK127/3 evrak referansı veya zaman değiştiğinde önceki 24/48 saat hesabı GEÇERSİZ',
    'TUR008: Danıştay 2007 kararının dergide yayımlı örneği ile asıl mahkeme kararı statüsü ayrı görünür',

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
        $note = (Read-Host 'Yalnız hata kategorisi girin: UI, MATH, CRASH, ACCESS, OTHER').Trim().ToUpperInvariant()
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
    application = 'Embasic Legal Windows TUR008 Alpha'
    time_local = (Get-Date).ToString('o')
    os_version = [Environment]::OSVersion.VersionString
    exesha256 = $actualSha
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
$dest = Join-Path $location ('TUR008-UAT-' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '.json')
$proof | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $dest -Encoding utf8
$recordSha = (Get-FileHash -Algorithm SHA256 -LiteralPath $dest).Hash.ToLowerInvariant()
"$recordSha  $(Split-Path -Leaf $dest)" | Set-Content -LiteralPath ($dest + '.sha256') -Encoding utf8
Write-Host ''
Write-Host "LOCAL USER OBSERVATIONS RECORDED: $dest"
Write-Host "LOCAL RECORD SHA-256: $recordSha"
Write-Host 'This is NOT a signed or independently authenticated physical Windows test.'
Write-Host 'All evidence requires independent review. Official-law and release NO-GO remain.'
) { throw 'Invalid SHA manifest line.' }
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
if ($verified -lt 5) { throw 'Incomplete hash manifest; UAT not authorized.' }
Write-Host "EXE SHA256 verified: $actualSha"
Write-Host 'No automated GUI/PDF acceptance is claimed; respond only from real observations.'
Write-Host 'Do NOT input actual investigation notes, personal data, or passwords in this script.'
$cases = @(
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
    Write-Host ''
    Write-Host $case
    $answer = (Read-Host 'P=observed PASS; F=observed FAIL; N=NOT_RUN (default)').Trim().ToUpperInvariant()
    if ($answer -notin @('P','F','N')) { $answer = 'N' }
    $note = ''
    if ($answer -eq 'F') {
        $note = Read-Host 'Short non-sensitive failure note (no case facts)'
    }
    $results += [pscustomobject]@{
        check = $case
        status = @{P='PASS';F='FAIL';N='NOT_RUN'}[$answer]
        observation = $note
    }
}
$proof = [ordered]@{
    application = 'Embasic Legal Windows TUR008 Alpha'
    time_local = (Get-Date).ToString('o')
    os_version = [Environment]::OSVersion.VersionString
    exesha256 = $actualSha
    simulated_tests = $false
    physical_windows_acceptance = if (($results | Where-Object {$_.status -ne 'PASS'}).Count -eq 0) {'USER_REPORTED_ALL_PASS_PENDING_INDEPENDENT_REVIEW'} else {'INCOMPLETE_OR_FAIL'}
    legal_production = 'NO-GO'
    results = $results
}
$location = Join-Path $env:LOCALAPPDATA 'Embasic\Legal\UAT'
New-Item -ItemType Directory -Force -Path $location | Out-Null
$dest = Join-Path $location ('TUR008-UAT-' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '.json')
$proof | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $dest -Encoding utf8
Write-Host ''
Write-Host "LOCAL USER OBSERVATIONS RECORDED: $dest"
Write-Host 'All evidence requires independent review. Official-law and release NO-GO remain.'
