# Embasic Legal

WPF Windows masaüstü uygulaması.

## Ceza Muhakemesi Hukuku

47 merkez dersinde konuya özgü öğrenme hedefleri, madde okuma rehberi, **141 hatırlama kartı**, **141 karşılaştırma satırı**, seçilebilir şema adımları ve **47 dallanan senaryo** bulunur. Merkezdeki 94 uygulama sorusu açıklamalı tekrar denemesini destekler; ilk yanıt puanı tekrarlarla artmaz. Kaynak metin doğrulanmadığından bunlar kavramsal taslaklardır.

Sol menüde tek açılır başlık altında 13 kategori, **47 konu ve 94 soru** bulunur. İlgili konularda **İşlem sistematiği** sekmesi, mevcut Tutuklama, Adli Kontrol ve Arama/El Koyma araçlarını açar. Aynı araç veya konu için ayrı üst menü oluşturulmaz; geçici işaretler aynı nesnede korunur. Basit yargılama, özel yargılama usulleri içindedir.

**Alternatif Çözümler** ayrı üst menüdür: Seri Muhakeme, Uzlaştırma ve Ön Ödeme (**3 konu / 6 soru**). Önceki birleşik CMK250/253 özeti görünür katalogdan kaldırılmış, ayrıntılı dersler tekilleştirilmiştir. Toplam **50 benzersiz konu / 100 soru**. Eski konu kimliği geriye uyumlu erişimde ilgili ayrıntılı derse yönlendirilir.

- Dal: `tur029/cmk-active-learning-workshop`
- Temel: TUR028 / Draft PR #29
- Açılış: `Embasic.Legal.exe` → Ceza Muhakemesi Hukuku

Güncel mevzuat **NOT_VERIFIED**; fiziksel Windows GUI kabulü **NOT_RUN**. Üretim/stable **NO-GO**, main merge veya release yoktur.

```powershell
dotnet restore Embasic.Legal.slnx
dotnet build Embasic.Legal.slnx -c Release --no-restore
dotnet test tests/Embasic.Legal.Tests/Embasic.Legal.Tests.csproj -c Release --no-restore
```
