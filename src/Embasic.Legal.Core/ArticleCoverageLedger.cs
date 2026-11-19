using System.Security.Cryptography;
using System.Text;

namespace Embasic.Legal.Core;

public sealed record ArticlePageFragment(int PageNumber, string ArticleId, string FragmentSha256);

/// <summary>
/// A provisional coverage ledger from extracted source pages and a separately enumerated
/// expected-article manifest. Consistency != official origin, correctness or legal effect.
/// </summary>
public sealed record ArticleCoverageReview(
    IReadOnlyList<string> MissingArticles,
    IReadOnlyList<string> UnlistedArticles,
    IReadOnlyList<string> DuplicatePageEntries,
    bool StructuralConsistencyOnly)
{
    public bool OfficialFullTextVerified => false;
    public bool HistoricalEffectiveDateVerified => false;
    public bool NoteBindingAuthorized => false;
}

public static class ArticleCoverageLedger
{
    public static ArticleCoverageReview Audit(
        IReadOnlyCollection<string> expectedIds,
        IReadOnlyCollection<ArticlePageFragment> extracted)
    {
        if (expectedIds is null || extracted is null)
            throw new ArgumentNullException("Eksik kaynak veya madde manifesti.");
        if (expectedIds.Count == 0)
            throw new InvalidDataException("Boş madde manifesti geçerli bir kapsam kanıtı değildir.");
        if (expectedIds.Any(string.IsNullOrWhiteSpace) ||
            expectedIds.Distinct(StringComparer.Ordinal).Count() != expectedIds.Count)
            throw new InvalidDataException("Tekrarlanan veya boş beklenen madde kimliği.");
        foreach (var item in extracted)
        {
            if (item.PageNumber <= 0 || string.IsNullOrWhiteSpace(item.ArticleId) ||
                item.FragmentSha256.Length != 64 || !item.FragmentSha256.All(Uri.IsHexDigit))
                throw new InvalidDataException("Madde-sayfa parçasında bozuk kimlik/hash var.");
        }

        var expected = expectedIds.ToHashSet(StringComparer.Ordinal);
        var found = extracted.Select(x => x.ArticleId).ToHashSet(StringComparer.Ordinal);
        var duplicateEntries = extracted.GroupBy(x => (x.PageNumber, x.ArticleId))
            .Where(x => x.Count() > 1)
            .Select(x => x.Key.PageNumber.ToString() + ":" + x.Key.ArticleId)
            .OrderBy(x => x, StringComparer.Ordinal).ToList();
        var missing = expected.Except(found, StringComparer.Ordinal)
            .OrderBy(x => x, StringComparer.Ordinal).ToList();
        var unknown = found.Except(expected, StringComparer.Ordinal)
            .OrderBy(x => x, StringComparer.Ordinal).ToList();

        return new ArticleCoverageReview(missing, unknown, duplicateEntries,
            missing.Count == 0 && unknown.Count == 0 && duplicateEntries.Count == 0);
    }

    public static ArticlePageFragment Fragment(int physicalPage, string articleId, string text) =>
        new(physicalPage, articleId,
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))).ToLowerInvariant());
}
