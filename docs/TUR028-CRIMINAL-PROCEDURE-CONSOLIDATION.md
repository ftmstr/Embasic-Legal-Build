# TUR028 — Ceza Muhakemesi Hukuku menü ve konu birleştirmesi

Kullanıcı talebi: Alternatif Çözümler hariç CMK bölümlerini merkez içinde topla, tekrarları birleştir ve başlığı Ceza Muhakemesi Hukuku yap.

- Sol menüde **Ceza Muhakemesi Hukuku** tek açılır üst başlıktır. 13 kategori onun altındadır. Ana sayfa kartı ve pencere başlığı aynı adı kullanır.
- Önceki ayrı Tutuklama, Adli Kontrol ve Arama/El Koyma üst menüleri kaldırılmıştır. İlgili derslerin **İşlem sistematiği** sekmesi mevcut aynı araç nesnelerini gösterir. 7 arama/el koyma bağlantısı dahil işlem nesneleri çoğaltılmaz; işaretler, dosya/evrak referansları, norm/saat kapıları ve çıkışta geçici veri uyarısı korunur.
- **Alternatif Çözümler** ayrı üst menüdür: **Seri Muhakeme**, **Uzlaştırma**, **Ön Ödeme** (3 konu / 6 soru). Basit yargılama, CMK merkezinin Özel yargılama usulleri kategorisindedir.
- Tekrar eden birleşik CMK250/253 karşılaştırma dersi görünür listeden çıkarılmıştır. Eski UI konu kimliği ayrıntılı Seri Muhakeme dersine yönlenir. Eski çekirdek kayıt ve API geriye uyum için tutulur; kullanıcıya ikinci ders olarak gösterilmez.
- Merkez: **47 konu / 94 soru**; ayrı alternatif bölüm: **3 konu / 6 soru**; toplam **50 benzersiz konu / 100 soru**. 1–335 numaralı ana aralık okuma haritası iki bölümün birleşiminde korunur; güncel hukuk/fıkra tamlığı onayı değildir.

Mevcut xUnit paketinin kapsam/sayaç beklentileri yeni tekil kataloğa uyarlandı; alternatif konuların merkeze tekrar girmediğini, Basit yargılamanın merkezde olduğunu ve birleşik özetin görünür olmadığını denetleyen bir test eklendi. Windows STA komut yürütücüsü gerçek uygulama nesnelerinde yeni menü ağacını, iki kapsam arasında geçişi, tek sistematik nesnesini ve geçici verinin korunmasını sınar. Fiziksel Windows GUI/DPI kabulü NOT_RUN; resmî güncel mevzuat NOT_VERIFIED; üretim/stable NO-GO. Main merge, tag, release veya installer yok.
