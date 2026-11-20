# TUR012 — Onaysız veri kaybını engelleyen şifreli kasa geri yüklemesi

Temel sürüm: TUR011 Draft PR #12, ef8f20b5585fec502afad1049e5ebbb450484682, 74/74 xUnit.

## Somut risk ve düzeltme
TUR011 tüm mevcut AES-GCM notları doğrulasa da eski ve geçerli şifreli yedek yeni notları silip mevcut notları geçmiş içeriğe döndürebiliyordu.
Yeni PreviewRestore, kaynak ve hedefin tüm mevcut şifreli notlarını doğrular; içerik metnini dışarı çıkarmadan yalnız kanun/madde/sürüm kimliği, şifreli içerik SHA-256 ve ID farklarını sayar. Eksik hedef not ve değişmiş not sayıları kullanıcıya sunulur.
RestoreFrom varsayılan userConfirmedDataLoss=false durumunda veri kaybı yaratacak geri yüklemeyi reddeder. WPF ekranı sayıları gösteren açık EVET/HAYIR uyarısıyla onay alır. HAYIR veya eksik onay varsa kasa değişmez.
ApprovedPreview kaynak ve hedef SHA-256 değerlerini içerir. Onaydan önce ve geri yükleme dosyası yerine konmadan hemen önce bu hash'ler tekrar doğrulanır. Kullanıcı onayından sonra başka bir kayıt oluşursa yeniden ön inceleme gerekir.
Mevcut rollback dosyası ezilmez; yeni bir rollback oluşturularak onaylı geri dönüşte eski içerik korunur.

## Windows'a özgü test düzeltmesi
İlk TUR012 Windows CI #44836402765 başarısızdı: SHA hesaplayan FileStream FileShare.Read açıldığı için SQLite yedeği okumada Windows paylaşım ihlali oluştu. FileShare.ReadWrite düzeltmesi yapıldı. Hata gizlenmedi; nihai PASS yalnız son exact HEAD Windows CI ile verilecektir.

## Açık sınırlar
AES-GCM kimlik doğrulaması silinmiş tüm geçmiş kayıtları, bütün-kasa rollback/replay saldırısını veya kanun/madde/sürüm gibi SQLite metadata'nın açıklığını çözmez. Bağımsız imzalı, harici güvenli monotonic karşılık ve Windows ACL/doğrudan kullanıcı UAT yok. Eski şifresiz SQLite kaynakları ve yedekleri otomatik silinmez.
AYM 12.02.2026 E.2023/128 K.2026/36 RG 25.05.2026/33264 (https://www.resmigazete.gov.tr/25.05.2026) yayımlama kaydı mevcut; tam karar byte/fıkra, sonraki kanun ve olay tarihi CMK134 geçerliliği NOT_VERIFIED.
Danıştay 2007/948 İstanbul Barosu basılı karar sureti, İDDK 2012/1117 ve 2025 YD 2024/629 onama/yürütme durdurma kaynakları orijinallik, safahat ve olay tarihi etkisi bakımından NOT_VERIFIED. CMK127/3 ön hâkim kararı ve 24/48 saat hesapları yalnız mekanik delil uyarısıdır.
Korundu: iki tarih modülü, tek fail-closed Gece Vakti, üç Sistematiği, Ceza/Para/YDO, E-terazi, açık/koyu tema, sentetik şifreli not kasası. 49 önceki ve 5 yeni olmak üzere 54 insan gözlemi gerektiren UAT; fiziksel Windows NOT_RUN.
Resmî TCK/CMK/Yönetmelik tamlığı, 7571, UYAP Güneş çizelgesi, GPLv3/MIT ve logo dağıtımı NO-GO. Main merge/tag/installer/release yapılmaz.