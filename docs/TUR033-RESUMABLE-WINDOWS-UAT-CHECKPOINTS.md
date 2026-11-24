# TUR033 — 158 maddelik Windows UAT için kesintiye dayanıklı kontrol noktaları

## Kanıtlı sorun ve hedef

Önceki TUR032 Windows kabul aracı, 158 soruyu tek \`foreach\` döngüsünde kullanıcıya soruyor; \`$results\` dizisini yalnız **tüm soruların sonunda** diske yazıyordu. Kullanıcı \`Ctrl+C\`, oturum kapanması veya güç kaybı yaşarsa tamamladığı her gözlem kaybolabilirdi. Kaynak kodu bu davranışı doğrudan gösteriyor. Bu tur gerçek Windows PC'ye erişim iddia etmez; 158 maddelik kabulün uygulanabilirliğini ve sonuçların kazara kaybolmamasını iyileştirir.

## Tek iyileştirme

* \`tools/TUR033-Windows-UAT.ps1\`: **58 değil 158** soruluk kaynak liste korunur, varsayılan \`-BatchSize 10\` ile bir oturumda en fazla 10 soru sorulur (\`-BatchSize 158\` mümkün).
* Kullanıcı başta gerçek Windows oturumu, özgün EXE/SHA256SUMS, GitHub exact HEAD ve manuel gözlem onayını **her başlatmada** yine doğrular. Kaynak hash ve kullanıcı onayı atlanmaz.
* Açık onaydan sonra \`%LOCALAPPDATA%\Embasic\Legal\UAT\TUR033-UAT-...\` altında yalnız **kullanıcı cevapları** (PASS, FAIL, NOT_RUN; FAIL ise hata kategorisi) ve mevcut sentetik kontrol metinleri tutulur. Kullanıcı gerçek soruşturma/mağdur/şüpheli bilgisi girmemelidir.
* İlk kaynak doğrulamasından sonra adım 000, her soru yanıtlandığında 001..158 numaralı **değiştirilmeyen, önceki kayıtları silmeyen** tam JSON dosyası kaydedilir. Her dosya adı, kendi içeriğinin SHA-256 değerini taşır; önceki tamamlanan kontrol noktaları korunur. Dosyalar aynı klasörde geçici dosyadan yeniden adlandırılır.
* \`-ResumeFrom "...\TUR033-UAT-..."\` ile **aynı doğrulanmış GitHub HEAD ve EXE hash'i** üzerinde kaldığı yerden devam edilir. Tüm checkpoint sırası, dosya SHA'ları, senaryo metinleri/ID'leri, yanıt enum değerleri ve önceki kaynak kimliği tek tek doğrulanır. Uyuşmazlık halinde sessiz düzeltme/silme/otomatik PASS yok; fail-closed.
* Yanıtlanmayanlar **henüz gözlenmiş sayılmaz**. Son dosya yalnız kullanıcı beyanıdır, \`physical_windows_independently_verified=false\`, \`USER_SELF_REPORTED_NOT_INDEPENDENTLY_VERIFIED\`, hukuk/lisans/production \`NO-GO\` değişmez. SHA-256 hilesiz kaynak/bağımsız dijital imza ya da fail-safe anti-tamper sağlamaz; yalnız kazara bozulma denetimidir.
* Eski 47 CMK dersi/94 soru/141 kart, üç sistematik, tek Gece Vakti/iki tarih, para/YDO, E-terazi, iki tema ve AES-GCM kasa kodunda hiçbir değişiklik yapılmaz.

## Fiziksel Windows kullanıcı rehberi

1. GitHub **TUR033** exact-final Push Actions özgün ZIP'ini indirin; ZIP içindeki \`Embasic.Legal.exe\`, \`EXE_SHA256.txt\`, \`SHA256SUMS.txt\`, \`BUILD_PROVENANCE.json\`, \`TUR033-Windows-UAT.ps1\` dosyalarını birlikte çıkarın. EXE hash'i ayrı PowerShell \`Get-FileHash\` ile kontrol edilebilir.
2. Bu Alpha yazılım yalnız kimliksiz/sentetik veriyle denenmelidir. PowerShell'i ZIP'ten çıkarılan klasörde açın; betiği izin verilen normal kurumsal yürütme ilkeleri altında \`.\TUR033-Windows-UAT.ps1\` olarak çalıştırın. Sırf bu test için koruma kapılarını devre dışı bırakmayın.
3. GitHub Draft PR'dan SHA-40 değeri bağımsız olarak kontrol edilir; EVET onayı sonrası 10 gözlem sorusu gelir. \`P\` yalnız bizzat görülen PASS, \`F\` FAIL (hata kategorisi), \`N\` NOT_RUN (varsayılan).
4. Oturum sonunda gösterilen **SESSION FOLDER** yolunu güvenli şekilde saklayın. Sonraki oturum: \`.\TUR033-Windows-UAT.ps1 -ResumeFrom "C:\Users\...\AppData\Local\Embasic\Legal\UAT\TUR033-UAT-..." -BatchSize 10\` (gerçek ekrandaki klasörü kullanın). Betik aynı EXE/GitHub SHA'yı ve checkpointleri doğrulamadan devam etmez.
5. 900 DIP ve %125/%150 ölçekleme, Tab/Enter, Windows Narrator, ayrı pencere, Tutuklama/Adli Kontrol/Arama-El Koyma ile başlanmalıdır. 158 sorunun yanıtlanması bile **bağımsız fiziksel onay, kanun güncelliği, hukukî yetki veya üretim izni değildir**.
6. Son JSON'u yalnız adlî/kişisel verisiz gözlem kaydı olarak değerlendirin; kayıtlar kullanıcının yerel bilgisayarında kalır, otomatik Drive/GitHub upload yok.

## Açık kaynak / güvenlik kapıları

Güncel/meri/konsolide CMK/TCK ve diğer mevzuat **yalnız mevzuat.gov.tr** üzerinden kabul edilebilir; 24.11.2026 canlı girişim site timeout verdiğinden CMK/TCK tam güncel bayt/yürürlük/fıkra **NOT_VERIFIED**. AYM E.2023/128 K.2026/36 için resmî web araması karar kaydını doğrulasa da tam karar sureti/hüküm metni bu turda okunmadı; Danıştay resmî arama ekranı erişilebilir ancak ilgili karar asılları/safahat NOT_VERIFIED. Lisans/E-terazi dağıtımı ve stable/production NO-GO; fiziksel Windows 11 gerçek gözlemi NOT_RUN; main merge/tag/installer/release yapılmaz.
