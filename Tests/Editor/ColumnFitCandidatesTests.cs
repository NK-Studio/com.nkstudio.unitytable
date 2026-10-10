using System.Collections.Generic;
using NKStudio.TabularEditor.Data;
using NUnit.Framework;

namespace NKStudio.TabularEditor.Tests
{
    /// <summary>
    /// 열 폭 맞춤에서 글자 폭을 잴 후보 행을 고르는 규칙을 검증합니다.
    /// </summary>
    public sealed class ColumnFitCandidatesTests
    {
        [Test]
        public void Test_SelectLongestRows_KeepsLongestOnes()
        {
            int[] lengths = { 1, 9, 3, 7, 5 };

            List<int> rows = ColumnFitCandidates.SelectLongestRows(row => lengths[row], 0, 4, 2);

            CollectionAssert.AreEquivalent(new[] { 1, 3 }, rows);
        }

        [Test]
        public void Test_SelectLongestRows_EmptyColumnHasNoCandidates()
        {
            List<int> rows = ColumnFitCandidates.SelectLongestRows(_ => 0, 0, 9, 5);

            Assert.IsEmpty(rows);
        }

        [Test]
        public void Test_SelectLongestRows_FewerRowsThanCount()
        {
            int[] lengths = { 4, 0, 2 };

            List<int> rows = ColumnFitCandidates.SelectLongestRows(row => lengths[row], 0, 2, 5);

            CollectionAssert.AreEquivalent(new[] { 0, 2 }, rows);
        }
    }
}
