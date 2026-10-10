using NUnit.Framework;

namespace NKStudio.TabularEditor.Tests
{
    /// <summary>
    /// 문구 표의 빠진 번역과 언어 전환을 검증합니다.
    /// </summary>
    public sealed class LocalizationTests
    {
        [Test]
        public void Test_Strings_EveryKeyHasBothLanguagesAndPluralPairs()
        {
            CollectionAssert.IsEmpty(Localization.FindIncompleteKeys());
        }

        [Test]
        public void Test_Get_FollowsOverriddenLanguage()
        {
            using (Localization.Override(Language.English))
                Assert.AreEqual("Sort Ascending", Localization.Get("menu.sortAscending"));

            using (Localization.Override(Language.Korean))
                Assert.AreEqual("오름차순 정렬", Localization.Get("menu.sortAscending"));
        }

        [Test]
        public void Test_Count_PicksSingularOrPlural()
        {
            using (Localization.Override(Language.English))
            {
                Assert.AreEqual("1 row", Localization.Count("count.row", 1));
                Assert.AreEqual("3 rows", Localization.Count("count.row", 3));
            }
        }

        [Test]
        public void Test_Get_UnknownKeyShowsKey()
        {
            Assert.AreEqual("no.such.key", Localization.Get("no.such.key"));
        }
    }
}
