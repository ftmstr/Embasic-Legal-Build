namespace Embasic.Legal.Core;

public static partial class CmkStudyCatalog
{
    private static (string Prompt, string Correct, string NearMiss, string Generalization) TransferCase(string key) => key switch
    {
        "cmk1-2" => (
            "Bir dosyada iddianamenin kabulünden önce ve sonra aynı kişi dinlenmiş. Eğitim notunda iki işlemdeki kişi sıfatı aynı yazılmış. Hangi düzeltme gerekir?",
            "Evreyi ve kabul eşiğini belirleyerek şüpheli ile sanık sıfatlarını ayrı göstermek.",
            "Kişi değişmediği için muhakeme evresi ve sıfatını aynı kabul etmek.",
            "Suçun adı aynıysa iddianamenin kabulünün kişi sıfatına etkisini araştırmamak."),
        "cmk3-21" => (
            "İki dosya arasında bağlantı ileri sürülüyor; farklı mahkeme türleri ve yerler söz konusu. İlk şema nasıl kurulmalıdır?",
            "Bağlantı, görev ve yer yönünden yetkiyi ayrı sorular hâlinde incelemek.",
            "Bağlantı iddiasını görev ve yetki uyuşmazlıklarının tümünü çözen karar saymak.",
            "Dosyalar ilişkiliyse bütün mahkemelerin aynı tür ve yerde görevli olduğunu varsaymak."),
        "cmk22-32" => (
            "Hâkim, kendisine ilişkin önceki bir ilişkiyi bildirerek çekinme konusunu gündeme getirmiş. Bu kayıt nasıl adlandırılmalıdır?",
            "Hâkimin kendi bildirimiyle başlayan çekinme başlığı, tarafın ret isteminden ayrılarak incelenmelidir.",
            "Hâkimin kendi bildirimi her durumda tarafça verilmiş ret dilekçesi sayılmalıdır.",
            "İlişki bildirilmesi, inceleme ve karar usulünün tümünün tamamlandığını gösterir."),
        "cmk33-38a" => (
            "Elektronik dosyada karar tarihi, sisteme yükleme tarihi ve bildirim kaydı farklı günlere ait. Hangi yaklaşım doğrudur?",
            "Her tarihin hangi usul olayını kaydettiğini ayrı belirlemek.",
            "En son tarihi bütün usul olaylarının tek tarihi olarak kullanmak.",
            "Sisteme yükleme ile her muhataba tebliği aynı olay saymak."),
        "cmk39-42" => (
            "Bir çalışma notunda günle belirlenen süre ile ayla belirlenen süre aynı aritmetik kalıpla hesaplanmış. Ne kontrol edilmelidir?",
            "Sürenin birimi ve ona ilişkin hesaplama kuralı, başlangıç kaydıyla birlikte ayrı okunmalıdır.",
            "Bütün süreleri sabit sayıda güne çevirmek her durumda aynı sonucu verir.",
            "Başlangıç belgesi bulunduğunda sürenin gün, hafta veya ay olması önem taşımaz."),
        "cmk43-61" => (
            "Tanık hem meslek sırrına ilişkin bilgiye hem kişiyle yakınlığa değiniyor. İnceleme nasıl ayrılmalıdır?",
            "İleri sürülen her çekinme sebebinin kapsamı kendi başlığında araştırılmalıdır.",
            "Bir çekinme sebebi söylenince diğer sebebin kapsamı her durumda aynı sayılır.",
            "Tanığın mesleği bilindiğinde yakınlık ve belirli soruya cevap vermeme meseleleri kendiliğinden çözülür."),
        "cmk62-73" => (
            "İki teknik rapor farklı sonuçlara ulaşmış. Yalnız raporu düzenleyenin unvanına bakılarak birinin seçilmesi öneriliyor. Hangi inceleme eksiktir?",
            "Veri, yöntem, bulgu ve sonuç ilişkisi ile görüşlere yöneltilen itirazların karşılaştırılması.",
            "Unvanın, veri ve yöntemin incelenmesinin yerine geçirilmesi.",
            "Rapor sayısının çok olmasının hukukî değerlendirmeyi gereksiz kılması."),
        "cmk74" => (
            "Gözleme ilişkin bir karar bulunduğu söyleniyor; kurum kayıtları ve kişinin süreçteki haklarına ilişkin belgeler eksik. Nasıl ilerlenmelidir?",
            "Kararın varlığı ile uygulama, kişi güvenceleri ve denetim kayıtları ayrı incelenmelidir.",
            "Karar bulunduğu için kurum, kişi hakları ve denetim belgeleri araştırılmamalıdır.",
            "Uygulama kaydı eksikliği muayene ile gözlemin aynı işlem olduğunu gösterir."),
        "cmk75-77" => (
            "Bir tıbbî işleme kişi rıza göstermiş; eğitim notu bu nedenle diğer bütün usul şartlarının tamamlandığını söylüyor. Hangi değerlendirme doğrudur?",
            "Kişi sıfatı, müdahale türü ve ilgili usul rejimi belirlenmeden rızadan böyle kapsamlı bir sonuç çıkarılamaz.",
            "Her türlü rıza, bütün muayene ve örnek alma işlemlerinde aynı usul sonucunu tek başına doğurur.",
            "Rıza varsa iç muayene, dış muayene ve örnek alma ayrımı önem taşımaz."),
        "cmk78-80" => (
            "Genetik sonuç raporu mevcut; örneğin teslim zinciri ve sonucun kullanım/saklama kayıtları görülmemiş. Hangi konular kalır?",
            "Örnek zinciri ile sonuçların gizlilik, kullanım ve saklama/imha başlıkları ayrıca incelenir.",
            "Sonuç raporunun varlığı bütün örnek ve veri işlemlerinin uygunluğunu gösterir.",
            "Analiz tamamlandığı için verinin sonraki kullanımı muhakeme bakımından önemsizleşir."),
        "cmk81-89" => (
            "Fizik kimliğinin belirlenmesi ile ölüm nedeninin saptanması aynı kontrol kutusuna yazılmış. Hangi düzenleme uygundur?",
            "Bilgi ihtiyaçlarını ve ilgili inceleme türlerini ayrı iş akışlarına ayırmak.",
            "Aynı kişiyle ilgili oldukları için kimlik ve ölüm incelemesini aynı işlem saymak.",
            "Bir inceleme raporu varsa diğer bilgi ihtiyacının da cevaplandığını kabul etmek."),
        "cmk90-99" => (
            "Sağlık kaydı var, fakat müdafi ve hak bildirimine ilişkin kayıtlar belirsiz. Hangisi doğru bir kontroldür?",
            "Sağlık güvencesi ile savunma ve bildirim güvencelerini ayrı izlemek.",
            "Sağlık raporunun diğer bütün kişi hakları kayıtlarının yerine geçtiğini kabul etmek.",
            "Tek bir güvenceye ilişkin belgeyle bütün yakalama/gözaltı sürecini tamamlanmış saymak."),
        "cmk100-108" => (
            "Öğrenme notunda kişinin tutuklanmış olmasının suçluluğunu kesinleştirdiği yazıyor. Hangi düzeltme gerekir?",
            "Koruma tedbirini, yargılama sonunda kurulacak suçluluk hükmünden ayırmak.",
            "Tutuklama ile mahkûmiyetin aynı karar türü olduğunu kabul etmek.",
            "Tedbir kararı bulunduğu için isnat ve savunmanın ayrıca değerlendirilmesine gerek olmadığını söylemek."),
        "cmk109-115" => (
            "Adlî kontrol kararı sonradan değiştirilmiş; eski ve yeni yükümlülükler tek tarihsiz listede birleştirilmiş. Hangi yöntem daha uygundur?",
            "Yükümlülük kapsamını karar ve değişiklik tarihleriyle ayrı izlemek.",
            "Bütün yükümlülüklerin her tarihte birlikte geçerli olduğunu varsaymak.",
            "Karar değişikliğini yalnız başlık düzeltmesi sayıp uygulama kaydını araştırmamak."),
        "cmk116-122" => (
            "Arama kararında tarif edilen kapsam ile uygulama tutanağındaki yerler aynı görünmüyor. Hangi karşılaştırma gerekir?",
            "Karar/emir kapsamı ile fiilen yapılan arama işlemini ayrı eşleştirmek.",
            "Tutanak bulunduğu için karar kapsamının otomatik genişlediğini kabul etmek.",
            "Aramada eşya bulunmasının kapsam farkını incelemeyi gereksiz kıldığını varsaymak."),
        "cmk123-128" => (
            "Eşyanın iadesi talep edilmiş; yalnız ilk el koyma belgesine bakılarak sonraki bütün işlemlerin cevabının orada olduğu söyleniyor. Hangi ayrım gerekir?",
            "İlk tedbirin kurulması ile muhafaza ve iade sürecini ayrı incelemek.",
            "İlk el koymanın malın sonraki akıbetine ilişkin bütün usulleri tamamladığını varsaymak.",
            "İade talebini, kendiliğinden verilmiş müsadere kararıyla eşitlemek."),
        "cmk128a" => (
            "Özel finansal işlem için yeni tarihli bir düzenleme ileri sürülmüş; dosyanın işlem tarihi daha eski. Nasıl değerlendirilmelidir?",
            "Özel düzenlemenin yürürlük ve geçiş şartlarını dosyanın ilgili tarihleriyle ayrı eşleştirmek.",
            "Özel madde numarasının varlığını her tarihte aynı uygulanabilirliğin kanıtı saymak.",
            "Genel el koyma şablonunu özel düzenleme ve tarih incelemesinin yerine geçirmek."),
        "cmk129-133" => (
            "Postaya ilişkin bir işlem ile avukat bürosuna ilişkin işlem aynı özel usul başlığına yazılmış. Ne yapılmalıdır?",
            "İşlem nesneleri ayrılıp her özel başlığın kendi usulü araştırılmalıdır.",
            "Özel işlem denmesi nedeniyle iki başlığın şartlarının aynı olduğu kabul edilmelidir.",
            "Bir başlığa ilişkin kararın diğer bütün özel el koyma türlerini kapsadığı varsayılmalıdır."),
        "cmk134" => (
            "Karar aslı ve kapsamı mevcut görünüyor; fakat imajın hangi kopyadan üretildiği ve teslim zinciri belirsiz. Hangi inceleme gereklidir?",
            "Hukukî dayanak incelemesinden ayrı olarak teknik bütünlük ve kopya zincirini araştırmak.",
            "Karar bulunduğunda teknik bütünlük ve teslim zincirini incelenmiş saymak.",
            "Hukukî dayanağın varlığını hash değerlerinin eşleştiğinin teknik kanıtı olarak kullanmak."),
        "cmk135-138" => (
            "Tedbir kararı dosyada, ancak alınan kayıtların ne kapsamda tutulduğu ve kullanıldığı belli değil. Hangi yaklaşım doğrudur?",
            "Kararın kurulması ile icra, kayıt ve kullanım sınırlarını ayrı incelemek.",
            "Kararın varlığından bütün kayıtların sınırsız kullanılabileceği sonucunu çıkarmak.",
            "Kayıt elde edilmesinin sonraki kullanım usulünü kendiliğinden tamamladığını kabul etmek."),
        "cmk139-140" => (
            "Bilginin teknik kayıttan mı yoksa görevlendirilen kişinin faaliyetinden mi üretildiği raporda ayrılmamış. Neden sorunludur?",
            "Bilgi kaynağı ve yöntem ayrılmadan her bir işlemin dayanak ve sınırı doğru izlenemez.",
            "Bilgi aynı sonuca işaret ediyorsa yöntem ve kaynak ayrımı önemsizdir.",
            "Yöntemler aynı dosyada geçtiğinde her birine ilişkin usul kaydı birbirinin yerine kullanılabilir."),
        "cmk141-144" => (
            "Tazminat başvurusu verilmiş; talebin kabul edildiğine ilişkin karar yok. Hangi değerlendirme doğrudur?",
            "Başvuru yapılması ile giderim şartlarının ve sonucunun kabul edilmesi ayrı aşamalardır.",
            "Başvuru dilekçesi tazminat miktarının da kesinleştiğini gösterir.",
            "Talepte bulunulması zarar, istisna ve başvuru usulü incelemesini gereksiz kılar."),
        "cmk145-148" => (
            "Tutanağın başlığı sorgu, ancak dinleme kolluk tarafından yapılmış görünüyor. Hangi kavramsal kontrol gerekir?",
            "Tutanağın adına ek olarak dinleyen makam ve işlem usulünü belirleyerek ifade/sorgu ayrımını araştırmak.",
            "Tutanaktaki başlığı her durumda makam ve usul incelemesinin yerine geçirmek.",
            "Dinleme tutanağı bulunmasının bütün dinleme türlerini aynı işlem hâline getirdiğini kabul etmek."),
        "cmk149-156" => (
            "Avukatın seçildiği biliniyor; dosyaya erişim ve savunmaya hazırlanma olanağı tartışılıyor. Hangisi doğru bir ayrımdır?",
            "Avukatın seçilmesi ile savunma yardımının etkili kullanılabilmesi ayrı incelenir.",
            "Seçim kaydı bütün görüşme ve dosya erişimi sorunlarını kendiliğinden çözer.",
            "Etkili savunma imkânı, mağdur vekilliği sıfatının diğer adıdır."),
        "cmk157-169" => (
            "Araştırma planında yalnız iddiayı destekleyen kaynaklar var; aksi yöndeki belirli bir kayıt önerisi dışarıda kalmış. Ne gözden geçirilmelidir?",
            "Lehe ve aleyhe olguların birlikte aydınlatılmasına yönelik araştırma planı.",
            "İhbarın iddia içermesinin yalnız aleyhe araştırmayı yeterli kıldığı varsayımı.",
            "Araştırma yapılmasının herhangi bir hüküm sonucu ile aynı işlem olduğu düşüncesi."),
        "cmk170-174" => (
            "Eğitim dosyasında KYOK ile beraat tek sonuç koduna yazılmış. Hangi ayrım gereklidir?",
            "Soruşturma sonundaki karar ile kovuşturma sonunda verilen hüküm türünü ayırmak.",
            "İkisi de dosyanın sona ermesiyle ilgili olduğu için aynı evre ve karar türü saymak.",
            "KYOK görülmesinin iddianamenin kabulü ve yargılamanın tamamlandığını gösterdiğini kabul etmek."),
        "cmk175-181" => (
            "Hazırlık aşamasında uzman raporu dosyaya girmiş; raporun taraflarca duruşmada tartışıldığına dair kayıt yok. Ne ayrılmalıdır?",
            "Delilin hazırlıkta dosyaya girmesi ile duruşmada ortaya konulup tartışılması.",
            "Hazırlık dosyasındaki her raporun taraflarca tartışılmış sayılması.",
            "Duruşmanın planlanmasının bütün delil değerlendirmelerini tamamlaması."),
        "cmk182-196" => (
            "Oturumun açık olduğu kaydedilmiş, fakat sanığın katılımına ilişkin usul tartışması var. Hangi inceleme kalır?",
            "Açıklık kaydından ayrı olarak sanığın o işlemdeki hazır bulunma şartları.",
            "Açık duruşma kaydının her katılımcının hazır bulunma usulünü tamamlaması.",
            "Açık olmanın yoklukta yapılan bütün işlemlere kendiliğinden aynı sonucu vermesi."),
        "cmk197-200" => (
            "Sanık sonraki oturumda hazır olmuş; önceki yokluğunda hangi işlemlerin yapıldığından haberdar olup olmadığı belirsiz. Ne araştırılır?",
            "Önceki işlemler ile sonradan bilgi edinme ve savunma imkânı ayrı izlenir.",
            "Sonradan hazır olma bütün önceki katılım sorunlarını her durumda geriye dönük çözer.",
            "Müdafi daha önce bulunduysa sanığın sonraki bilgi edinme imkânı incelenmez."),
        "cmk201-206" => (
            "Tutanakta kişinin imzası var; kullanılan dili anlayıp anlamadığı ve dil desteği konusu belirsiz. Hangi değerlendirme doğrudur?",
            "İmza kaydı ile anlamlı katılım ve dil desteği ihtiyacı ayrı incelenmelidir.",
            "İmzanın bulunması dil desteği ve anlama meselesini tek başına kanıtlar.",
            "Delilin dosyada olması kişinin her açıklamayı anladığını da gösterir."),
        "cmk207-218" => (
            "Elde edilme usulüne ilişkin kayıtlar mevcut; ancak anlatım diğer kayıtlarla çelişiyor. Hangi konu ayrıca değerlendirilir?",
            "Delilin güvenilirliği, tutarlılığı ve diğer delillerle ilişkisi.",
            "Usul kaydının bulunması içeriğin mutlaka doğru olduğunu gösterir.",
            "Hukuka uygun elde edilme ile delilin her olguyu kesin ispat etmesi aynı sorudur."),
        "cmk219-222" => (
            "Hükümde sonuç yazılı; itiraz edilen oturum işleminin kaydı görünmüyor. Hangi belge işlevleri ayrılmalıdır?",
            "Duruşma işlemini kaydeden tutanak ile sonuca ulaşmayı açıklayan hüküm gerekçesi.",
            "Hükmün sonuç cümlesinin bütün oturum kayıtlarının yerine geçmesi.",
            "Sonuç yazılmasının usul işlemlerinin kaydını incelemeyi gereksiz kılması."),
        "cmk223-232" => (
            "Karar taslağı yalnız delillerin adlarını sıralıyor; kabul edilen olgular ile sonuç arasında açıklama bulunmuyor. Ne eksiktir?",
            "Delil değerlendirmesini, kabul edilen olguları ve hüküm sonucunu bağlayan gerekçe.",
            "Delil adlarının bulunması her durumda gerekçenin tümünü oluşturur.",
            "Sonuç türünün adı bütün olgu ve delil değerlendirmesinin yerine geçer."),
        "cmk233-236" => (
            "Bir kişi hem mağdur hem şikâyetçi olarak geçiyor; katılma sıfatı ayrıca tartışılıyor. Nasıl öğrenilmelidir?",
            "Kişinin farklı sıfatları ve her sıfatın usulî hakları ayrı belirlenmelidir.",
            "Mağdur, şikâyetçi ve katılan aynı etiketin birbirinin yerine geçen adlarıdır.",
            "Bir sıfat yazılması diğer bütün sıfatlara ilişkin usul kararlarını kanıtlar."),
        "cmk237-243" => (
            "Katılma istemi içeren dilekçe var; istem hakkında kararın ne olduğu belirsiz. Hangi adımlar ayrılır?",
            "İstem, buna ilişkin değerlendirme ve katılma kararının sonucu.",
            "İstem dilekçesi her durumda katılmanın kabulü kararının kendisidir.",
            "Dilekçenin varlığı davadaki sıfata ilişkin bütün aşamaların tamamlandığını gösterir."),
        "cmk244-249" => (
            "Tüzel kişinin temsiline ilişkin eksiklik ile bir gerçek kişinin bulunamaması aynı sorun olarak yazılmış. Hangi düzeltme gerekir?",
            "Temsil sorununu gerçek kişinin gaiplik/kaçaklık durumundan ayrı ele almak.",
            "Tüzel kişi temsilcisinin belirsizliğini her durumda gerçek kişinin kaçaklığıyla eşitlemek.",
            "Muhakemede bulunamayan her unsur için aynı özel kişi sıfatını kullanmak."),
        "cmk251-252" => (
            "Basit yargılama kararına karşı başvuru düşünülüyor; başka özel usulün kabul belgesi tek ölçüt yapılıyor. Hangi inceleme gereklidir?",
            "Basit yargılamanın kendi bildirim, karar ve itiraz rejimini araştırmak.",
            "Diğer usuldeki kabul belgesinin basit yargılamanın bütün başvuru koşullarını tamamlaması.",
            "Özel usullerin başvuru adlarının ve etkilerinin her durumda aynı olması."),
        "cmk256-259" => (
            "Müsadere talebi üçüncü kişinin hak iddiasını da ilgilendiriyor; yalnız eşyanın depoda olduğu kaydedilmiş. Hangi başlık eksiktir?",
            "İlgililerin katılımı ile müsadere talep ve karar usulünün ayrıca incelenmesi.",
            "Depoda bulunma kaydının ilgili kişilerin bütün katılım güvencelerini göstermesi.",
            "El koymanın müsadere muhakemesinin bütün aşamalarını otomatik tamamlaması."),
        "cmk260-266" => (
            "Kişi karardan etkilendiğini söylüyor; hangi sıfatla hangi kanun yoluna başvurabileceği belirlenmemiş. Ne araştırılır?",
            "Karar türü ile kişinin ilgili yola başvuru sıfatı ve usul şartları.",
            "Karardan etkilenme iddiasının bütün kanun yolları için aynı başvuru yetkisini sağlaması.",
            "Başvuru isteğinin karar ve kişi yönünden bütün kabul şartlarının yerine geçmesi."),
        "cmk267-271" => (
            "İtiraz şemasında kararı veren makam ile itirazı inceleyen makam tek kutuya yazılmış. Hangi kontrol gerekir?",
            "Kararı veren makam ile inceleme merciinin rolünü ayrı belirlemek.",
            "Kararı veren makamın her durumda aynı zamanda itiraz mercii olduğunu varsaymak.",
            "Dilekçe verilmesinin merci ayrımını gereksiz kıldığını kabul etmek."),
        "cmk272-285" => (
            "İstinaf başvurusu hakkında usule ilişkin bir sonuç verilmiş; eğitim notu bunun isnadın esası hakkında hüküm olduğunu söylüyor. Hangi ayrım korunmalıdır?",
            "Başvurunun kabul edilebilirliği ile esas incelemesi sonucunu ayırmak.",
            "Usule ilişkin her sonucun maddî isnat hakkında aynı değerlendirme sayılması.",
            "Başvuru hakkındaki her kararın delillerin esas yönünden yeniden değerlendirildiğini göstermesi."),
        "cmk286-307" => (
            "Bozma kararı bulunmuş; sonrasında hangi yargılama işlemlerinin yapılacağı ayrı gösterilmemiş. Hangi yaklaşım doğrudur?",
            "Bozmanın etkisi ile sonraki yargılama usulünü ayrı aşamalar olarak incelemek.",
            "Bozma kararının sonraki bütün yargılama işlemlerinin yapılmış olduğu anlamına gelmesi.",
            "Temyiz ile bozma sonrası yargılamanın aynı makamda aynı işlem olduğunun kabulü."),
        "cmk308-310" => (
            "Bir tarafın dilekçesi, yetkili makamın olağanüstü başvuru kararıyla aynı belge türüne yazılmış. Hangi ayrım gerekir?",
            "İlgilinin talebi ile ilgili olağanüstü yolu işletmeye yetkili makamın işlemini ayırmak.",
            "Tarafın talebini her durumda başsavcılık itirazının kendisi saymak.",
            "Olağanüstü yol adının dilekçede geçmesinin bütün makam şartlarını tamamlaması."),
        "cmk311-323" => (
            "Yenileme başvurusunun incelenmesine geçilmiş; bunun yeni hükmün kurulmasıyla aynı olduğu ileri sürülüyor. Nasıl değerlendirilmelidir?",
            "Kabul edilebilirlik, esas incelemesi, yeniden yargılama ve yeni hüküm ayrı aşamalardır.",
            "Bir incelemeye geçilmesinin yeni hükmü kendiliğinden kurduğu kabul edilmelidir.",
            "Başvurunun varlığı bütün lehe ve aleyhe yenileme sonuçlarını aynı anda doğurur."),
        "cmk324-330" => (
            "Birden fazla isnatla ilgili işlemlerin giderleri tek kalemde toplanmış; isnatların sonuçları farklı. Hangi bağlantı araştırılır?",
            "Giderin hangi işlem ve isnattan doğduğu ile karar sonucuna bağlı yükletme gerekçesi.",
            "Bütün giderlerin bütün isnatlarla her durumda aynı ilişkiye sahip olduğu varsayımı.",
            "Toplam tutarın bilinmesinin işlem ve sonuç bağlantısını incelemeyi gereksiz kılması."),
        "cmk331-335" => (
            "Bir notta yürürlük ve yürütme hükümleri, adlî tatildeki bütün süre hesaplarının yerine kullanılmış. Hangi düzeltme gerekir?",
            "Son hükümlerin farklı işlevlerini ayırıp tatilin etkisini işlem türüyle ayrıca araştırmak.",
            "Yürütme hükmünün her başvuru süresini tek başına belirlediğini kabul etmek.",
            "Son hükümler aynı bölümde olduğu için takvim ve yetki sorularını aynı saymak."),
        "cmk-gecici" => (
            "Düzenlemenin yayım tarihi görülmüş; bazı hükümler için ileri uygulama veya geçiş ihtimali araştırılmamış. Hangi sonuç doğrudur?",
            "Yayım, yürürlük ve ilgili işe uygulanma şartları ayrı belirlenmelidir.",
            "Yayım tarihinin bütün hükümlerin her dosyada aynı anda uygulanmasını zorunlu kıldığı kabul edilmelidir.",
            "Güncel metne ulaşılması bütün eski işlem tarihlerine ilişkin soruları otomatik çözmelidir."),
        _ => throw new InvalidOperationException("Konuya özgü aktarım senaryosu eksik.")
    };
}
