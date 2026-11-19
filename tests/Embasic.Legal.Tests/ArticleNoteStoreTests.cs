using Embasic.Legal.Infrastructure;
using Xunit;

namespace Embasic.Legal.Tests;

public sealed class ArticleNoteStoreTests
{
    private static string NewPath() => Path.Combine(Path.GetTempPath(),
        "embasic-legal-synthetic-" + Guid.NewGuid().ToString("N") + ".sqlite3");

    [Fact]
    public void UpsertAndReopenKeepSameNoteIdAndUnicode()
    {
        var file = NewPath();
        try
        {
            var key = new ArticleNoteKey("5237", "75", "6", "SYNTHETIC_VERSION_A");
            string id;
            using (var store = new ArticleNoteStore(file))
            {
                id = store.Save(key, "İlk not: ⚖ Türkçe").Id;
                Assert.Equal(id, store.Save(key, "Düzenlenmiş not — özel şüphe").Id);
            }
            using var reopened = new ArticleNoteStore(file);
            Assert.Equal(id, reopened.Get(key)?.Id);
            Assert.Contains("Düzenlenmiş", reopened.Get(key)?.Text);
            Assert.True(reopened.HasAnyNote("5237", "75"));
        }
        finally { File.Delete(file); }
    }

    [Fact]
    public void VersionAndArticleIsolationPreventsSilentCarryOver()
    {
        var file = NewPath();
        try
        {
            using var store = new ArticleNoteStore(file);
            var old = new ArticleNoteKey("5271", "253", "3", "SYNTHETIC_OLD");
            var newer = old with { VersionKey = "SYNTHETIC_NEW" };
            store.Save(old, "Eskiden kalan not");
            Assert.Null(store.Get(newer));
            Assert.True(store.HasAnyNote("5271", "253"));
            Assert.False(store.HasAnyNote("5271", "250"));
            Assert.False(store.Delete(newer));
            Assert.NotNull(store.Get(old));
            Assert.True(store.Delete(old));
            Assert.False(store.HasAnyNote("5271", "253"));
        }
        finally { File.Delete(file); }
    }

    [Fact]
    public void ParameterizedInputNeverExecutesSql()
    {
        var file = NewPath();
        try
        {
            using var store = new ArticleNoteStore(file);
            var strange = new ArticleNoteKey("5237", "75' OR 1=1 --", "", "synthetic");
            store.Save(strange, "Bilgi");
            Assert.Equal("Bilgi", store.Get(strange)?.Text);
            Assert.False(store.HasAnyNote("5237", "75"));
        }
        finally { File.Delete(file); }
    }

    [Fact]
    public void MissingIdentityOrEmptyTextFailsClosed()
    {
        var file = NewPath();
        try
        {
            using var store = new ArticleNoteStore(file);
            var key = new ArticleNoteKey("5237", "1", "", "synthetic");
            Assert.Throws<ArgumentException>(() => store.Save(key, " "));
            Assert.Throws<ArgumentException>(() => store.Save(key with { VersionKey = "" }, "not"));
        }
        finally { File.Delete(file); }
    }
}
