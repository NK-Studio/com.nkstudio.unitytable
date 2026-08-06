namespace NKStudio.TabularEditor.Window
{
    /// <summary>
    /// 재활용되는 셀 VisualElement가 현재 어떤 문서 좌표를 표시 중인지 기록합니다.
    /// </summary>
    public sealed class TableCellBinding
    {
        /// <summary>
        /// 문서 기준 행 인덱스입니다.
        /// </summary>
        public int Row { get; set; } = -1;

        /// <summary>
        /// 문서 기준 열 인덱스입니다.
        /// </summary>
        public int Column { get; set; } = -1;
    }
}
