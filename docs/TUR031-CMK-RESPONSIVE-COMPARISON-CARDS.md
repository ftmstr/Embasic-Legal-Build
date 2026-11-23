# Embasic Legal Windows TUR031 — dar WPF penceresinde CMK karşılaştırma tabloları

**Baz:** TUR030 Draft PR #31 exact SHA `e9da460bbc4f22c3b571f0aecffb1c82703de96b`, 198/198 xUnit, 42/42 Windows STA, iki CI SUCCESS, özgün ZIP Drive ID `11GfHm8pXGFS0y_BwNyPTTJ5B6-spK85v`, rapor ID `10dxd_Nf3UaQ0OzWnWec7pZn_PV9hln9i`.

## Özgün, kaynak koduyla doğrudan kanıtlanan WPF UX kusuru
`MainWindow.xaml` asgari **900 DIP** genişlikte açılabiliyor, sol navigasyon kolonu **255 DIP**, ana panel Border yatay margin **24+24**, padding **20+20 DIP**; öğretim içeriği yaklaşık **557 DIP** kullanılabilir en fazla yatay alana düşüyor (diğer kenar/bölücü kalınlıkları hariç). `App.xaml` Ceza Muhakemesi Hukuku / **2 · Tablo** görünümünde iki DataGrid'in dört sabit sütun genişliği **120+275+265+245 = 905 DIP**. Hem 47 etkin öğrenme profili (`Comparison`) hem Alternatif Çözümler/fallback (`Matrix`) için çok dar pencerede yatay gezinme gerektirmektedir. Satır içi `TextWrapping=Wrap` tek başına sütun genişliğini azaltmaz. Ekran okuyucu, klavye, farklı DPI ve fiziksel yatay-kaydırma davranışları önceden **NOT_RUN**.

## Tek yeni somut UX iyileştirmesi
Aynı `2 · Tablo` sekmesinde:
- **Varsayılan dikey okuma kartları**, hem `Comparison` hem `Matrix` için 4 alanı tek kolonda ve `TextWrapping=Wrap` olarak gösterir. Eğitim verisi, soru, delil, hukukî karar, uygunluk algoritması **yeniden üretilmez**.
- `Comparison`: ölçüt, birinci kavram, ikinci kavram ve neden önemli; `Matrix`: aşama, ön inceleme sorusu, aranacak belge, açık risk; hepsi özgün aynı ViewModel nesnelerine bağlanır.
- **Açılır geniş tablo**, `IsExpanded=False` ile varsayılan kapalıdır, iki eski dört sütunlu DataGrid aynen ve yatay kaydırma özelliğiyle korunur.
- Özgün açık/koyu dinamik fırçalar ve UI Automation adları korunur/eklenir. Klavye/donanım ekran okuyucu kabulü **gerçek Windows testine bağlıdır**, CI bu iddiayı PASS yapmaz.
- Ana 47 konu, 94 soru, 141 kart, tekrar gündemi, 3 gömülü sistematik ve ayrı Alternatif Çözümler değişmez. Tarih 2, Gece Vakti 1, ceza/para/YDO, E-terazi ve AES-GCM not güvenliği aynen korunur.

## Kabul planı ve sınırlamalar
Windows-2022 .NET10 derleme, xUnit ve STA navigasyon; iki WPF ItemsControl + iki özgün DataGrid, otomatik XML/XAML derleme ve PowerShell kaynak yerleşim doğrulaması. `tools/TUR031-Windows-UAT.ps1` özgün 152 manuel gözlem üzerine 3 yeni ekran okuyucusu/900 DIP/125-150% DPI/koyu-açık tema ve grid/kart eşdeğerliği gözlemi ekler; **fiziksel Windows 11 GUI henüz NOT_RUN**.

Güncel/meri/konsolide mevzuat için yalnız `mevzuat.gov.tr` kanoniktir. Bu tur kanun metnine veya hukukî senaryolara müdahale etmez; güncel CMK/TCK PDF/DOC bayt/sürüm/yürürlük `NOT_VERIFIED`, AYM/Danıştay resmî karar tam metinleri `NOT_VERIFIED`. E-terazi dağıtım/lisans, fiziksel Windows kabul ve stable/production **NO-GO**; gerçek adlî/soruşturma veya kişisel veri kullanımı yasak. `main` merge/tag/installer/release yapılmaz.
