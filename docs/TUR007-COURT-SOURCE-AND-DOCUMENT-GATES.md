# TUR007 — Danıştay Karar Kaynakları ve CMK 127/3 Evrak Güvenliği

Tarih: 20.11.2026. TUR006 stacked Draft PR #6 exact HEAD b6e9eb68e7f327d88243eb147a7b56169d554a57 esas alınmıştır. Resmî karar/korpus onayı ya da fiziksel Windows kullanıcı kabulü YOKTUR.

## 1. Danıştay kaynaklarının hukuki kanıt düzeyi

| Karar | Erişilen kaynak | Düzey | Eksik |
| --- | --- | --- | --- |
| 10. Daire, 13.03.2007, E.2005/6392 K.2007/948 | İstanbul Barosu Dergisi 2007 C.81 S.5 basılı s.2417–2428 yayımlanmış karar METNİ | PublishedDecisionReproduction; resmî Danıştay sureti DEĞİL | Asıl imzalı/onaylı suret ve olay tarihli norm |
| İDDK, 14.09.2012, E.2007/2257 K.2012/1117 | Baro duyurusu ile Bakanlık mevzuat dipnotu onama bilgisi | SecondarySummary | Asıl karar tam metni ve sonraki safahat |
| İDDK, 23.01.2025, YD İtiraz No 2024/629 | LEXPERA konsolide m.17/1 YD dipnotu | SecondarySummary | Asıl karar, yürütmenin durdurulmasının sonraki durumu, olay-tarihli etki |

**2007 sonuç ayrımı:** m.8/1(a)'nın belirli konut/işyeri/eklenti/araç araması ibaresi, m.8/1(f) ilgilinin rızası ibaresi ve m.30/1 yönünden **iptal**; m.8/1(c) ve m.27 iptal istemleri **ret**. Yönetmeliğin 8. maddesinin bütünü ya da m.27 iptal edilmiş gibi sunulamaz. İptalin 2012 farklı gerekçeli onaması hakkında ikinci kaynak kanıtı, Danıştay imzalı tam kararın yerine geçmez.

Bağlantılar:
- Yayımlanmış D2007 karar metni: https://www.istanbulbarosu.org.tr/files/Yayinlar/Dergi/doc/ibd20075.pdf
- D2012 onama duyurusu: https://istanbulbarosu.org.tr/haber/danistay-iddk-adli-arama-konusunda-adalet-bakanliginin-yonetmelik-duzenleme-yetkisi-bulunmamaktadir
- Bakanlık CİGM kaynak PDF: https://cigm.adalet.gov.tr/Resimler/SayfaDokuman/1222021100307Adli%20ve%20%C3%96nleme%20Aramalar%C4%B1%20Y%C3%B6netmeli%C4%9Fi.pdf
- D2025 YD ikincil kayıt: https://www.lexpera.com.tr/mevzuat/yonetmelikler/adli-ve-onleme-aramalari-yonetmeligi-1
- CMK mevzuat UYAP sayfası: https://mevzuat.adalet.gov.tr/mevzuat/102946

PDF araştırması sırasında Baro dergisi kararın tam metni web indeksiyle açıldı; web PDF ekran görüntüsü cache-miss olduğundan bağımsız görsel sayfa incelemesi tamamlanmadı. Özgün kaynak PDF baytları, SHA ve Danıştay kurumundan imzalı suret edinilmedi; resmî karar arşivi veya tam hukuk metin korpusu PASS ilan edilmez.

## 2. Olay tarihli norm güven kapısı

CourtDecisionEvidenceRegister kaynak türünü ve açıklanan hükmün kapsamını ayrı tutar. İlgili koddaki OfficialDecisionOriginalVerified, EffectiveOnCaseDateVerified, LegalEffectCertified değerleri false'tur; yalnız olay tarihi girilerek değişmez.

Yönetmelik tasnifi: 5–17 adlî arama, 18–26 önleme araması, 27–32 aramanın yapılma şekli. m.30/1 iptal kaydı ortak icra bölümüne aittir. CMK 134 dijital tedbiri, Yönetmelik m.17/1 yürütmenin durdurulması kaydıyla otomatik ve normlar hiyerarşisi gözetilmeksizin eşleştirilmez. Asıl karar ve 7571 geçiş hükümleri ayrıca incelenir.

## 3. Elkoyma belge-tarih güvenliği

CMK 127/3 için elkoyma-sunma 24 saat ve elkoyma-hâkim kararı 48 saat yalnız mekanik referanslardır. Her tarih-saat için kimliksiz evrak referansı seçeneği eklendi. SeizureDocumentGate belgeye **atıf girilip girilmediğini** inceler; atıf evrakın sahihliği değildir. İlgili üç belgenin saati, sırası veya referansı eksik/çelişkili ise uyarı verilir, hukuki PASS üretilmez.

**TUR007 P0 düzeltme:** Kullanıcı olay tarihini değiştirince önceki norm risk değerlendirmesi geçersiz gösterilir. Elkoyma, sunma veya karar saati ya da bunların evrak referansları değiştirilince önceki 24/48 aritmetik sonucu hemen geçersizleşir. Yeniden karşılaştırılmadan eski sonucun panoya raporlanması önlenir. Referanslar RAM'de kalır ve otomatik Drive/UYAP/SQLite kaydı yoktur; kullanıcıya gerçek soruşturma verisi girmemesi hatırlatılır.

## 4. Test, kabul ve lisans

TUR006 41 xUnit test korunmuştur; TUR007 dört odaklı test ekler: 2007 iptal/ret ayrımı; 2012/2025 ikincil kanıt düzeyi; evrak referans eksiklikleri; ters zaman sırası veya eksik tarih. Önceki şifreli not ve açık onaylı göç mekanizması korunur. Kodda mevcut E-terazi, iki tema, iki tarih aracı, tek fail-closed Gece Vakti, üç ayrı Sistematiği düğmesi, 12+12+27 dosya kontrolü değişmeden kalır.

Fiziksel Windows gözlem kaydı belirtilen Drive /Uygulama ve /Rapor klasörlerinde bulunmadı: **PHYSICAL WINDOWS UAT NOT_RUN**. TUR007-Windows-UAT.ps1, EXE hash doğrulayıp yalnız gerçek insan gözlemlerini kaydetmek için pakete eklenir.

GPL-3.0 repo LICENSE ile Master Prompt MIT tercihi çelişkisi açık; E-terazi yeniden dağıtım hakkı, transitive NOTICE, eksiksiz TCK/CMK/Yönetmelik kaynak baytları, resmî UYAP Güneş Doğuş–Batış verisi ve gerçek soruşturma verisi gizliliği hâlâ NO-GO. Main merge, tag, installer, Stable release yapılmaz.
