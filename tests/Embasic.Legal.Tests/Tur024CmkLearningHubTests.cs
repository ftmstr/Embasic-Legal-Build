using Embasic.Legal.Core;
using Xunit;

namespace Embasic.Legal.Tests;

public sealed class Tur024CmkLearningHubTests
{
    [Fact]
    public void TopicCatalogueCoversCmk74AndPreviousPracticeModulesWithoutDuplicates()
    {
        var topics = CmkLearningCatalog.All;
        Assert.Equal(13, topics.Count);
        Assert.Equal(13, topics.Select(t => t.Key).Distinct(StringComparer.Ordinal).Count());
        foreach (string key in new[]
        {
            "cmk74", "cmk75-77", "cmk78-80", "cmk81-89", "cmk90-99",
            "cmk100-108", "cmk109-115", "cmk116-122", "cmk123-128",
            "cmk128a", "cmk134", "cmk250-253", "tck75"
        })
            Assert.NotNull(CmkLearningCatalog.Find(key));
    }

    [Fact]
    public void EveryTopicHasFourFlowStepsFourTableRowsAndTwoInteractiveQuizzes()
    {
        Assert.All(CmkLearningCatalog.All, topic =>
        {
            Assert.Equal(4, topic.Flow.Count);
            Assert.Equal(4, topic.Matrix.Count);
            Assert.Equal(2, topic.Quiz.Count);
            Assert.Equal(new[] {1,2,3,4}, topic.Flow.Select(x=>x.Order));
            Assert.All(topic.Flow, s => {
                Assert.False(string.IsNullOrWhiteSpace(s.ReviewPrompt));
                Assert.False(string.IsNullOrWhiteSpace(s.DocumentToInspect));
                Assert.Contains("İNSAN KONTROLÜ", s.Gate);
            });
            Assert.All(topic.Matrix, row =>
                Assert.False(string.IsNullOrWhiteSpace(row.UnresolvedRisk)));
        });
    }

    [Fact]
    public void SourceAndLegalApprovalStayFailClosedForEveryModule()
    {
        Assert.Equal("https://www.mevzuat.gov.tr/", CmkLearningCatalog.CanonicalSource);
        Assert.StartsWith(CmkLearningCatalog.CanonicalSource,CmkLearningCatalog.CurrentCmkUrl);
        Assert.StartsWith(CmkLearningCatalog.CanonicalSource,CmkLearningCatalog.CurrentTckUrl);
        Assert.All(CmkLearningCatalog.All, topic =>
        {
            Assert.Equal("NOT_VERIFIED",topic.SourceStatus);
            Assert.False(topic.IsCurrentLawVerified);
            Assert.Contains("NOT_VERIFIED",topic.SourceNotice);
            Assert.DoesNotContain("tbmm.gov.tr",topic.SourceNotice,StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public void CorrectTrainingAnswersStillCannotAuthorizeAnyMeasure()
    {
        foreach (var topic in CmkLearningCatalog.All)
        for (int i=0;i<topic.Quiz.Count;i++)
        {
            var question=topic.Quiz[i];
            Assert.Equal(3,question.Choices.Count);
            Assert.InRange(question.CorrectIndex,0,2);
            var correct=CmkLearningCatalog.Assess(topic.Key,i,question.CorrectIndex);
            Assert.True(correct.Correct);
            Assert.False(correct.LegalComplianceCertified);
            Assert.Equal("NOT_VERIFIED",correct.SourceStatus);
            Assert.Contains("NOT_VERIFIED",correct.Feedback);
            Assert.Contains("KARAR/YETKİ ONAYI YOK",correct.Feedback);
            var wrong=CmkLearningCatalog.Assess(topic.Key,i,(question.CorrectIndex+1)%3);
            Assert.False(wrong.Correct);
            Assert.False(wrong.LegalComplianceCertified);
            Assert.Equal("NOT_VERIFIED",wrong.SourceStatus);
        }
    }

    [Fact]
    public void QuizzesAreProceduralLearningRatherThanInventedLawText()
    {
        foreach (var topic in CmkLearningCatalog.All)
        {
            Assert.True(topic.ArticleLocator.StartsWith("CMK",StringComparison.Ordinal) || topic.ArticleLocator.StartsWith("TCK",StringComparison.Ordinal));
            Assert.False(string.IsNullOrWhiteSpace(topic.Flow[0].DocumentToInspect));
            Assert.DoesNotContain(topic.Quiz,q=>q.CorrectIndex<0);
            Assert.Contains("NOT_VERIFIED",topic.SourceNotice);
        }
    }

    [Theory]
    [InlineData("cmk74",-1,0)]
    [InlineData("cmk74",2,0)]
    [InlineData("cmk74",0,-1)]
    [InlineData("cmk74",0,3)]
    public void AssessmentRejectsOutOfRangeIndices(string topic,int question,int option)
    {
        Assert.Throws<ArgumentOutOfRangeException>(()=>
            CmkLearningCatalog.Assess(topic,question,option));
    }

    [Fact]
    public void MissingTopicsAreNeverSilentlyMappedToAnotherStatute()
    {
        Assert.Throws<InvalidOperationException>(()=>
            CmkLearningCatalog.Find("cmk-unknown-synthetic"));
    }

    [Fact]
    public void AlternativeSolutionsAndCmk134RemainUnverified()
    {
        foreach (string id in new[]{"cmk134","cmk128a","cmk250-253","tck75"})
        {
            var topic=CmkLearningCatalog.Find(id);
            Assert.False(topic.IsCurrentLawVerified);
            Assert.Contains("NOT_VERIFIED",topic.SourceNotice);
        }
    }
}
