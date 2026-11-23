using Embasic.Legal.Core;
using Xunit;

namespace Embasic.Legal.Tests;

public sealed class Tur029CmkActiveLearningTests
{
    [Fact]
    public void EveryCentralTopicHasOneWorkshopAndAlternativesRemainSeparate()
    {
        Assert.Equal(47, CmkStudyCatalog.All.Count);
        Assert.Equal(CmkCurriculum.CentralTopics.Select(t => t.Key).Order(),
            CmkStudyCatalog.All.Select(t => t.TopicKey).Order());
        Assert.All(CmkCurriculum.AlternativeCategory.TopicKeys, key => Assert.Null(CmkStudyCatalog.TryFind(key)));
        Assert.Null(CmkStudyCatalog.TryFind("cmk250-253"));
    }

    [Fact]
    public void EveryWorkshopHasDistinctComparisonCardsFourStepsAndTwoQuestions()
    {
        foreach (var profile in CmkStudyCatalog.All)
        {
            Assert.Equal(3, profile.Comparison.Count);
            Assert.Equal(3, profile.Cards.Count);
            Assert.Equal(3, profile.Objectives.Count);
            Assert.Equal(4, profile.Flow.Count);
            Assert.Equal(new[] { 1, 2, 3, 4 }, profile.Flow.Select(s => s.Order));
            Assert.Equal(2, profile.Quiz.Count);
            Assert.NotEqual(profile.LeftLabel, profile.RightLabel);
            Assert.All(profile.Comparison, row =>
            {
                Assert.NotEqual(row.Left, row.Right);
                Assert.True(row.Reason.Length > 30);
            });
            Assert.All(profile.Cards, card =>
            {
                Assert.Contains(profile.LeftLabel, card.Question);
                Assert.Contains(profile.RightLabel, card.Answer);
                Assert.False(string.IsNullOrWhiteSpace(card.ArticleLocator));
            });
        }
        Assert.Equal(141, CmkStudyCatalog.All.Sum(p => p.Cards.Count));
        Assert.Equal(94, CmkStudyCatalog.All.Sum(p => p.Quiz.Count));
    }

    [Fact]
    public void EveryChoiceHasItsOwnFeedbackAndExactlyOneCorrectAnswer()
    {
        foreach (var profile in CmkStudyCatalog.All)
        for (int q = 0; q < profile.Quiz.Count; q++)
        {
            var question = profile.Quiz[q];
            Assert.Equal(3, question.Options.Select(o => o.Text).Distinct().Count());
            Assert.Equal(3, question.Options.Select(o => o.Explanation).Distinct().Count());
            int correct = 0;
            for (int option = 0; option < question.Options.Count; option++)
            {
                var result = CmkStudyCatalog.AssessQuiz(profile.TopicKey, q, option);
                if (result.Correct) correct++;
                Assert.Contains(question.Options[option].Explanation, result.Feedback);
                Assert.False(result.LegalComplianceCertified);
                Assert.Equal("NOT_VERIFIED", result.SourceStatus);
            }
            Assert.Equal(1, correct);
        }
    }

    [Fact]
    public void AssessmentUsesNewScenariosRatherThanReplayingGuidedAnswers()
    {
        foreach (var profile in CmkStudyCatalog.All)
        {
            Assert.NotSame(profile.BranchCase, profile.Quiz[0]);
            Assert.NotEqual(profile.BranchCase.Prompt, profile.Quiz[0].Prompt);
            Assert.DoesNotContain(profile.Quiz[0].Prompt, profile.BranchCase.Prompt);
        }
        var defense = CmkStudyCatalog.Find("cmk149-156").Comparison[2];
        Assert.Contains("Savunma yardımının", defense.Right);
        Assert.DoesNotContain("Mağdur vekilliği", defense.Right);
    }

    [Fact]
    public void CorrectAnswerLocationsAreNotAlwaysTheSameColumn()
    {
        Assert.Equal(new[] { 0, 1, 2 }, CmkStudyCatalog.All.Select(p => p.BranchCase.CorrectIndex).Distinct().Order());
        Assert.Equal(new[] { 0, 1, 2 }, CmkStudyCatalog.All.Select(p => p.Quiz[1].CorrectIndex).Distinct().Order());
    }

    [Fact]
    public void RecallRequiresOpeningAnswerAndRatingsAreIndependentAcrossTopics()
    {
        var session = new CmkStudySession();
        Assert.Throws<InvalidOperationException>(() => session.Rate("cmk1-2", 0, CmkRecallRating.Remembered));
        session.Reveal("cmk1-2", 0);
        session.Rate("cmk1-2", 0, CmkRecallRating.Remembered);
        Assert.True(session.IsRevealed("cmk1-2", 0));
        Assert.False(session.IsRevealed("cmk134", 0));
        Assert.Equal(CmkRecallRating.NotRated, session.Rating("cmk134", 0));
    }

    [Fact]
    public void RepeatedRatingReplacesSelfAssessmentAndResetClearsEveryTopic()
    {
        var session = new CmkStudySession();
        foreach (var profile in CmkStudyCatalog.All)
        {
            session.Reveal(profile.TopicKey, 0);
            session.Rate(profile.TopicKey, 0, CmkRecallRating.Remembered);
            session.Rate(profile.TopicKey, 0, CmkRecallRating.Remembered);
            session.Rate(profile.TopicKey, 0, CmkRecallRating.Review);
            Assert.Equal(CmkRecallRating.Review, session.Rating(profile.TopicKey, 0));
        }
        session.Reset();
        Assert.All(CmkStudyCatalog.All, p =>
        {
            Assert.False(session.IsRevealed(p.TopicKey, 0));
            Assert.Equal(CmkRecallRating.NotRated, session.Rating(p.TopicKey, 0));
        });
        Assert.False(session.LegalComplianceCertified);
    }

    [Fact]
    public void BranchSelectionsSurviveNavigationAndCanBeReconsideredWithoutLegalApproval()
    {
        var session = new CmkStudySession();
        session.SelectBranch("cmk134", 0);
        session.SelectBranch("cmk1-2", 2);
        Assert.Equal(0, session.Branch("cmk134"));
        session.SelectBranch("cmk134", 1);
        Assert.Equal(1, session.Branch("cmk134"));
        Assert.Equal(2, session.Branch("cmk1-2"));
        Assert.False(session.LegalComplianceCertified);
        session.Reset();
        Assert.Null(session.Branch("cmk134"));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    public void RecallAndBranchRejectInvalidIndices(int value)
    {
        var session = new CmkStudySession();
        Assert.Throws<ArgumentOutOfRangeException>(() => session.Reveal("cmk1-2", value));
        Assert.Throws<ArgumentOutOfRangeException>(() => session.SelectBranch("cmk1-2", value));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(2)]
    public void QuizRejectsInvalidQuestionIndices(int value) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => CmkStudyCatalog.AssessQuiz("cmk1-2", value, 0));

    [Fact]
    public void UnknownOrAlternativeProfilesNeverFallBackToAnUnrelatedLesson()
    {
        Assert.Throws<InvalidOperationException>(() => CmkStudyCatalog.Find("missing"));
        Assert.Throws<InvalidOperationException>(() => CmkStudyCatalog.AssessBranch("tck75", 0));
        Assert.Throws<InvalidOperationException>(() => new CmkStudySession().Reveal("cmk250", 0));
    }

    [Fact]
    public void HighConfusionExamplesDoNotPromoteTechnicalOrTemporaryFactsToLegalConclusions()
    {
        static string Correct(string key)
        {
            var question = CmkStudyCatalog.Find(key).BranchCase;
            return question.Options[question.CorrectIndex].Text;
        }
        Assert.Contains("ayrı tutulmalıdır", Correct("cmk134"));
        Assert.Contains("tek başına göstermez", Correct("cmk123-128"));
        Assert.Contains("ayrı değerlendirilir", Correct("cmk170-174"));
        Assert.Contains("ayrı incelenmelidir", Correct("cmk197-200"));
    }
}
