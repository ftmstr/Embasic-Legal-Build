using Embasic.Legal.Core;
using Xunit;

namespace Embasic.Legal.Tests;

public sealed class Tur010Cmk134AymAndTimelineTests
{
    [Fact]
    public void GazetteEntryOnlyDoesNotCertifyFullDecisionOrContemporaryStatute()
    {
        var a = Cmk134ConstitutionalCourtGate.Assess(new DateOnly(2026, 11, 20));
        Assert.Equal(Cmk134RiskPeriod.PublishedBeforeDeferredDate, a.Period);
        Assert.False(a.CompleteCourtDecisionVerified);
        Assert.False(a.CurrentLegislationAfterJudgmentVerified);
        Assert.False(a.OfficialCaseDateConsolidationVerified);
        Assert.False(a.EnablesDigitalSearchAuthorization);
        Assert.Contains("25.02.2027", a.Warning);
    }

    [Theory]
    [InlineData(2026, 5, 24, Cmk134RiskPeriod.PrePublication)]
    [InlineData(2026, 5, 25, Cmk134RiskPeriod.PublishedBeforeDeferredDate)]
    [InlineData(2027, 2, 24, Cmk134RiskPeriod.PublishedBeforeDeferredDate)]
    [InlineData(2027, 2, 25, Cmk134RiskPeriod.OnOrAfterDeferredDate)]
    [InlineData(2028, 1, 1, Cmk134RiskPeriod.OnOrAfterDeferredDate)]
    public void DateBoundariesOnlyChangeRiskPeriodAndNeverGrantLegalPermission(
        int year, int month, int day, Cmk134RiskPeriod expected)
    {
        var a = Cmk134ConstitutionalCourtGate.Assess(new DateOnly(year, month, day));
        Assert.Equal(expected, a.Period);
        Assert.False(a.LegalStatusAutomaticallyCertified);
        Assert.False(a.EnablesDigitalSearchAuthorization);
    }

    [Fact]
    public void MissingCaseDateIsFailClosed()
    {
        var a = Cmk134ConstitutionalCourtGate.Assess(null);
        Assert.Equal(Cmk134RiskPeriod.UnknownCaseDate, a.Period);
        Assert.False(a.CompleteCourtDecisionVerified);
        Assert.Contains("BAŞLATILAMADI", a.Warning);
    }
}
