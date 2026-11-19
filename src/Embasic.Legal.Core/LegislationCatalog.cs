namespace Embasic.Legal.Core;

// Source-directory metadata only: NOT law text and NOT proof of current legislation.
public sealed record LawDirectoryEntry(string Number, string Title);
public static class LegislationCatalog
{
    public const bool OfficialFullTextVerified = false;
    public static IReadOnlyList<LawDirectoryEntry> InitialDirectory { get; } =
    [
        new("5237", "Türk Ceza Kanunu"),
        new("5271", "Ceza Muhakemesi Kanunu"),
        new("YON-ARAMA", "Adlî ve Önleme Aramaları Yönetmeliği"),
        new("5607", "Kaçakçılıkla Mücadele Kanunu"),
        new("6136", "Ateşli Silahlar ve Bıçaklar ile Diğer Aletler Hakkında Kanun"),
        new("2918", "Karayolları Trafik Kanunu"),
        new("2559", "Polis Vazife ve Salâhiyet Kanunu"),
        new("2313", "Uyuşturucu Maddelerin Murakabesi Hakkında Kanun"),
        new("5395", "Çocuk Koruma Kanunu"),
        new("2863", "Kültür ve Tabiat Varlıklarını Koruma Kanunu"),
        new("5275", "Ceza ve Güvenlik Tedbirlerinin İnfazı Hakkında Kanun"),
        new("6222", "Sporda Şiddet ve Düzensizliğin Önlenmesine Dair Kanun"),
        new("3713", "Terörle Mücadele Kanunu"),
        new("6114", "Ölçme, Seçme ve Yerleştirme Merkezi Hizmetleri Hakkında Kanun"),
        new("1632", "Askerî Ceza Kanunu")
    ];
}
