namespace NKStudio.TabularEditor.Selection
{
    /// <summary>
    /// 선택이 무엇을 대상으로 하는지 나타냅니다. Delete 키의 동작을 이 값으로 구분합니다.
    /// </summary>
    public enum CellSelectionKind
    {
        /// <summary>
        /// 개별 셀 범위입니다. Delete는 내용만 지웁니다.
        /// </summary>
        Cells,

        /// <summary>
        /// 행 번호를 눌러 선택한 행 전체입니다. Delete는 행을 삭제합니다.
        /// </summary>
        Rows,

        /// <summary>
        /// 열 제목을 눌러 선택한 열 전체입니다. Delete는 열을 삭제합니다.
        /// </summary>
        Columns
    }
}
