using Embasic.Legal.Core;
using Xunit;

namespace Embasic.Legal.Tests;

public sealed class Tur025LearningChecklistSessionTests
{
    [Fact]
    public void SwitchingTopicAndReturningPreservesIndependentReviewMarks()
    {
        var state = new CmkLearningChecklistSession();
        state.SetReviewed("cmk74", 1, true);
        state.SetReviewed("cmk74", 3, true);
        Assert.Equal(2,state.ReviewedCount("cmk74"));
        // Simulate choosing a different topic and then coming back.
        Assert.False(state.IsReviewed("cmk134", 1));
        state.SetReviewed("cmk134", 2, true);
        Assert.Equal(1,state.ReviewedCount("cmk134"));
        Assert.Equal(2,state.ReviewedCount("cmk74"));
        Assert.True(state.IsReviewed("cmk74",1));
        Assert.True(state.IsReviewed("cmk74",3));
    }

    [Fact]
    public void UncheckingOneTopicDoesNotEraseDifferentTopicProgress()
    {
        var s=new CmkLearningChecklistSession();
        s.SetReviewed("cmk74",1,true);
        s.SetReviewed("cmk134",1,true);
        s.SetReviewed("cmk74",1,false);
        Assert.Equal(0,s.ReviewedCount("cmk74"));
        Assert.True(s.IsReviewed("cmk134",1));
    }

    [Fact]
    public void ResetClearsAllTopicsAndNeverChangesOfficialSourceStatus()
    {
        var s=new CmkLearningChecklistSession();
        foreach(var topic in CmkLearningCatalog.All)
            s.SetReviewed(topic.Key,1,true);
        Assert.All(CmkLearningCatalog.All,
            topic=>Assert.Equal(1,s.ReviewedCount(topic.Key)));
        s.Reset();
        Assert.All(CmkLearningCatalog.All,
            topic=>Assert.Equal(0,s.ReviewedCount(topic.Key)));
        Assert.False(s.LegalComplianceCertified);
        Assert.Equal("NOT_VERIFIED",s.OfficialLawSourceStatus);
    }

    [Fact]
    public void ReviewingAllStepsCannotAuthorizeAnyLegalMeasure()
    {
        var state=new CmkLearningChecklistSession();
        foreach (var topic in CmkLearningCatalog.All)
            foreach (var step in topic.Flow)
                state.SetReviewed(topic.Key,step.Order,true);
        Assert.All(CmkLearningCatalog.All,
            topic=>Assert.Equal(topic.Flow.Count,state.ReviewedCount(topic.Key)));
        Assert.False(state.LegalComplianceCertified);
        Assert.Equal("NOT_VERIFIED",state.OfficialLawSourceStatus);
    }

    [Fact]
    public void RepeatedTrueAndFalseMarksAreIdempotent()
    {
        var s=new CmkLearningChecklistSession();
        s.SetReviewed("cmk74",1,true);
        s.SetReviewed("cmk74",1,true);
        Assert.Equal(1,s.ReviewedCount("cmk74"));
        s.SetReviewed("cmk74",1,false);
        s.SetReviewed("cmk74",1,false);
        Assert.Equal(0,s.ReviewedCount("cmk74"));
    }

    [Theory]
    [InlineData("cmk74",0)]
    [InlineData("cmk74",5)]
    [InlineData("cmk134",-1)]
    public void UnknownStepNumbersFailClosed(string key,int order)
    {
        var s=new CmkLearningChecklistSession();
        Assert.Throws<ArgumentOutOfRangeException>(()=>s.SetReviewed(key,order,true));
        Assert.Throws<ArgumentOutOfRangeException>(()=>s.IsReviewed(key,order));
    }

    [Fact]
    public void UnknownTopicAndEmptyKeyFailClosed()
    {
        var s=new CmkLearningChecklistSession();
        Assert.Throws<InvalidOperationException>(()=>s.SetReviewed("synthetic-nonexistent",1,true));
        Assert.Throws<ArgumentException>(()=>s.SetReviewed("",1,true));
    }

    [Fact]
    public void PerTopicChecklistDoesNotAlterTwentySixQuizQuestions()
    {
        var s=new CmkLearningChecklistSession();
        int expected=CmkLearningCatalog.All.Sum(x=>x.Quiz.Count);
        Assert.Equal(26,expected);
        s.SetReviewed("cmk74",2,true);
        Assert.Equal(26,CmkLearningCatalog.All.Sum(x=>x.Quiz.Count));
        var answer=CmkLearningCatalog.Assess("cmk74",0,1);
        Assert.True(answer.Correct);
        Assert.False(answer.LegalComplianceCertified);
        Assert.True(s.IsReviewed("cmk74",2));
    }
}
