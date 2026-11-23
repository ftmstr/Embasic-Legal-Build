# TUR030 — Ceza Muhakemesi Hukuku: Tekrar ve Eksik Konular

## Neden?
TUR029 / stacked Draft PR #30 üzerinde 47 merkezî ders, 94 ayrı senaryo sorusu ve 141 hatırlama kartı var. Ancak kullanıcı konular arasında hareket ettikçe hangi dersin ilk denemesinde hata yaptığını, hangi kartlara 'Tekrar çalışacağım' dediğini ve hangi etkinlikleri yarıda bıraktığını tek yerden göremiyor. Bu tur aynı konuları, testleri, resmî hukuk kaynaklarını veya işlem araçlarını yeniden oluşturmadan **tek tekrarsız eğitim deneyimi eksiğini** giderir.

## Uygulama tasarımı
Yeni **5 · Tekrar** sekmesi (resmî kaynak sekmesi **6 · Kaynak**):
- Başlanan derslerde ilk denemede yanlış yanıtlanan sorular (sonradan doğru yapılmışsa **ayrıca** gösterilir; ilk deneme geçmişi puan şişirmek amacıyla değiştirilmez).
- Özdeğerlendirmesi **Tekrar çalışacağım** olan kartlar.
- Başlanıp tamamlanmayan quiz/inceleme işaretleri.
- **Hiç başlanmamış dersler başarısız sayılmaz**, 'Hatırladım' özdeğerlendirmesi resmî öğrenme başarısı veya mevzuat onayı sayılmaz.
- En önce ilk yanlış yanıtlar, ardından tekrar kartları ve kalan sorular; her satırdan **Derse dön ve tekrar çalış** ile var olan dersin kendisine gidilir.
- Filtreleme ve özet yalnız RAM'deki mevcut `_answered/_correct/_recovered`, `CmkStudySession`, `CmkLearningChecklistSession` üzerinden; yeni dosya, bulut, vaka alanı, ağ veya UYAP gönderimi yok.
- **Alternatif Çözümler** ayrı kalır, yanlışlıkla 47 derslik merkezî tekrar gündemine girmez.
- Şema, karşılaştırma tablosu, 47 ders, 141 kart, 47 rehberli senaryo, 94 quiz, üç işlem sistematiği, tarih/gece/para/YDO ve AES-GCM notlar aynen korunur.

## Test planı
12 yeni xUnit test: boş oturum; ilk yanlış; sonradan doğru; tekrar kartı; Hatırladım'ın başarısızlık/sertifika yaratmaması; kısmi kontrol; tüm doğru tamamlanmış ders; öncelik sırası; reset; Alternatif Çözümler ayrımı; yinelenen anahtar/aralık; veriye yan etkisizlik. Gerçek Windows Windows-2022 CI ve PowerShell manual UAT için tema/klavye/konuya dönüş ve reset senaryoları; fiziksel GUI **NOT_RUN**.

## Hukukî/üretim sınırı
**Güncel/meri/konsolide mevzuat yalnız `mevzuat.gov.tr`**; bu tur resmî CMK/TCK kanun baytlarını getirmiyor veya değiştirmiyor. Kaynak sürüm, yürürlük, madde/fıkra **NOT_VERIFIED**; tarihsel TBMM ilk kabul metni güncel norm yerine geçmez. AYM/Danıştay tam resmî karar asılları **NOT_VERIFIED**. Review gündemi, işaretler ve quiz sonucu hukukî uygunluk, zorlayıcı tedbir kararı veya öğrenme sertifikası değildir. Fiziksel Windows UAT, hukuk, lisans/E-terazi dağıtım ve production **NO-GO**. `main` merge, tag, installer, release yok.
