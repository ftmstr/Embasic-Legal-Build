namespace Embasic.Legal.Core;

/// <summary>
/// Ephemeral, per-topic checked-step memory for the CMK 74+ learning hub.
/// It is deliberately independent from legal sources and quiz grades:
/// marking any or all steps does not authenticate evidence or authorize an
/// investigative measure. No disk, cloud or system credential storage.
/// </summary>
public sealed class CmkLearningChecklistSession
{
    private readonly Dictionary<string, HashSet<int>> _marks = new(StringComparer.Ordinal);

    private static CmkLearningTopic RequireStep(string key, int order)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Eğitim konu anahtarı zorunludur.", nameof(key));
        var topic = CmkLearningCatalog.Find(key); // Unknown topic fails closed.
        if (!topic.Flow.Any(step => step.Order == order))
            throw new ArgumentOutOfRangeException(nameof(order), "Bu konuda böyle bir kontrol adımı yok.");
        return topic;
    }

    public void SetReviewed(string topicKey, int order, bool reviewed)
    {
        RequireStep(topicKey, order);
        if (reviewed)
        {
            if (!_marks.TryGetValue(topicKey, out var set))
                _marks[topicKey] = set = new HashSet<int>();
            set.Add(order);
        }
        else if (_marks.TryGetValue(topicKey, out var set))
        {
            set.Remove(order);
            if (set.Count == 0) _marks.Remove(topicKey);
        }
    }

    public bool IsReviewed(string topicKey, int order)
    {
        RequireStep(topicKey, order);
        return _marks.TryGetValue(topicKey, out var set) && set.Contains(order);
    }

    public int ReviewedCount(string topicKey)
    {
        var topic = CmkLearningCatalog.Find(topicKey);
        return _marks.TryGetValue(topic.Key, out var set)
            ? set.Count : 0;
    }

    public void Reset() => _marks.Clear();
    public bool LegalComplianceCertified => false;
    public string OfficialLawSourceStatus => "NOT_VERIFIED";
}
