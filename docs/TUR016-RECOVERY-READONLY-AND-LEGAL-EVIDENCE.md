# TUR016 — Kurtarma ön incelemesi SQLite salt-okunur kapısı

## P0 teknik bulgu (TUR015 kod analizi)
`EncryptedArticleNotes.PreviewRestore` kullanıcı tarafından seçilen yedek ve hedefi normal `EncryptedArticleNotes` ile açıyordu. Bu constructor, `ArticleNoteStore` üzerinden `SqliteOpenMode.ReadWriteCreate` kullanıp açılışta `CREATE TABLE IF NOT EXISTS ArticleNotes` ve indeks DDL çalıştırır. **İstenen işlem salt-okunur ön inceleme olmasına rağmen**, kullanıcı yanlış bir yabancı SQLite dosyası seçtiğinde, parola başlığı bulunamadan önce yabancı dosyaya `ArticleNotes` tablosu eklenebilmesi veri bütünlüğü ihlalidir.

## Düzeltme
- `ArticleNoteStore(path, readOnly: true)`: yalnız `SqliteOpenMode.ReadOnly`, `PRAGMA query_only=ON`; klasör, tablo, indeks, kaynak dosya oluşturma YOK. Açılış hatasında bağlantı kapatılır.
- `EncryptedArticleNotes` gizli salt-okunur mevcut kasa açılışı; restore ön inceleme kaynak/hedef, doğrulanmış staging ve eski yedeklerin açılışında kullanılır. Normal kasa oluşturma ve not kaydetme hâlâ mevcut EAL4 uyumlu yazılabilir yoldadır.
- Kaynak veya hedefte TUR015 WAL/SHM/journal fail-closed gate değişmeden korunur. Gerekli sistem düzeyi kilit / TOCTOU / junction / symlink savunması kanıtlanmış değildir. Olay tarihli hukuk normlarına otomatik izin üretilmez.
- Odaklı testler: yabancı SQLite yedek ve hedefte SHA + şema değişmemesi; geçerli yedeğin SHA koruması; bulunmayan dosyanın yaratılmaması; manuel onaylı mevcut restore semantiğinin korunması.
- Manuel UAT TUR015 67 + 5 TUR016 maddesi. Fiziksel Windows **NOT_RUN**.

## Hukuk kaynakları ve sınırlar
7571 m.22 CMK128/A, m.26 CMK Geçici 8 ve m.39 yürürlük tarihleri TBMM kanun kaydı ile desteklenmiştir; canlı olay tarihli eksiksiz CMK/TCK/Yönetmelik konsolidasyonu ve geçiş koşulları **NOT_VERIFIED**. AYM E.2023/128 K.2026/36 kaydı ve 25.05.2026/33264 RG kimliği doğrulandı; AYM resmî tam karar HTML/PDF özgün baytları ve karar sonucu–fıkra eşlemesinin exact hash kontrolü henüz **NOT_VERIFIED**. Danıştay 2007/948, İDDK 2012/1117 ve YD 2024/629 özgün mahkeme kararları **NOT_VERIFIED**. Norm/mahkeme sonuçları için otomatik PASS yok.

İki tarih hesabı, tek Gece Vakti, üç Sistematiği, Adli Ceza–Para–YDO, E-terazi logoları, açık/koyu tema ve şifreli notlar korunmuştur. Fiziksel UAT, lisans/NOTICE, resmî hukuk doğruluğu, stable üretim **NO-GO**.
