using System.Collections.Generic;
using NKStudio.TabularEditor.AssetLinks;
using NUnit.Framework;

namespace NKStudio.TabularEditor.Tests
{
    /// <summary>
    /// 에셋 경로 열 자동 판정을 검증합니다. "found/"로 시작하는 값만 에셋으로 찾아진다고 둔다.
    /// </summary>
    public sealed class AssetColumnDetectorTests
    {
        [Test]
        public void Test_IsAssetColumn_MostlyResolvedValues()
        {
            Assert.IsTrue(Detect("found/a", "found/b", "typo/c"));
        }

        [Test]
        public void Test_IsAssetColumn_FewResolvedValuesIsNot()
        {
            Assert.IsFalse(Detect("found/a", "Cat", "Dog", "Bird"));
        }

        [Test]
        public void Test_IsAssetColumn_IgnoresEmptyCells()
        {
            Assert.IsTrue(Detect("", "found/a", " ", "found/b"));
        }

        [Test]
        public void Test_IsAssetColumn_EmptyColumnIsNot()
        {
            Assert.IsFalse(Detect("", ""));
        }

        [Test]
        public void Test_IsAssetColumn_LooksOnlyAtSample()
        {
            List<string> values = new();

            for (int index = 0; index < AssetColumnDetector.SampleCount; index++)
                values.Add("found/" + index);

            for (int index = 0; index < 1000; index++)
                values.Add("plain");

            Assert.IsTrue(Detect(values.ToArray()));
        }

        private static bool Detect(params string[] values)
        {
            return AssetColumnDetector.IsAssetColumn(values, value => value.StartsWith("found/"));
        }
    }
}
