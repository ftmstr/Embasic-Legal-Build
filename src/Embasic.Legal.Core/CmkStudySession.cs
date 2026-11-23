namespace Embasic.Legal.Core;

public enum CmkRecallRating { NotRated, Remembered, Review }

/// <summary>RAM-only study activity; no claim of mastery, official law or legal approval.</summary>
public sealed class CmkStudySession
{
    private readonly HashSet<(string Key, int Card)> _revealed = [];
    private readonly Dictionary<(string Key, int Card), CmkRecallRating> _ratings = [];
    private readonly Dictionary<string, int> _branches = new(StringComparer.Ordinal);
    private static void RequireCard(string key, int card)
    {
        var profile = CmkStudyCatalog.Find(key);
        if (card < 0 || card >= profile.Cards.Count) throw new ArgumentOutOfRangeException(nameof(card));
    }
    public void Reveal(string key, int card) { RequireCard(key, card); _revealed.Add((key, card)); }
    public bool IsRevealed(string key, int card) { RequireCard(key, card); return _revealed.Contains((key, card)); }
    public void Rate(string key, int card, CmkRecallRating rating)
    {
        RequireCard(key, card);
        if (!Enum.IsDefined(rating)) throw new ArgumentOutOfRangeException(nameof(rating));
        if (!_revealed.Contains((key, card))) throw new InvalidOperationException("Önce cevabı açıp karşılaştırın.");
        _ratings[(key, card)] = rating;
    }
    public CmkRecallRating Rating(string key, int card)
    {
        RequireCard(key, card);
        return _ratings.GetValueOrDefault((key, card), CmkRecallRating.NotRated);
    }
    public void SelectBranch(string key, int option)
    {
        CmkStudyCatalog.AssessBranch(key, option); // validates profile and option
        _branches[key] = option;
    }
    public int? Branch(string key)
    {
        CmkStudyCatalog.Find(key);
        return _branches.TryGetValue(key, out var option) ? option : null;
    }
    public void Reset() { _revealed.Clear(); _ratings.Clear(); _branches.Clear(); }
    public bool LegalComplianceCertified => false;
}
