# TUR029 — Ceza Muhakemesi Hukuku etkin öğrenme atölyesi

## Amaç ve kaynak sınırı

Mevcut 47 merkez dersini kavramları ayırma, hatırlama, uygulama ve açıklamalı düzeltme yoluyla geliştirmek. 23.11.2026'da kullanıcının resmî CMK DOC adresi ve PDF alternatifi yeniden timeout verdi. İlgili Library kaynak kataloğu da tam bayt/metin doğrulaması içermedi. Bu yüzden içerik özgün **kavramsal eğitim taslağıdır**, tam veya güncel konsolide kanun açıklaması değildir. Kesin süre, tedbir yetkisi, güncel fıkra veya yürürlük doğrulaması uydurulmaz. Tek güncel mevzuat kaynağı mevzuat.gov.tr kalır.

## Öğrenme döngüsü

1. **Anla:** Mevcut kavram açıklaması ve örneğe ek olarak konuya özgü üç öğrenme hedefi ve madde aralığı için okuma rehberi.
2. **Karşılaştır:** 47 konuda iki kavramı üç ortak ölçüte göre karşılaştıran gerçek tablo; farkın neden önemli olduğuna ilişkin açıklama. Toplam 141 karşılaştırma satırı.
3. **Hatırla:** Her konuda üç cevap gizleme kartı. Öğrenci önce kendi cevabını düşünür, cevabı açar, Hatırladım veya Tekrar çalışacağım şeklinde özdeğerlendirme yapar. Toplam 141 kart. Özdeğerlendirme öğrenme veya hukukî uygunluk belgesi değildir.
4. **Uygula:** Dört şema adımı seçilebilir; seçilen adımın açıklaması ve vurgu değişir. Ardından konuya özgü senaryoda üç yoldan biri seçilir ve o seçeneğe özgü gerekçe gösterilir. Toplam 47 dallanan senaryo.
5. **Düzelt:** Merkez derslerindeki 94 soru, şemadakinden farklı 47 aktarım senaryosu ve 47 kavram eşleştirme sorusundan oluşur; cevap konumları tek sütunda sabit değildir. Yanlış yanıt sonrasında yeniden denemeye izin verilir. İlk yanıt puanı ayrı tutulur; geri bildirim sonrası doğru yanıt ayrı sayılır. Aynı doğru cevabı tekrar göndermek puan şişirmez.

Örnek ayrımlar: soruşturma/kovuşturma; görev/yetki; arama/el koyma; teknik bütünlük/hukukî dayanak; iade/beraat; delil güvenilirliği/hukuka uygun elde edilme; yeni belge/yenileme sebebi. Yanlış seçenekler konuyla ilgili makul yanılgılardır; ilgisiz nesne veya ekran rengi seçenekleri yeni atölyede kullanılmaz.

## Mevcut uygulama ile ilişki

Ceza Muhakemesi Hukuku menüsündeki 47 konu yeni atölyeyi kullanır. Ayrı Alternatif Çözümler üç dersi ve kendi eski arayüzü korunur; toplam görünür katalog 50 konu/100 soru kalır. Dersler, kartlar, senaryo seçimleri ve eğitim işaretleri yalnız RAM'dedir. Konu değişiminde kart özdeğerlendirmesi ve dal seçimi korunur. Oturumu Sıfırla hepsini temizler. Mevcut işlem sistematiği sekmesi, aynı araç nesneleri ve kendi veri sıfırlama/çıkış güvenlik kapıları değişmez.

`CmkStudyCatalog` içerik ve doğru/yanlış geribildirimini, `CmkStudySession` RAM kart/dal durumunu tutar. `CmkRecallCardViewModel` kart etkileşimini yönetir. Yeni paket, dış model veya ağ çağrısı eklenmez. Eğitim motoru hiçbir cevapta `LegalComplianceCertified=true` üretmez.

## Doğrulama

Yeni xUnit kapsamı: 47/47 sahiplik; 141 kart/karşılaştırma satırı; 94 soru ve her seçeneğe özgü açıklama; her soruda tek doğru; cevap konumlarının dengesi; cevap açmadan özdeğerlendirme reddi; bağımsız konu durumu; tekrar işaretleme ve tüm oturum sıfırlama; dal değiştirme; aralık dışı parametreler; alternatif kapsamını bozmama; temel kavram yanılgılarının doğru cevap olarak sunulmaması.

Windows STA yürütücüsüne gerçek uygulama komutları üzerinden adım seçme/ilerletme, kart açma/özdeğerlendirme, yanlış dalın açıklaması, yanlış sonrası doğru yanıt, puan şişirmeme ve konu dönüşünde durum koruma eklendi. Bunlar fiziksel Windows GUI, fare/klavye, ekran okuyucu veya DPI kabulü değildir. UAT kolektöründe yedi yeni manuel vaka vardır. Fiziksel GUI NOT_RUN; hukuk ve production/stable NO-GO kapıları korunur. Main merge, tag, release veya installer yoktur.

Bağımsız kod/içerik incelemesindeki üç bulgu düzeltildi: etkili savunma karşılaştırmasının üçüncü hücresi, şema sorusunun testte aynen tekrar edilmesi ve eski dört satır şartını taşıyan UAT maddesi. Testin ilk sorusu artık her konuda ayrı yazılmış yeni bir olaya ilişkindir; ikinci soru bilinçli olarak kavram eşleştirmeyi ölçer. 94 sorunun tamamı farklı vaka sorusu olarak nitelendirilmez.
