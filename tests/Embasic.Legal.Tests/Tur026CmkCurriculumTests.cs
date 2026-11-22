using System.Text.RegularExpressions;
using Embasic.Legal.Core;
using Xunit;

namespace Embasic.Legal.Tests;

public sealed class Tur026CmkCurriculumTests
{
    [Fact]
    public void CategoriesOwnEveryTopicExactlyOnceAndRetainLegacyObjectIdentity()
    {
        Assert.Equal(13, CmkCurriculum.Categories.Count);
        Assert.Equal(50, CmkCurriculum.All.Count);
        Assert.Equal(47, CmkCurriculum.CentralTopics.Count);
        var keys = CmkCurriculum.AllCategories.SelectMany(x => x.TopicKeys).ToArray();
        Assert.Equal(keys.Length, keys.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(CmkCurriculum.All.Select(x => x.Key), keys);
        foreach (var category in CmkCurriculum.AllCategories)
        {
            Assert.Same(category, CmkCurriculum.FindCategory(category.Key));
            Assert.NotEmpty(category.Topics);
            Assert.Equal(category.TopicKeys, category.Topics.Select(x => x.Key));
        }
        foreach (var legacy in CmkLearningCatalog.All)
            Assert.Same(legacy, CmkLearningCatalog.Find(legacy.Key));
    }

    [Fact]
    public void AlternativesAreSeparateAndDuplicateOverviewIsNotVisible()
    {
        var central = CmkCurriculum.CentralTopics.Select(x => x.Key).ToHashSet();
        Assert.Equal(new[] { "cmk250", "cmk253-255", "tck75" }, CmkCurriculum.AlternativeCategory.TopicKeys);
        Assert.Empty(CmkCurriculum.AlternativeCategory.TopicKeys.Where(central.Contains));
        Assert.Contains("cmk251-252", central);
        Assert.DoesNotContain(CmkCurriculum.All, x => x.Key == "cmk250-253");
        Assert.Equal(94, CmkCurriculum.CentralTopics.Sum(x => x.Quiz.Count));
        Assert.Equal(6, CmkCurriculum.AlternativeCategory.Topics.Sum(x => x.Quiz.Count));
    }

    [Fact]
    public void ReadingLocatorsCoverNumberedArticlesOneThrough335AndInsertedProvisions()
    {
        var covered = new HashSet<int>();
        foreach (var topic in CmkCurriculum.All.Where(t => t.ArticleLocator.StartsWith("CMK")))
        {
            var range = Regex.Match(topic.ArticleLocator, @"CMK (\d+)–(\d+)");
            if (range.Success)
                foreach (int n in Enumerable.Range(int.Parse(range.Groups[1].Value),
                    int.Parse(range.Groups[2].Value) - int.Parse(range.Groups[1].Value) + 1)) covered.Add(n);
            else
            {
                var single = Regex.Match(topic.ArticleLocator, @"CMK (\d+)");
                if (single.Success) covered.Add(int.Parse(single.Groups[1].Value));
            }
        }
        Assert.Empty(Enumerable.Range(1, 335).Except(covered));
        Assert.Contains("38/A", CmkLearningCatalog.Find("cmk33-38a").ArticleLocator);
        Assert.Contains("128/A", CmkLearningCatalog.Find("cmk128a").ArticleLocator);
        Assert.Contains("308/A", CmkLearningCatalog.Find("cmk308-310").ArticleLocator);
        Assert.Contains("doğrulanmadı", CmkLearningCatalog.Find("cmk-gecici").ArticleLocator);
        Assert.StartsWith("TCK", CmkLearningCatalog.Find("tck75").ArticleLocator);
    }

    [Fact]
    public void EveryLessonHasDistinctExplanationsValidFlowAndAssessableQuizAnswers()
    {
        foreach (var topic in CmkCurriculum.All)
        {
            var lesson = CmkCurriculum.Explain(topic.Key);
            Assert.True(lesson.Concept.Length > 55);
            Assert.True(lesson.Distinction.Length > 55);
            Assert.True(lesson.Example.Length > 55);
            Assert.NotEqual(lesson.Concept, lesson.Distinction);
            Assert.Equal(new[] { 1, 2, 3, 4 }, topic.Flow.Select(x => x.Order));
            Assert.Equal(2, topic.Quiz.Count);
            foreach (var question in topic.Quiz)
            {
                int index = topic.Quiz.ToList().IndexOf(question);
                Assert.Equal(3, question.Choices.Count);
                Assert.Equal(3, question.Choices.Distinct().Count());
                var correct = CmkLearningCatalog.Assess(topic.Key, index, question.CorrectIndex);
                Assert.True(correct.Correct);
                Assert.False(correct.LegalComplianceCertified);
                Assert.False(CmkLearningCatalog.Assess(topic.Key, index, (question.CorrectIndex + 1) % 3).Correct);
            }
        }
        Assert.Equal(100, CmkCurriculum.All.Sum(x => x.Quiz.Count));
    }

    [Fact]
    public void CrossCategoryNavigationPreservesMarksAndResetCoversNewTopics()
    {
        var state = new CmkLearningChecklistSession();
        foreach (var category in CmkCurriculum.AllCategories)
            foreach (var topic in category.Topics) state.SetReviewed(topic.Key, 2, true);
        foreach (var topic in CmkCurriculum.All) Assert.True(state.IsReviewed(topic.Key, 2));
        state.SetReviewed("cmk311-323", 2, false);
        Assert.True(state.IsReviewed("cmk74", 2));
        Assert.False(state.IsReviewed("cmk311-323", 2));
        state.Reset();
        Assert.All(CmkCurriculum.All, t => Assert.Equal(0, state.ReviewedCount(t.Key)));
    }

    [Fact]
    public void SourceFailureNeverBecomesCurrentLawOrLegalApproval()
    {
        Assert.Equal("https://www.mevzuat.gov.tr/MevzuatMetin/1.5.5271.doc", CmkCurriculum.RequestedDocumentUrl);
        Assert.Equal("NOT_VERIFIED", CmkCurriculum.SourceStatus);
        Assert.Contains("doğrulanmadı", CmkCurriculum.SourceRetrievalNote);
        Assert.All(CmkCurriculum.All, topic =>
        {
            Assert.False(topic.IsCurrentLawVerified);
            Assert.Equal("NOT_VERIFIED", topic.SourceStatus);
        });
        Assert.Throws<InvalidOperationException>(() => CmkCurriculum.FindCategory("missing"));
        Assert.Throws<InvalidOperationException>(() => CmkCurriculum.Explain("missing"));
    }
}
