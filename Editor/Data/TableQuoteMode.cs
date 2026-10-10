namespace NKStudio.TabularEditor.Data
{
    /// <summary>
    /// 저장할 때 셀을 따옴표로 감싸는 규칙입니다.
    /// </summary>
    public enum TableQuoteMode
    {
        /// <summary>모든 셀을 감싼다.</summary>
        Always,

        /// <summary>구분 기호·따옴표·줄바꿈이 든 셀만 감싼다. 불필요한 diff가 생기지 않는 기본값이다.</summary>
        Minimal,

        /// <summary>감싸지 않는다. 구분 기호가 든 셀은 다시 읽을 때 칸이 나뉘므로 주의해야 한다.</summary>
        Never,
    }
}
