namespace Embasic.Legal.Core;

/// <summary>
/// Per-tool input revision guard. A result can only be copied after an explicit
/// successful calculation for the latest input revision. Not legal certification.
/// </summary>
public sealed class ArithmeticDraftGuard
{
    public long InputRevision { get; private set; }
    private long _resultRevision = -1;
    public bool HasCurrentResult => _resultRevision == InputRevision;

    public void InputChanged()
    {
        // If revisions ever overflow, fail closed rather than retaining an old
        // 'current' result (practically unreachable but keeps the invariant).
        if (InputRevision == long.MaxValue)
        {
            InputRevision = 0;
            _resultRevision = -1;
            return;
        }
        InputRevision++;
        _resultRevision = -1;
    }

    public void CalculationSucceeded() => _resultRevision = InputRevision;
}
