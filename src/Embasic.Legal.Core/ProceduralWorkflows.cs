namespace Embasic.Legal.Core;

/// <summary>
/// Teaching/checklist questions, NOT authenticated consolidated law texts.
/// Do not treat a completed checklist as a judgment of legality or a valid judicial decision.
/// </summary>
public sealed record ProceduralCheck(
    string Id, string Phase, string Question, string PracticalEvidence, string LegalReferences,
    bool Conditional = false);
public sealed record ProceduralWorkflow(
    string Key, string Title, string Description, IReadOnlyList<ProceduralCheck> Checks);
public enum ProceduralEvidenceState
{
    NotReviewed = 0,
    Documented = 1,
    Deficiency = 2,
    NotApplicable = 3
}
public sealed record ProceduralEvidence(string CheckId, ProceduralEvidenceState State, string EvidenceReference);
public sealed record ProceduralReview(
    int Total, int Documented, int Deficiencies, int NotReviewed,
    int NotApplicable, int UnsupportedDeclarations)
{
    public bool ReviewRowsComplete => NotReviewed == 0 && UnsupportedDeclarations == 0;
    public bool LegalComplianceCertified => false;
    public bool SourceCorpusVerified => false;
    public bool JudgmentIssued => false;
}
public static class ProceduralReviewEngine
{
    public static ProceduralReview Evaluate(ProceduralWorkflow workflow,
        IEnumerable<ProceduralEvidence> marks)
    {
        ArgumentNullException.ThrowIfNull(workflow);
        ArgumentNullException.ThrowIfNull(marks);
        var list = marks.ToList();
        if (list.Select(m => m.CheckId).Distinct(StringComparer.Ordinal).Count() != list.Count)
            throw new InvalidDataException("Aynı kontrol birden fazla işaretlenemez.");
        var known = workflow.Checks.Select(c => c.Id).ToHashSet(StringComparer.Ordinal);
        if (list.Any(m => !known.Contains(m.CheckId)))
            throw new InvalidDataException("Bilinmeyen denetim kimliği.");
        var mapped = list.ToDictionary(x => x.CheckId, StringComparer.Ordinal);
        int documented = 0, problems = 0, unknown = 0, excluded = 0, unsupported = 0;
        foreach (var check in workflow.Checks)
        {
            if (!mapped.TryGetValue(check.Id, out var mark) ||
                mark.State == ProceduralEvidenceState.NotReviewed)
            { unknown++; continue; }
            if (mark.State == ProceduralEvidenceState.Deficiency)
            { problems++; continue; }
            bool hasReference = !string.IsNullOrWhiteSpace(mark.EvidenceReference);
            if (mark.State == ProceduralEvidenceState.Documented)
            {
                if (hasReference) documented++; else unsupported++;
            }
            else if (mark.State == ProceduralEvidenceState.NotApplicable)
            {
                // N/A must be reasoned, even for "conditional" questions.
                if (hasReference) excluded++; else unsupported++;
            }
            else unsupported++;
        }
        return new ProceduralReview(workflow.Checks.Count, documented, problems,
            unknown, excluded, unsupported);
    }
}
public static class ProceduralWorkflows
{
    private static ProceduralCheck Q(string id,string phase,string question,string evidence,
        string laws,bool conditional=false) => new(id,phase,question,evidence,laws,conditional);

    public static ProceduralWorkflow Detention { get; } = new("detention",
        "Tutuklama Sistematiği",
        "Somut delil ve tutuklama nedeninden ölçülülük, adli kontrol alternatifleri, savunma ve periyodik incelemeye öğretici yol haritası.",
        [
            Q("T01","1. BAŞLANGIÇ","Dosyadaki somut delillerle desteklenen kuvvetli suç şüphesi ayrı ayrı değerlendirildi mi?",
                "Delil listesi, isnat edilen fiil, kişi bağlantısı; soyut kanaat yeterli sayılmamalı.","CMK m.100/1"),
            Q("T02","2. NEDEN","Kaçma/saklanma veya delil karartma ya da kişi üzerinde baskı riski hangi somut olgulara dayanıyor?",
                "Risk olgusu ve her delile referans; katalog suç varsa otomatik tutuklama sonucu çıkarma.","CMK m.100/2-3"),
            Q("T03","3. KANUNİ SINIR","Tutuklama yasağı, suç tipi/ceza sınırı, özel kişi-çocuk rejimi yönünden dosya incelendi mi?",
                "Suç vasfı, yaş, uygulanabilecek ceza ve özel norm kontrol listesi.","CMK m.100/4, m.102; özel hükümler"),
            Q("T04","4. ORANTILILIK","Tutuklama tedbiri işin önemi ve beklenen yaptırımla ölçülü mü; daha hafif tedbir neden yetmiyor?",
                "Alternatif adlî kontrol yükümlülüklerine ilişkin dosyaya özgü karşılaştırma.","CMK m.100/1, m.101/1-2, m.109"),
            Q("T05","5. İSTEM","Tutuklamaya sevk/istemde kuvvetli şüphe, nedenler, somut dayanak ve adli kontrolün neden yetersiz kaldığı açıklanmış mı?",
                "İstem yazısı ve hangi belgeye dayandığı; otomatik sevk önerilmez.","CMK m.101/1"),
            Q("T06","6. SAVUNMA","Şüpheli ve müdafi hakları, huzura çıkarma, dinleme ve savunma koşulları doğrulandı mı?",
                "İfade/duruşma kaydı, müdafi mevcudiyeti, usul ve hak bildirimi.","CMK m.101, m.147"),
            Q("T07","7. GEREKÇE","Hâkim kararında kuvvetli şüphe, tutuklama nedeni ve ölçülülük yönünden somut gerekçe var mı?",
                "Hâkim karar metni ve gerekçe kontrolü.","CMK m.101/2"),
            Q("T08","8. TEBLİĞ","Kararın bildirilmesi ve itiraz haklarıyla ilgili belgeler kontrol edildi mi?",
                "Tebliğ/tefhim, müdafi bildirimi ve süreye ilişkin evrak.","CMK m.101, m.267 vd."),
            Q("T09","9. SÜRE","Azami tutukluluk, somut dosyadaki özel uzatma veya çocuk statüsü değerlendirilmiş mi?",
                "Başlangıç, uzatma kararları; kanuni sürelerin insan tarafından doğrulanması.","CMK m.102; özel düzenlemeler"),
            Q("T10","10. PERİYODİK İNCELEME","Devam gerekliliği en geç kanuni aralıklarla ve değişen olgularla gözden geçiriliyor mu?",
                "CMK m.108 inceleme kararları ve yeni deliller.","CMK m.108"),
            Q("T11","11. TAHLİYE/DEĞİŞİM","Salıverilme ve adli kontrole geçme ihtimalleri düzenli izleniyor mu?",
                "Tahliye istemi, lehe değişiklik, CMK m.103-105 kayıtları.","CMK m.103-105"),
            Q("T12","12. ÖZEL DURUM","Çocuk, gebelik, sağlık veya diğer kanuni özel koşullar varsa ayrıca değerlendirildi mi?",
                "Özel durum yoksa gerekçe yazılarak uygulanmaz işaretlenebilir.","CMK m.100-110; ilgili özel normlar",true)
        ]);

    public static ProceduralWorkflow JudicialControl { get; } = new("supervision",
        "Adli Kontrol Sistematiği",
        "Şüphe ve ölçülülükten yükümlülüğün seçimi, karar yetkisi, süre ve periyodik gözden geçirmeye kontrol şeması.",
        [
            Q("A01","1. ŞÜPHE","CMK 100'deki nedenlerle ilişki, dosyaya özgü olgular ve alternatif tedbir ihtiyacı açıklandı mı?",
                "Somut şüphe ve özgürlük kısıtlama amacı belgelendi mi?","CMK m.100, m.109/1-2"),
            Q("A02","2. ALTERNATİF","Tutuklama yerine daha hafif adli kontrol neden yeterli veya gerekli değerlendirildi?",
                "Ölçülülük, tedbirin somut dosyayla bağlantısı.","CMK m.109; Anayasa m.13"),
            Q("A03","3. YÜKÜMLÜLÜK","Seçilen yükümlülük CMK 109/3 kapsamına giriyor ve yeterince belirli mi?",
                "Yurt dışı yasağı, imza, konut terk etmeme vb. seçimin kapsamı.","CMK m.109/3"),
            Q("A04","4. MERCİ","Soruşturma/kovuşturma evresine göre talep ve karar mercii doğru mu?",
                "Cumhuriyet savcısı istemi / sulh ceza hâkimi / mahkeme kontrolü.","CMK m.110"),
            Q("A05","5. UYGULANABİLİRLİK","Yükümlülük kişinin durumu ve dosyanın ihtiyacına göre fiilen uygulanabilir mi?",
                "Saat, sıklık, adres, merci, ekonomik durum gibi gerekçe belgeleri.","CMK m.109-110"),
            Q("A06","6. KİŞİSEL DURUM","Çocukluk, gebelik, ağır hastalık/engellilik gibi özel güvenceler incelendi mi?",
                "Özel durum yoksa neden uygulanmadığı kaydedilmeli.","CMK m.109/4, 110/A/3; özel mevzuat",true),
            Q("A07","7. BİLDİRİM","Kararın gerekçesi, kapsamı, bildirimi ve itiraz olanağı denetlendi mi?",
                "Karar, tebliğ/tefhim ve başvuru evrakı.","CMK m.110-111"),
            Q("A08","8. TAKİP","Yükümlülüğe uyum, kolluk/idari bildirim ve olay kayıtları takip ediliyor mu?",
                "Yükümlülük yerine getirme çizelgesi; otomatik ihlal varsayma.","CMK m.109, m.112"),
            Q("A09","9. SÜRE","CMK 110/A azami süre, uzatma ve çocuklar için ayrı rejim kontrol edildi mi?",
                "Tedbir başlangıç ve uzatma tarihleri; insan onaylı süre kontrolü.","CMK m.110/A"),
            Q("A10","10. GÖZDEN GEÇİRME","Tedbir devamı en geç kanundaki periyodik aralıklarla ele alındı mı?",
                "Somut olgular ve devam kararı; varsayılan otomatik devam yok.","CMK m.110/4"),
            Q("A11","11. DEĞİŞTİRME/KALDIRMA","Kaldırma, değiştirme veya geçici muafiyet talebi uygun merciye taşındı mı?",
                "Yeni delil, koşul değişimi, talep ve karar evrakı.","CMK m.110/2, m.111"),
            Q("A12","12. İHLAL","İhlal iddiası varsa kasıt, mazeret, tebligat ve CMK 112 şartları ayrı incelendi mi?",
                "İhlal yoksa gerekçeli uygulanmaz seçilebilir; ihlal otomatik tutuklama değildir.","CMK m.112",true)
        ]);

    public static ProceduralWorkflow SearchSeizure { get; } = new("search",
        "Arama ve El Koyma Sistematiği",
        "CMK adlî araması ve Adlî ve Önleme Aramaları Yönetmeliği'nin ADLÎ ARAMA bölümü paralel denetimi; önleme araması rejimiyle karıştırılmaz.",
        [
            Q("S01","A. TÜR","Tedbir gerçekten adlî arama mı, önleme araması mı; soruşturulan fiil ve amaç ayrıştırıldı mı?",
                "Suç delili/yakalama amacı ile önleyici genel denetim ayrımı.","CMK m.116-117; Yönetmelik m.5; önleme m.18-26; ortak icra m.27-32"),
            Q("S02","A. ŞÜPHE","Adlî aramanın makul şüphe dayanakları somut olgu ve ihbarı destekleyen emarelerle gösterildi mi?",
                "Olay tarihi, eşyanın niteliği, konum, şüpheyi destekleyen olgular.","CMK m.116; Yönetmelik m.6"),
            Q("S03","A. KİŞİ","Şüpheli/sanık yeri ile üçüncü kişiye ait arama ayrımı ve ek şartlar belgelendi mi?",
                "Üçüncü kişinin yerinde delil/şüpheli olabileceğini gösteren olaylar.","CMK m.116-117"),
            Q("S04","A. YER","Üst, araç, konut, işyeri, kapalı alan veya özel rejim doğru sınıflandırıldı mı?",
                "Konut/kapalı alan koruması, açık adres ve aranacak kapsam.","CMK m.116-119; Yönetmelik m.5, m.7"),
            Q("S05","B. KARAR MERCİİ","Hâkim kararı esas mı; gecikmede sakınca gerekçesi varsa savcının yazılı emri usulüne uygun mu?",
                "Karar/emir tarih-sayısı; gecikme nedeniyle somut delil kaybı riski.","CMK m.119/1; Yönetmelik m.4, m.7"),
            Q("S06","B. KAPALI ALAN","Konut, işyeri ve kamuya açık olmayan kapalı alanda kolluk amiri emrinin yetkisizliği ayrıca denetlendi mi?",
                "Kararı veren merci ve yer sınıfının eşleştirilmesi.","CMK m.119/1; Yönetmelik m.7"),
            Q("S07","B. SAVCIYA ULAŞILAMAMA","Savcıya ulaşılamama ileri sürülmüşse iletişim denemeleri ve yazılı gerekçe/tutanak var mı?",
                "Arama saatleri, haberleşme girişimleri, ulaşamama tutanağı ve bildirim.","CMK m.119/1; Yönetmelik m.7",true),
            Q("S08","B. KARAR KAPSAMI","Kararda fiil, aranacak kişi/yer/eşya, adres ve geçerli süre açıkça belirlenmiş mi?",
                "Karar veya yazılı emir ve ekleri; kapsam aşımı kontrolü.","CMK m.119/2; Yönetmelik m.7"),
            Q("S09","B. KARAR İSTİSNASI","Kararsız arama iddiası varsa hangi istisna ve iptal edilen yönetmelik ibareleri kontrol edildi?",
                "Yönetmelik m.8 istisnaları ve Danıştay iptal kararları; salt rıza yeterli sayılmamalı.","Yönetmelik m.8; CMK m.116-119",true),
            Q("S10","C. GECE VAKTİ","Konut/işyeri/kapalı yerde gece yasağı ve CMK 118/2 istisnaları gerçekten incelendi mi?",
                "Doğuş/batış resmi UYAP çizelgesi yoksa kesin saat hesabı yapılmaz.","CMK m.118; TCK m.6/1-e; Yönetmelik m.4"),
            Q("S11","C. ÖZEL DURUM","Olay yeri incelemesi söz konusuysa açık-kapalı alan ve gerekli karar rejimi ayrıldı mı?",
                "Özel alan için ayrı karar veya kanuni istisna analizi.","Yönetmelik m.9",true),
            Q("S12","C. İCRA","Cumhuriyet savcısı hazır olmadan konut/işyeri/kapalı yerde iki tanık şartı kontrol edildi mi?",
                "İhtiyar heyeti/komşudan iki kişi, kimlik ve tutanak.","CMK m.119/4",true),
            Q("S13","C. HAZIR BULUNANLAR","Yer sahibi/zilyet veya kanuni ikamesi; avukatın aramada bulunma hakkı denetlendi mi?",
                "Hazır bulunanlar, CMK 117 üçüncü kişi bilgilendirmesi, vekil erişimi.","CMK m.120"),
            Q("S14","C. UYGULAMA","Arama fiilî kapsam, süre, ölçülülük, müdahale ve delil bütünlüğü yönünden denetlendi mi?",
                "İcra başlangıç-bitiş, işlem yapanlar, aranan yerler ve eşyalar; Yönetmelik m.30/1 iptal kaydı.","CMK m.119-120; Yönetmelik m.11, m.27-32"),
            Q("S15","D. TUTANAK","Arama tutanağında karar/emir, yer-saat, konu, kimlik, bulunan deliller, hazır olanlar ve imzalar var mı?",
                "CMK m.119/3 ve Yönetmelik m.11 alanları; eksik/hatalı bilgi ayrı belirtilecek.","CMK m.119/3; Yönetmelik m.11"),
            Q("S16","D. BELGE VERME","İstem hâlinde arama nedenini, delil bulunmadığını ve eşya listesini gösteren belge verilmiş mi?",
                "Talep ve verilen belge, eşya envanteri; mümkünse teslim imzası.","CMK m.121; Yönetmelik m.12"),
            Q("S17","D. BELGE/KÂĞIT","Özel kâğıtlar/mektuplar üzerindeki inceleme yetkisi ve mühürleme güvenceleri denetlendi mi?",
                "Sadece yetkili inceleme; özel/müdafi yazışması ayrımı.","CMK m.122, m.126; Yönetmelik m.16",true),
            Q("S18","E. ELKOYMA ŞARTI","Arama ile elkoyma kararının hukuki dayanağı, delil/ müsadere bağlantısı ve teslim şekli ayrıştırıldı mı?",
                "Elkonulan eşya için ayrı karar/emir, muhafaza zinciri, kimlik.","CMK m.123-127; Yönetmelik m.15"),
            Q("S19","E. ELKOYMA MERCİİ","Hâkim kararı veya kanuni acil yazılı emir ve arama kararından bağımsız yetki değerlendirildi mi?",
                "Elkoyma yazısı/emri, işlemi yapanlar ve tarih.","CMK m.127; Yönetmelik m.15"),
            Q("S20","E. HÂKİM ONAYI","Hâkim kararsız elkoymada kanuni 24 saat sunma ve 48 saat karar süresi belgelerle denetlendi mi?",
                "Elkoyma anı, sunma saati, hâkim karar ve tebliğ kayıtları; otomatik PASS yok.","CMK m.127/3; Yönetmelik m.10, m.15",true),
            Q("S21","E. TESADÜFİ DELİL","Karar/emir kapsamı dışı deliller için koruma, savcılığa bildirim ve yeni işlem gerekçesi var mı?",
                "Ayrı olay/delil kaydı, ayrı karar ve zaman çizelgesi.","CMK m.138/1; Yönetmelik m.10",true),
            Q("S22","E. ENVANTER","Elkonulan eşyanın ayırt edici özellikleri, muhafaza ve teslim zinciri belgeli mi?",
                "Seri no, mühür, teslim-tesellüm, dijital hash ve emanet kayıtları.","CMK m.127; Yönetmelik m.11-12, m.15"),
            Q("S23","E. İADE","Elkoymanın devam gereği ve iade/itiraz imkânları yeniden değerlendirildi mi?",
                "İade talebi, talebe cevap, el koyma devamına ilişkin kayıt.","CMK m.131-132; m.267 vd."),
            Q("S24","F. DİJİTAL MATERYAL","Bilgisayar/program/kütük araması varsa CMK 134'e özgü şüphe, son çare, yetki ve kopyalama güvenceleri kontrol edildi mi?",
                "Dijital cihaz, imaj/hash, inceleme protokolü. Yönetmelik m.17 hakkında 2025 YD bilgisi ayrıca kontrol edilir.","CMK m.134; Yönetmelik m.17 (yürütme durdurma/güncellik kontrolü)",true),
            Q("S25","F. AVUKAT BÜROSU","Avukat bürosu veya meslek sırrı söz konusuysa özel arama usulü ve hazır bulunacaklar uygulandı mı?",
                "Mahkeme kararı, savcı denetimi, baro katılımı, itiraz ve mühür.","CMK m.130; Yönetmelik m.13",true),
            Q("S26","F. ASKERÎ MAHAL","Askerî mahal veya özel mevzuat alanı varsa ayrı yetki ve iştirak şartları incelendi mi?",
                "Askerî makam katılımı, savcı nezareti, özel norm.","CMK m.119/5; Yönetmelik m.14",true),
            Q("S27","G. KAYNAK SÜRÜMÜ","Olay tarihindeki CMK/Yönetmelik hükümleri, Danıştay kararları ve değişikliklerin kapsamı ayrı doğrulandı mı?",
                "Güncel/olay tarihli resmî norm ve karar metni, yürürlük belgesi; otomatik mevzuat PASS verilmez.","CMK m.116-138; Yönetmelik m.5-17; Danıştay iptal/YD kararları")
        ]);
    public static IReadOnlyList<ProceduralWorkflow> All { get; } =
        [Detention, JudicialControl, SearchSeizure];
    public static ProceduralWorkflow Get(string key) => key switch
    {
        "detention" => Detention, "supervision" => JudicialControl,
        "search" => SearchSeizure, _ => throw new ArgumentException("Bilinmeyen sistematik.")
    };
}
