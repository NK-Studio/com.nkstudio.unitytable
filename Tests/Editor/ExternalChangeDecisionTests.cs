using NKStudio.TabularEditor.Window;
using NUnit.Framework;

namespace NKStudio.TabularEditor.Tests
{
    /// <summary>
    /// 에디터 밖 변경을 감지했을 때 무시·자동 반영·알림 중 무엇을 고르는지 검증합니다.
    /// </summary>
    public sealed class ExternalChangeDecisionTests
    {
        [Test]
        public void Test_Decide_SameContentIsIgnored()
        {
            Assert.AreEqual(ExternalChangeAction.Ignore, ExternalChangeDecision.Decide(false, true, true));
        }

        [Test]
        public void Test_Decide_CleanDocumentReloads()
        {
            Assert.AreEqual(ExternalChangeAction.Reload, ExternalChangeDecision.Decide(true, false, false));
        }

        [Test]
        public void Test_Decide_UnsavedChangesNotify()
        {
            Assert.AreEqual(ExternalChangeAction.Notify, ExternalChangeDecision.Decide(true, true, false));
        }

        [Test]
        public void Test_Decide_TypingInCellNotifies()
        {
            Assert.AreEqual(ExternalChangeAction.Notify, ExternalChangeDecision.Decide(true, false, true));
        }
    }
}
