using NKStudio.TabularEditor.Data;
using NUnit.Framework;

namespace NKStudio.TabularEditor.Tests
{
    /// <summary>
    /// .meta userData에 헤더 행 수를 기록하는 형식을 검증합니다.
    /// </summary>
    public sealed class TableAssetSettingsTests
    {
        [Test]
        public void Test_FormatHeaderRowCount_RoundTrips()
        {
            string userData = TableAssetSettings.FormatHeaderRowCount(3);

            Assert.AreEqual(3, TableAssetSettings.ParseHeaderRowCount(userData));
        }

        [Test]
        public void Test_FormatHeaderRowCount_ZeroLeavesMetaEmpty()
        {
            Assert.AreEqual(string.Empty, TableAssetSettings.FormatHeaderRowCount(0));
            Assert.AreEqual(string.Empty, TableAssetSettings.FormatHeaderRowCount(-1));
        }

        [Test]
        public void Test_ParseHeaderRowCount_EmptyIsZero()
        {
            Assert.AreEqual(0, TableAssetSettings.ParseHeaderRowCount(null));
            Assert.AreEqual(0, TableAssetSettings.ParseHeaderRowCount(string.Empty));
        }

        [Test]
        public void Test_ParseHeaderRowCount_ForeignUserDataIsZero()
        {
            Assert.AreEqual(0, TableAssetSettings.ParseHeaderRowCount("some-other-tool:42"));
            Assert.AreEqual(0, TableAssetSettings.ParseHeaderRowCount("{\"other\":1}"));
        }

        [Test]
        public void Test_ParseHeaderRowCount_NegativeIsClampedToZero()
        {
            Assert.AreEqual(0, TableAssetSettings.ParseHeaderRowCount("{\"headerRowCount\":-2}"));
        }

        [Test]
        public void Test_IsOwnedOrEmpty_ProtectsForeignUserData()
        {
            Assert.IsTrue(TableAssetSettings.IsOwnedOrEmpty(string.Empty));
            Assert.IsTrue(TableAssetSettings.IsOwnedOrEmpty(TableAssetSettings.FormatHeaderRowCount(1)));
            Assert.IsFalse(TableAssetSettings.IsOwnedOrEmpty("some-other-tool:42"));
        }
    }
}
