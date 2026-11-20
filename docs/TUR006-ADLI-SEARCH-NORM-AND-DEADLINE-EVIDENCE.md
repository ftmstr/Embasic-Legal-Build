# TUR006 — Adlî Arama/Elkoyma Kaynak Kapısı ve Belgeli Süre Kontrolü

Tarih: 20 Kasım 2026. Hedef: `ftmstr/Embasic-Legal-Build`, stacked Draft TUR006.

## A. Kaynak hiyerarşisi ve tarihsel norm ayrımı

**Birincil kurum kaynağı olarak WEB üzerinden incelenebilen ancak EXE'ye bütünlüklü korpus olarak aktarılmayan kaynaklar:**
- Adalet Bakanlığı CİGM Adlî ve Önleme Aramaları Yönetmeliği PDF: https://cigm.adalet.gov.tr/Resimler/SayfaDokuman/1222021100307Adli%20ve%20%C3%96nleme%20Aramalar%C4%B1%20Y%C3%B6netmeli%C4%9Fi.pdf
- Adalet Bakanlığı İNHAK Yönetmelik PDF: https://inhak.adalet.gov.tr/Resimler/Dokuman/12122019120126um_adliveonlemearamalari.pdf
- UYAP CMK güncel sayfa aday kaydı: https://mevzuat.adalet.gov.tr/mevzuat/102946

CİGM PDF'nin ilk sayfası web PDF görüntüsüyle de denetlendi; 2005 kanun atıflarından sonra 2016 güncelleme dipnotları mevcut. İNHAK PDF daha eski metin unsurları taşıyor (4926 sayılı Kanun atfı vb.). Her iki PDF'nin kurum kökenli WEB görüntüsü, olay tarihindeki **tam, değişiklikleri işlenmiş, dağıtımı lisanslı resmî korpus** statüsünü tek başına ispatlamaz. Ham eksiksiz PDF içeriği uygulamanın kaynak sürüm sistemine indirildi, tüm maddeler ve sayfa konumları eşleştirildi veya SHA manifesti oluşturuldu **DENİLEMEZ**. Bu tur yalnız atıf ve kaynak kaydı geliştirdi.

**İkincil/dipnotlu konsolidasyon:**
https://www.lexpera.com.tr/mevzuat/yonetmelikler/adli-ve-onleme-aramalari-yonetmeligi-1
Başlıklandırma: Yönetmelik m.5–17 **Adlî Arama**, m.18–26 **Önleme Aramaları**, m.27–32 **Aramaların Yapılma Şekli**. Daha önce önleme m.18–27 şeklindeki gevşek sınıflandırma düzeltilmiştir.

## B. İptal/YD risk defteri

- **R08A** — m.8/1(a)'daki yerleşim yeri/konut/işyeri/araç vb. kararsız arama genişletmesi; Danıştay 10. Daire 13.03.2007 E.2005/6392 K.2007/948 iptal; İDDK 14.09.2012 E.2007/2257 K.2012/1117 onama bilgisi Bakanlık PDF dipnotlarında mevcut. Asıl kararın tam metin/safahat kabulü ayrıca gerekli.
- **R08F** — m.8/1(f) “ilgilinin rızası ile” ibaresi için aynı iptal/onama dipnotu. **Salt rıza**, bağımsız kanuni adlî arama yetkisi şeklinde otomatik yorumlanmaz.
- **R30** — Yönetmelik 30/1 için aynı tarihli iptal/onama dipnotu; icra hükümlerini adlî bölümün m.5–17 maddeleriyle karıştırmamak gerek.
- **R17** — Yönetmelik m.17/1 hakkında Danıştay İDDK **23.01.2025, YD İtiraz 2024/629** bilgisi LEXPERA konsolidasyonunda bulunuyor. Birincil Danıştay kararın tam metni, devam/safahatı, somut döneme etkinliği **doğrulanmadı**. Bilgisayar araması için CMK 134'ün kendine özgü şartları ayrıca değerlendirilmeli.
- **RDATE** — Olay tarihli CMK, yönetmelik, iptal ve YD, 7571 ve geçiş etkileri insan incelemesi olmaksızın otomatik belirlenemez.

Kod: `src/Embasic.Legal.Core/AdliSearchNormGate.cs` ve `SystematicsPage.cs`. Risklerin `CertifiedPrimaryDecision` ve `EffectiveOnCaseDateProved` çıktıları **false**. Kullanıcının yalnız olay tarihi girmesi bunları PASS yapmaz.

## C. CMK 127/3: 24/48 saat delil-zaman denetimi

CMK 127/3'te **hâkim kararı bulunmaksızın elkoymanın 24 saat içinde hâkim onayına sunulması**, **hâkimin kararının elkoymadan itibaren 48 saat içinde açıklanması** ayrı başlangıç ve sürelerdir. Uygulama `SeizureClockAudit` ile üç belge zaman damgasını açık ofsetli `yyyy-MM-ddTHH:mm+03:00` biçiminde alır; **elapsed instant** karşılaştırması yapar. Sonuçlar: MissingEvidence, ChronologyConflict, WithinReferenceWindow, ExceedsReferenceWindow. İki süreyi birleştirmez veya 48 saat hesabını sunum anından başlatmaz. Özellikle tarihi yaz saati/UTC ofseti hatalı girilirse hesap reddedilmez ama bu OFSETİN evrak üzerinde doğruluğu ayrıca insan tarafından denetlenmelidir.

**Bu mekanik karşılaştırma kanun yürürlüğü, istisnalar, tarih/tebliğ kanıtı veya elkoymanın kendiliğinden kalkması hakkında bağlayıcı karar VERMEZ.** Sadece gerçek dosyada evrak eksikliği ve süre aşımı iddiasını incelemeye yarar.

## D. Tutuklama/adli kontrol ve dosya mahremiyeti

Önceki TUR005 12 + 12 + 27 = **51** sistematik sorusu, bölüm-içi düğmeler, öğretici şema ve dosya üzerinde insan denetimi korunur. Yalnız kimliksiz belge referansları; dosya içeriği/kişisel veri kullanılmamalı. İşaretleme RAM'de, kopyalama ancak kullanıcının panoya onayıyla. Kapatmada veri kaybı uyarısı.

## E. UYAP güneş çizelgesi ve hukuk verisi

UYAP Güneş Doğuş-Batış hizmetinin yayımlandığı:
https://www.uyap.gov.tr/Hizmetler
Kurum bağlantısı:
https://bigm-uyap.adalet.gov.tr/portal/zaman/zaman.html

Asıl çizelge baytları, il/yıl/saat standardı/yeniden dağıtım hakları **alınmadı**. Gece Vakti ekranı **fail-closed** kalır.

## F. Release sınırları

Fiziksel Windows UAT **NOT_RUN**; son testler otomatik GitHub Windows CI'dır. Devreden şifreli sentetik not ve kullanıcı onaylı eski not göçünde kaynak/geri alma korumaları korunur. Gerçek soruşturma/kasa gizlilik sertifikası veya lisans kabulü yoktur. GitHub LICENSE GPL-3.0; Master Prompt MIT tercihi ve E-terazi eser hakları hâlâ çözümlenmedi. **MAIN MERGE / TAG / INSTALLER / STABLE RELEASE YOK.**
