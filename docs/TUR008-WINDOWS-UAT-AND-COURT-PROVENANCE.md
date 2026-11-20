# TUR008 — Fiziksel Windows UAT güvenlik ve delil protokolü

**Kapsam:** `ftmstr/Embasic-Legal-Build` TUR008 .NET 10/WPF Windows Alpha. Bu protokol fiziksel testin yerine geçmez.

## 1. Gerçek kabul aşamaları
1. GitHub Actions'ın tam TUR008 win-x64 ZIP dosyasını fiziksel Windows 11 bilgisayara çıkar. Bu bir installer değil, kendi içinde .NET runtime içeren Alpha EXE'dir.
2. PowerShell terminalinde aynı klasörde `powershell -ExecutionPolicy Bypass -File .\TUR008-Windows-UAT.ps1` komutunu yalnız güvenilir/orijinal SHA'lı paket için çalıştır. Betik önce EXE_SHA256.txt ve SHA256SUMS.txt içindeki **bütün dosyalar** için SHA kontrolü yapar; uyuşmazlıkta UAT hiç başlatılmaz.
3. Betikte fiziksel Windows'da testleri şahsen gözlemleyeceğini **EVET** ile onayla. Her testte **P=gerçekten gözlenen PASS, F=gerçekten gözlenen FAIL, N=NOT_RUN** gir. Varsayılan veya yanlış giriş N'dir. Otomatik PASS YOK.
4. FAIL için serbest soruşturma içeriği yazma; yalnız UI/MATH/CRASH/ACCESS/OTHER sınıfından birini belirt. `LocalAppData\Embasic\Legal\UAT` içinde JSON gözlem kaydı ve yan SHA-256 özeti oluşur; ağa gönderilmez.
5. JSON bir **kullanıcı beyanıdır**; resmi fiziksel Windows bağımsız laboratuvar kanıtı veya kriptografik imza değildir. PASS olsa bile üretim NO-GO korunur.

## 2. Matematiksel sentetik senaryolar
- Ceza oranında 100 tam gün, 1/6, indirim → **83 tam gün**. “İndirim” seçimini kaldır → eski sonuç anında GEÇERSİZ; yeniden hesapta **116 tam gün**. `Sonucu kopyala` eylemi eski/başarısız hesap için engellenir.
- Hapis/para: 90 gün × 100 TL => matematiksel 9.000 TL. Günlük tutarı 150 TL yapınca 9.000 TL sonucu anında geçersizleşir; yeniden hesapta 13.500 TL gösterilir.
- Suç tarihi 31.05.2024 → tarihsel TCK52/2 20–100 TL, 01.06.2024 → 7499 m9/m40 tarihsel 100–500 TL notu. Lehe kanun/özel hüküm otomatik PASS değildir.
- YDO: başlangıç 1.000 TL, manuel yüzde 10;20 => 1.320 TL (yalnız matematik). Oranlar/yazılan kaynak değişince eski sonuç GEÇERSİZ; eski TL 1.000.000 = 1 TL para birimi örneği. YDO otomatik adli para cezasına uygulanmaz.
- Yanlış sayı, negatif gün, sıfır günlük tutar veya geçersiz tarih **hata gösterir**; eski sonuç geri dönmez.

## 3. Usul sistematikleri
- Tutuklama/Adli Kontrol/Arama ve El Koyma başlıklarının her birinde **Sistematiği** düğmesi.
- Arama/Elkoymada CMK127/3 saatleri için **elbette özgün evrak kullanılmadan** sentetik üç zaman referansı. Evrak veya saat değiştiğinde rapor geçersizleşmeli, eski rapor hukuki sonuç üretmemeli.
- Danıştay 2007/948 yayımlanmış karar kopyası ile mahkeme kaynaklı tam onaylı aslı birbirinden ayrı; 2012/1117 onama ve 2025 YD 2024/629 kararları ikincil safahatta, otomatik norm PASS yok.

## 4. Açık koşullar
- Gerçek fiziksel Windows GUI testi henüz **NOT_RUN**. GitHub CI yalnız derleme/xUnit ve PowerShell syntax/XAML statik kapısıdır.
- Başka soruşturma verisi, kişisel veri veya parola bu kabul sırasında **girilmemeli**.
- Resmi TCK/CMK/Yönetmelik tam metin tarihsel korpusu, Danıştay imzalı asıllar, UYAP Güneş Doğuş-Batış çizelgeleri **NOT_VERIFIED**.
- E-terazi yeniden dağıtım hakları ve GPLv3/MIT lisans çelişkisi **açık**. Main merge, installer, tag, Stable release **yok**.

**Danıştay resmî karar arama portalları:** https://karararama.danistay.gov.tr/ ve https://dosyasorgulama.danistay.gov.tr/ (dosya sorgu güvenlik kontrolüne tabi olabilir). Bu bağlantıların mevcudiyeti belirtilen dosyaların kaynakta elde edildiğini kanıtlamaz.
