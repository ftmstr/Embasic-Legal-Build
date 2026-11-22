namespace Embasic.Legal.Core;

/// <summary>
/// Instructional questions and user workflow reminders, NOT the text of any
/// current statute. The official current consolidation was not retrieved.
/// No legal result, permission, medical decision or procedural authorization.
/// </summary>
public sealed record CmkLearningStep(
    int Order, string Stage, string ReviewPrompt, string DocumentToInspect, string Gate);

public sealed record CmkLearningTableRow(
    string Stage, string HumanReviewQuestion, string DocumentToInspect, string UnresolvedRisk);

public sealed record CmkLearningQuizQuestion(
    string Prompt, IReadOnlyList<string> Choices, int CorrectIndex, string Explanation);

public sealed record CmkLearningTopic(
    string Key, string Group, string Title, string ArticleLocator, string LearningGoal,
    IReadOnlyList<CmkLearningStep> Flow, IReadOnlyList<CmkLearningTableRow> Matrix,
    IReadOnlyList<CmkLearningQuizQuestion> Quiz,
    string SourceStatus = "NOT_VERIFIED")
{
    public CmkLearningLesson? Lesson { get; init; }
    public bool IsCurrentLawVerified => false;
    public string SourceNotice =>
        "GÜNCEL MEVZUAT: NOT_VERIFIED. Madde numarası yalnız arama referansıdır; " +
        "kesin hüküm, süre, karar yetkisi veya usul şartı değildir. " +
        "Güncel konsolide metin yalnız mevzuat.gov.tr üzerinden doğrulanmalıdır.";
}

public sealed record CmkLearningResult(bool Correct, string Feedback,
    bool LegalComplianceCertified, string SourceStatus);

public static class CmkLearningCatalog
{
    public const string CurrentCmkUrl = "https://www.mevzuat.gov.tr/MevzuatMetin/1.5.5271.pdf";
    public const string CurrentTckUrl = "https://www.mevzuat.gov.tr/MevzuatMetin/1.5.5237.pdf";
    public const string CanonicalSource = "https://www.mevzuat.gov.tr/";

    private static CmkLearningTopic Make(
        string id, string group, string title, string locator, string objective,
        string question1, string question2, string question3, string question4,
        string scenario, string wrong1, string safeChoice, string wrong2)
    {
        var focus = new[] { question1, question2, question3, question4 };
        string[] names = ["Kapsam ve kişi", "Karar / yetki", "İcra ve güvenceler", "Belge / sürüm denetimi"];
        string[] docs =
        [
            "Kimliksiz olay özeti ve kişi sıfatı; kişisel veri girmeyin",
            "Karar/emir ve yazılı gerekçe: asıl ve yetki ayrıca doğrulanmalı",
            "Uygulama tutanağı, hak güvenceleri ve uzmanlık belgeleri",
            "Tarih, sürüm, kaynak ve işlem sonucu evrakı; asıl belge doğrulanmadı"
        ];
        var steps = Enumerable.Range(0, 4)
            .Select(i => new CmkLearningStep(i + 1, names[i], focus[i], docs[i],
                "İNSAN KONTROLÜ GEREKİR — otomatik karar yok")).ToArray();
        var matrix = steps.Select(step => new CmkLearningTableRow(
            step.Stage, step.ReviewPrompt, step.DocumentToInspect,
            "Resmî güncel norm ve özgün belgenin doğrulanması bekleniyor")).ToArray();
        CmkLearningQuizQuestion[] quiz =
        [
            new(scenario, [wrong1, safeChoice, wrong2], 1,
                "Bu yalnız eğitim senaryosunda en güvenli ilk inceleme yaklaşımıdır. " +
                "Somut olaya hukukî işlem izni veya karar üretmez."),
            new("Bu başlığa ilişkin güncel CMK/TCK metni mevzuat.gov.tr üzerinden doğrulanamadığında ne yapılır?",
                ["Eski TBMM metni otomatik güncel kabul edilir",
                 "Eksik kaynakla uygunluk PASS verilir",
                 "NOT_VERIFIED korunur, güncel resmî sürüm ve olay tarihi insan tarafından doğrulanır"],
                2, "TBMM ilk kabul metni güncel konsolide kanun değildir; hukukî uygunluk kanıtı oluşmadı.")
        ];
        return new CmkLearningTopic(id, group, title, locator, objective,
            Array.AsReadOnly(steps), Array.AsReadOnly(matrix), Array.AsReadOnly(quiz));
    }

    public static IReadOnlyList<CmkLearningTopic> All { get; } =
        Array.AsReadOnly(new[]
        {
            Make("cmk74", "Bilirkişilik ve sağlık", "Gözlem altına alınma",
                "CMK 74", "Akıl sağlığına ilişkin gözlem sürecini tıbbî ve adlî roller bakımından ayır.",
                "Gözlem ihtiyacı ile sıradan muayene veya bilirkişi raporu arasındaki fark dosyada belirlenmiş mi?",
                "Karar merciinin kim olduğu, talebin gerekçesi ve güncel usul şartları doğrulandı mı?",
                "Kişinin hakları, sağlık kurumu ve denetim belgeleri ayrı ayrı incelendi mi?",
                "Olay/karar tarihi ile güncel CMK 74 metni ve tıbbî belgelerin asılları eşleşiyor mu?",
                "Bir sağlık değerlendirmesi dosyada var ancak gözlem kararı belirsiz. İlk denetim?",
                "Muayene raporunu kendiliğinden gözlem izni say",
                "İşlemin niteliğini, özgün karar dayanağını ve güncel normu ayrı kontrol et",
                "Süre ve yetkiyi eski ders notundan otomatik uygula"),
            Make("cmk75-77", "Bilirkişilik ve sağlık", "Beden muayenesi ve örnek alma",
                "CMK 75–77", "Şüpheli/sanık ve diğer kişiler bakımından usul türlerini karıştırmadan incele.",
                "Hakkında işlem yapılacak kişi ve istenen muayene/örnek açıkça ayrıştırılmış mı?",
                "İlgili kararın yetkisi, gerekçesi ve müdahale niteliği araştırıldı mı?",
                "Tıbbî uygulayıcı, mahremiyet, bedensel bütünlük ve kayıt güvenceleri incelendi mi?",
                "Kişi türüne göre farklı norm ve olay tarihinde yürürlük kontrol edildi mi?",
                "Şüpheli ve mağdura ilişkin örnek talepleri aynı evrakla işleme konulacak. Ne yapılmalı?",
                "İki kişinin usulünü aynı kabul et",
                "Kişi sıfatına göre işlemi ayırıp karar, tıbbî müdahale ve kaynak şartlarını doğrula",
                "İşlem sonrası yalnız sonuç raporunu kontrol et"),
            Make("cmk78-80", "Bilirkişilik ve sağlık", "Moleküler genetik incelemeler",
                "CMK 78–80", "Genetik inceleme kararı, amacı, erişim ve saklama risklerini ayrı değerlendir.",
                "İstenen analiz türü ve ne için gerekli olduğu açıkça belirlenmiş mi?",
                "İncelemenin karar kapsamı ve uzman/laboratuvar yetkilendirmesi belgelenmiş mi?",
                "Örnekten analize zincir, erişim, gizlilik ve saklama tedbirleri incelendi mi?",
                "Sonuçların kullanılma sınırı, imha/saklama ve güncel norm doğrulandı mı?",
                "DNA raporu var, ancak örnek zinciri ve karar kapsamı belirsiz. İlk adım?",
                "Rapor bulunduğu için tüm usulü PASS say",
                "Karar kapsamını, örnek zincirini ve gizlilik/saklama kanıtlarını ayrı doğrula",
                "Maddi delil var diye kişisel veri güvenliğini atla"),
            Make("cmk81-89", "Bilirkişilik ve sağlık", "Kimlik, keşif ve ölüm incelemeleri",
                "CMK 81–89 (konu arama aralığı)", "Fizik kimliği, keşif, ölü muayenesi ve otopsiyi ayrı iş akışı olarak ele al.",
                "İstenen işlem kimlik tespiti, olay yeri incelemesi veya ölüm incelemesi olarak doğru sınıflandırılmış mı?",
                "İlgili inceleme/karar yetkisi, uzman katılımı ve işlem kaynağı ayrıştırılmış mı?",
                "Tutanak, görüntü, örnek, zincir ve uzman raporları denetlenmiş mi?",
                "Hangi madde hangi işleme uygulanır güncel mevzuat.gov.tr metninden kontrol edildi mi?",
                "Keşif ile otopsi adımlarını tek şema içinde kesin norm gibi kullanmak istiyorsunuz. Ne yapılmalı?",
                "Hepsini aynı yetki ve belgelendirme kabul et",
                "İşlem türünü ayrı belirleyip özgün tutanak, uzmanlık ve güncel maddeyi kontrol et",
                "Eski özet nottaki madde numarasını kanun aslı say"),
            Make("cmk90-99", "Koruma tedbirleri", "Yakalama ve gözaltı",
                "CMK 90–99", "Yakalama, gözaltı, bildirim, müdafilik ve gözden geçirme noktalarını ayır.",
                "Yakalama türü, kişi ve işlemin dayandığı somut koşullar ayrı tespit edilmiş mi?",
                "Karar, emir, kişi sıfatı ve süre hesabına esas başlangıç belgesi var mı?",
                "Hak bildirimleri, müdafi, sağlık ve itiraz/inceleme evrakı kontrol edildi mi?",
                "Tarih/saat zinciri, resmî güncel norm ve özgün belgeler doğrulandı mı?",
                "Yakalama saati dosyada farklı iki belgede farklı görünüyor. Yaklaşım?",
                "En erken saati otomatik doğru say ve kararı yaz",
                "Özgün evrak ve zaman çizelgesindeki çelişkiyi belgelendirip insan incelemesine bırak",
                "Yalnız son kolluk özetini yeterli gör"),
            Make("cmk100-108", "Koruma tedbirleri", "Tutuklama",
                "CMK 100–108", "Mevcut Tutuklama sistematiğiyle bağlantılı belge ve alternatif tedbir kontrolü yap.",
                "İsnat, şüphe/delil, ölçülülük ve kişinin durumu dosyada ayrı tartışılmış mı?",
                "Karar yetkisi, gerekçe ve alternatiflerin değerlendirilmesi özgün evrakta var mı?",
                "Müdafi, savunma, tebliğ ve denetim güvenceleri için belge kontrolü yapıldı mı?",
                "Süre/tarih ve olay tarihli güncel norm ile önceki sistematik karşılaştırıldı mı?",
                "Tutuklama gerekçesi hazır, ancak adli kontrol tartışması görünmüyor. Güvenli adım?",
                "Mevcut şablonu aynen kesin gerekçe say",
                "Eksik alternatif değerlendirme ve belge dayanaklarını insan incelemesine yönlendir",
                "Tedbiri otomatik uygula"),
            Make("cmk109-115", "Koruma tedbirleri", "Adlî kontrol",
                "CMK 109–115", "Mevcut Adli Kontrol sistematiğiyle yükümlülük, karar ve gözden geçirmeyi bağla.",
                "Tedbirin amacı ve kişi/olay bakımından koşullar ayrıştırılmış mı?",
                "Tedbir türü, karar kapsamı ve yetkili merciin asıl evrakı mevcut mu?",
                "Yükümlülüğün uygulanması, tebliğ ve değişiklik incelemesi belgelenmiş mi?",
                "Güncel kanun, sürelere ilişkin değişiklikler ve dosya tarihi doğrulanmış mı?",
                "Adli kontrol yükümlülüğü dosyada sadece başlık olarak var. Nasıl başlanır?",
                "Başlığın kendisini yeterli belge say",
                "Özgün kararı, yükümlülük kapsamını ve uygulama evrakını incele",
                "Sırf başlıkla uyulmuş say"),
            Make("cmk116-122", "Arama ve delil", "Adlî arama",
                "CMK 116–122", "Mevcut Arama ve El Koyma sistematiğindeki arama yetki ve kapsamını öğret.",
                "Aramanın türü, yeri, kişi ve hukuki kapsamı ayrı belirlenmiş mi?",
                "Hâkim kararı veya iddia edilen diğer karar/emir dayanağı yazılı olarak doğrulandı mı?",
                "Hazır bulunanlar, uygulama, tutanak ve haklar açısından eksik kanıt var mı?",
                "AYM/Danıştay asıl kararları ve güncel kanun/yönetmelik sürümü doğrulandı mı?",
                "Arama tutanağı var ancak dayanak kararın aslı yok. Neye öncelik verilir?",
                "Tutanak varsa kararın varlığı kabul edilir",
                "Yetki dayanağını ve kapsamını resmî asıl evraktan ayrıca doğrula",
                "Eski yönetmelik özetinden yeni karar yetkisi çıkar"),
            Make("cmk123-128", "Arama ve delil", "El koyma ve malvarlığı",
                "CMK 123–128", "El koyma, muhafaza, karar ve malvarlığına ilişkin belge akışını ayır.",
                "El koyma türü ve hangi varlığa müdahale edildiği net mi?",
                "Karar/emir, mahkeme denetimi ve özgün belgeler kontrol edildi mi?",
                "Teslim, muhafaza, zincir ve iade süreçleri belgeli mi?",
                "CMK 127/3 saatleri yalnız ön koşulları karşılanırsa aritmetik incelendi mi?",
                "El koyma saati var ama karar dayanağı belirsiz. 24/48 saat aracını kullanmadan önce?",
                "Bilinmeyen yetkiyi kararsız elkoyma kabul et",
                "Önce hâkim kararı/emir dayanağını ve özgün evrakı sınıflandır",
                "Süre hesabını hukuki uygunluk onayı say"),
            Make("cmk128a", "Arama ve delil", "Finansal hesaplara ilişkin özel işlem",
                "CMK 128/A — güncel sürüm doğrulanmadı", "Genel el koyma ile özel mali hesap işlem usulünü karıştırmadan incele.",
                "İşlem türü ve özel norm kapsamına girme iddiası belgeli mi?",
                "Hesap işleminin yazılı dayanağı, başlangıç ve bildirim kayıtları var mı?",
                "Kişinin başvuru/itiraz güvencesi ve muhafaza belgeleri incelendi mi?",
                "Olay tarihi, 7571 ilk metni ve sonraki konsolide değişiklikler ayrı doğrulandı mı?",
                "CMK 128/A ile genel el koyma için aynı otomatik süreyi hesaplamak isteniyor. Ne yapılmalı?",
                "Genel CMK 127 süresini doğrudan uygula",
                "Özel normu ve olay tarihli güncel sürümü bağımsız doğrula, aritmetiği beklet",
                "TBMM'nin ilk kabul metnini bugünkü konsolide CMK say"),
            Make("cmk134", "Arama ve delil", "Dijital arama, kopyalama ve el koyma",
                "CMK 134 — AYM aslı henüz doğrulanmadı", "Dijital inceleme, kopya, bütünlük ve AYM değişiklik risklerini birlikte izle.",
                "Dijital işlem tipi, cihaz ve aramanın kapsamı sınıflandırılmış mı?",
                "Arama/kopyalama/el koyma kararının aslı ve kapsamı dosyada var mı?",
                "İmaj, hash, zincir, tutanak ve teknik uygulama doğrulanmış mı?",
                "AYM hüküm-fıkra/ileri yürürlük ve olay tarihli konsolide CMK eşleşti mi?",
                "Dijital cihaz hash'i var ancak arama kararının aslı ve tarihli CMK sürümü yok. Öncelik?",
                "Hash tek başına hukuka uygunluk sağlar",
                "Karar aslı, hash zinciri ve olay tarihindeki normu ayrı incele",
                "AYM basın duyurusunu tam mahkeme karar aslı say"),
            Make("cmk250-253", "Alternatif çözümler", "Seri muhakeme ve uzlaştırma",
                "CMK 250 / 253 — sürüm doğrulanmadı", "Alternatif çözüm yollarında kapsam, taraf iradesi ve geçiş normu kontrolünü öğret.",
                "Süreç türü ve kapsamı doğru seçildi mi, istisnalar insan tarafından incelendi mi?",
                "Usul, bilgilendirme ve gerekli rıza/irade belgeleri var mı?",
                "Teklif, sonuç ve tutanakların asılları kontrol edildi mi?",
                "Geçici 8 dahil olay ve işlem tarihli değişiklikler doğrulandı mı?",
                "Uzlaştırma geçiş hükümleri tartışmalı ve dosya eski tarihli. İlk adım?",
                "Yeni düzenlemeyi eski dosyaya otomatik uygula",
                "Olay, teklif, uzlaşma tarihi ve geçici hükmü güncel resmî kaynakla incele",
                "Tarihleri görmeden tamamlandı kabul et"),
            Make("tck75", "Alternatif çözümler", "Ön ödeme",
                "TCK 75 — sürüm doğrulanmadı", "Kapsam, ödeme teklifi, bildirim ve sonuç belgelerini tarihli incele.",
                "Suç ve usul kapsamı, istisna iddiaları dosyada somut biçimde denetlenmiş mi?",
                "Tutar/hesap/teklif dayanağı olay tarihine göre doğrulanmış mı?",
                "Tebliğ, ödeme, süre ve sonuç tutanaklarının asılları ayrı görülmüş mü?",
                "Güncel TCK 75 ile geçiş/lehe norm gereklilikleri doğrulandı mı?",
                "Ön ödeme miktarı eski listeye dayanıyor. Nasıl incelenmeli?",
                "Tutar eski olduğu için aynen kullan",
                "Suç tarihi, geçerli norm, teklif ve tutar kaynağını resmî metinden kontrol et",
                "Hukukî kaynak olmadan otomatik teklif hazırla")
        });

    public static CmkLearningTopic Find(string key) =>
        CmkCurriculum.Find(key);

    public static CmkLearningResult Assess(string key, int quizIndex, int selectedOption)
    {
        var topic = Find(key);
        if (quizIndex < 0 || quizIndex >= topic.Quiz.Count)
            throw new ArgumentOutOfRangeException(nameof(quizIndex));
        var question = topic.Quiz[quizIndex];
        if (selectedOption < 0 || selectedOption >= question.Choices.Count)
            throw new ArgumentOutOfRangeException(nameof(selectedOption));
        bool correct = selectedOption == question.CorrectIndex;
        return new CmkLearningResult(correct,
            (correct ? "EĞİTİM YANITI DOĞRU. " : "EĞİTİM YANITI TEKRAR İNCELENMELİ. ") +
            question.Explanation + " GÜNCEL KANUN NOT_VERIFIED; KARAR/YETKİ ONAYI YOK.",
            LegalComplianceCertified: false, SourceStatus: "NOT_VERIFIED");
    }
}
