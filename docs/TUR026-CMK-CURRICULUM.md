# TUR026 — CMK öğrenme merkezi

13 doğrudan sol menü kategorisi, 51 konu, 102 soru, konu anlatımı ve açılıp kapanan 4 adımlı şemalar. TUR024/TUR025 içeriği korunmuştur; 38 yeni konu eklenmiştir. Dersler özgün kavramsal eğitim metinleridir. Numaralı CMK 1–335 ana aralıkları okuma haritasında yer alır; 38/A, 128/A ve 308/A bağlantıları ve ek/geçici hükümler için ayrı alan vardır. Bu kapsam bir madde/fıkra tamlık veya güncel yürürlük onayı değildir.

| Kategori | Konu sayısı | Okuma referansı |
|---|---:|---|
| Temel kavramlar ve yargı teşkilatı | 3 | 1–32 |
| Kararlar, tebligat ve süreler | 2 | 33–42, 38/A |
| Tanıklık, bilirkişilik ve adlî incelemeler | 6 | 43–89 |
| Yakalama, tutuklama ve adlî kontrol | 3 | 90–115 |
| Arama, el koyma ve dijital delil | 5 | 116–134, 128/A |
| Gizli tedbirler ve tazminat | 3 | 135–144 |
| İfade, sorgu ve savunma | 2 | 145–156 |
| Soruşturma ve kamu davasının açılması | 2 | 157–174 |
| Kovuşturma, duruşma ve hüküm | 7 | 175–232 |
| Mağdur hakları ve katılma | 2 | 233–243 |
| Özel usuller ve alternatif çözümler | 7 | 244–259; bağlantılı TCK 75 |
| İtiraz, istinaf, temyiz ve olağanüstü yollar | 6 | 260–323, 308/A |
| Giderler, son ve geçici hükümler | 3 | 324–335; ek/geçici hükümler |

## Kullanım

Sol menüde CMK Öğrenme Merkezi altındaki bir kategoriye tıklayın. Konuyu seçin; Anlatım, Şema, Tablo, Kontrol listesi, Kısa test ve Kaynak sekmelerini kullanın. Şema kartına tıklamak ayrıntıyı açar. Kontrol işaretleri ve soru puanları yalnız uygulama oturumunda tutulur. Kategori değişikliği ilerlemeyi silmez; açık Sıfırla işlemi bütün konuları temizler. Payda kategoriye göre değişmez: toplam 102 soru.

## Kaynak edinme sınırı

Kullanıcının verdiği **https://www.mevzuat.gov.tr/MevzuatMetin/1.5.5271.doc** okunamadı. 22.11.2026 doğrudan web açma timeout, curl zaman aşımı, tarayıcı `net::ERR_BLOCKED_BY_CLIENT`; resmî PDF ve mevzuat sorgu sayfası da timeout. Kaynak baytları, SHA-256, sürüm ve yürürlük **NOT_VERIFIED**. Bu değişiklik belgenin güncel tam metninin incelendiği iddiasını taşımaz. Başlıklar ve kavram açıklamaları eğitim taslağıdır; süre veya tedbir yetkisi hesaplanmaz. UYAP arama sonuçları yapısal çapraz kontrol sağlar; güncel mevzuat.gov.tr konsolidasyonunun ikamesi değildir. Önceki hukuk ve üretim kapıları korunur.

## Mimari ve doğrulama

`CmkCurriculum` kategori, ders ve konu ilişkisini tek yerde tutar. `CmkLearningCatalog.All` geriye uyum için önceki 13 nesneyi korur; `Find/Assess` yeni konuları da çözer. WPF tek ortak VM ve RAM oturumunu paylaşır; kalıcı veya dış sisteme veri yazmaz. Yeni bağımlılık yoktur.

5 odaklı xUnit testi: kategori sahipliği/benzersizlik/eski nesne kimliği; 1–335 okuma aralıkları ve ek madde referansları; 51 ders ve 102 sorunun cevap/geribildirim yapısı; kategoriler arası işaret koruma ve sıfırlama; kaynak başarısızlığının onaya dönüşmemesi. XML parse statik kontrolü fiziksel WPF kabulü değildir. `TUR026-Windows-UAT.ps1` yeni menü, kategori, anlatım, açılır şema, test, kaynak panosu, ilerleme ve DPI vakalarını içerir. Gerçek Windows GUI kabulü **NOT_RUN**.

Windows CI sonuçları PR ve teslim raporunda ayrıca kaydedilecektir. `main` merge, tag, release, installer veya üretim yayını yapılmaz.
