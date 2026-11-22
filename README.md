# Embasic Legal

Windows WPF masaüstü uygulamasının güncel geliştirme zinciri bu depodadır.

## CMK öğrenme merkezi

TUR027, TUR026 öğrenme yapısını uygulamanın ana sayfasına ve mevcut **Alternatif Çözümler** menüsüne bağlar. Sol menüdeki **CMK Öğrenme Merkezi** ve 13 kategori de aynı oturumu kullanır: **51 konu, 102 soru**, anlatım, açılır şema, tablo ve kontrol listesi.

- Geliştirme dalı: `tur027/cmk-app-entry-integration`
- Temel: `tur026/cmk-complete-learning-curriculum` (TUR026 Draft PR #27)
- Açılış: `Embasic.Legal.exe` → Ana Sayfa → CMK Öğrenme Merkezini Aç
- Alternatif Çözümler → seri muhakeme / uzlaştırma karşılaştırma dersi; kategori içinden ayrıntılı dersler ve TCK 75 ön ödeme başlığı.

Güncel mevzuat.gov.tr konsolidasyonu **NOT_VERIFIED**; gerçek Windows GUI kabulü **NOT_RUN**. Eğitim puanı hukukî uygunluk veya yetki onayı değildir. Ana dal/üretim yayını yapılmaz.

Windows kaynak derlemesi:

```powershell
dotnet restore Embasic.Legal.slnx
dotnet build Embasic.Legal.slnx -c Release --no-restore
dotnet test tests/Embasic.Legal.Tests/Embasic.Legal.Tests.csproj -c Release --no-restore
```
