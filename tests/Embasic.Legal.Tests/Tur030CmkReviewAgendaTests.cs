using Embasic.Legal.Core;
using Xunit;

namespace Embasic.Legal.Tests;

public sealed class Tur030CmkReviewAgendaTests
{
    private static IReadOnlyList<CmkReviewAgendaItem> Make(
        HashSet<string>? answered = null,
        HashSet<string>? firstCorrect = null,
        HashSet<string>? recovered = null,
        CmkStudySession? study = null,
        CmkLearningChecklistSession? check = null,
        IReadOnlyList<CmkLearningTopic>? topics = null)
    {
        study ??= new CmkStudySession();
        check ??= new CmkLearningChecklistSession();
        return CmkReviewAgendaPlanner.Build(topics ?? CmkCurriculum.CentralTopics,
            answered ?? new HashSet<string>(StringComparer.Ordinal),
            firstCorrect ?? new HashSet<string>(StringComparer.Ordinal),
            recovered ?? new HashSet<string>(StringComparer.Ordinal),
            study.Rating, check.ReviewedCount);
    }

    [Fact]
    public void UntouchedCurriculumIsNotMislabelledAsWeak()
    {
        Assert.Equal(47, CmkCurriculum.CentralTopics.Count);
        Assert.Empty(Make());
    }

    [Fact]
    public void FirstIncorrectAnswerCreatesOneTargetedReviewItem()
    {
        var agenda = Make(answered: new(["cmk74:0"]));
        var item = Assert.Single(agenda);
        Assert.Equal("cmk74", item.TopicKey);
        Assert.Equal(1, item.IncorrectFirstAnswers);
        Assert.Equal(1, item.UnansweredQuestions);
        Assert.Contains("İlk yanıtta yanlış: 1", item.ReviewReason);
        Assert.Equal("NOT_VERIFIED", item.SourceStatus);
        Assert.False(item.LegalComplianceCertified);
    }

    [Fact]
    public void LaterCorrectAnswerDoesNotRewriteFirstAttempt()
    {
        var agenda = Make(answered:new(["cmk134:0"]),
            recovered:new(["cmk134:0"]));
        var item = Assert.Single(agenda);
        Assert.Equal(1, item.IncorrectFirstAnswers);
        Assert.Equal(1, item.CorrectAfterFeedback);
        Assert.False(item.LegalComplianceCertified);
    }

    [Fact]
    public void ExplicitReviewCardSurfacesASeparateTopicWithoutCaseData()
    {
        var study = new CmkStudySession();
        study.Reveal("cmk78-80",0);
        study.Rate("cmk78-80",0,CmkRecallRating.Review);
        var item = Assert.Single(Make(study:study));
        Assert.Equal("cmk78-80",item.TopicKey);
        Assert.Equal(1,item.RecallCardsMarkedReview);
        Assert.Equal(0,item.IncorrectFirstAnswers);
    }

    [Fact]
    public void SelfRatedRememberedCardDoesNotImplyLegalApprovalOrWeakness()
    {
        var study = new CmkStudySession();
        study.Reveal("cmk74",0);
        study.Rate("cmk74",0,CmkRecallRating.Remembered);
        Assert.Empty(Make(study:study));
        Assert.False(study.LegalComplianceCertified);
    }

    [Fact]
    public void OnlyStartedIncompleteChecklistAppears()
    {
        var checkedSession = new CmkLearningChecklistSession();
        checkedSession.SetReviewed("cmk74",1,true);
        var item = Assert.Single(Make(check:checkedSession));
        Assert.Equal("cmk74",item.TopicKey);
        Assert.Equal(1,item.CheckedSteps);
        Assert.Equal(4,item.TotalSteps);
        Assert.Equal(2,item.UnansweredQuestions);
    }

    [Fact]
    public void FullyCorrectCompletedQuestionsWithoutReviewMarksDoNotAppear()
    {
        var both=new HashSet<string>(StringComparer.Ordinal) { "cmk74:0","cmk74:1" };
        Assert.Empty(Make(answered:both,firstCorrect:both));
    }

    [Fact]
    public void IncorrectFirstTakesPriorityOverReviewCardsAndPendingQuestions()
    {
        var study=new CmkStudySession();
        study.Reveal("cmk134",0);
        study.Rate("cmk134",0,CmkRecallRating.Review);
        var result=Make(answered:new(["cmk74:0"]),study:study);
        Assert.Equal(new[]{"cmk74","cmk134"},result.Select(i=>i.TopicKey));
    }

    [Fact]
    public void ResetRemovesAllAgendaItemsAndChangesNoSourceStatuses()
    {
        var study=new CmkStudySession();
        var marks=new CmkLearningChecklistSession();
        marks.SetReviewed("cmk74",2,true);
        study.Reveal("cmk134",0);
        study.Rate("cmk134",0,CmkRecallRating.Review);
        Assert.Equal(2,Make(study:study,check:marks).Count);
        study.Reset();
        marks.Reset();
        Assert.Empty(Make(study:study,check:marks));
        Assert.All(CmkCurriculum.CentralTopics,t=>Assert.False(t.IsCurrentLawVerified));
    }

    [Fact]
    public void CentralReviewRejectsAnyAlternativeSolutionsTopic()
    {
        Assert.Throws<InvalidOperationException>(()=>
            Make(topics:CmkCurriculum.AlternativeCategory.Topics));
    }

    [Fact]
    public void DuplicateTopicKeysAndInvalidReviewCountsAreRejected()
    {
        var topic=CmkCurriculum.CentralTopics[0];
        Assert.Throws<InvalidOperationException>(()=>
            Make(topics:new[]{topic,topic}));
        Assert.Throws<InvalidOperationException>(()=>
            CmkReviewAgendaPlanner.Build(new[]{topic},new HashSet<string>(),
                new HashSet<string>(),new HashSet<string>(),
                (_,_)=>CmkRecallRating.NotRated,_=>topic.Flow.Count+1));
    }

    [Fact]
    public void PlanningDoesNotMutateExistingQuizOrCardState()
    {
        var answered=new HashSet<string>(StringComparer.Ordinal){"cmk74:0"};
        var study=new CmkStudySession();
        study.Reveal("cmk134",1);
        study.Rate("cmk134",1,CmkRecallRating.Review);
        _=Make(answered:answered,study:study);
        Assert.Single(answered);
        Assert.True(study.IsRevealed("cmk134",1));
        Assert.Equal(CmkRecallRating.Review,study.Rating("cmk134",1));
        Assert.Equal(94,CmkCurriculum.CentralTopics.Sum(x=>x.Quiz.Count));
    }
}
