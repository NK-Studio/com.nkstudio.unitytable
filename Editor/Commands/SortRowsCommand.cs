using NKStudio.TabularEditor.Data;

namespace NKStudio.TabularEditor.Commands
{
    /// <summary>
    /// 한 열을 기준으로 행 순서를 정렬합니다. 되돌리기를 위해 적용한 순서를 보관합니다.
    /// </summary>
    public sealed class SortRowsCommand : ITableCommand
    {
        private readonly int _firstRow;
        private readonly int _column;
        private readonly bool _descending;

        private int[] _order;

        /// <summary>
        /// 행 정렬 작업을 생성합니다.
        /// </summary>
        /// <param name="firstRow">정렬할 첫 행입니다. 그 앞의 행(헤더)은 정렬하지 않습니다.</param>
        /// <param name="column">기준 열입니다.</param>
        /// <param name="descending">내림차순이면 true입니다.</param>
        public SortRowsCommand(int firstRow, int column, bool descending)
        {
            _firstRow = firstRow;
            _column = column;
            _descending = descending;
        }

        /// <summary>
        /// 작업 이름입니다.
        /// </summary>
        public string Name => Localization.Get(_descending ? "menu.sortDescending" : "menu.sortAscending");

        /// <summary>
        /// 행을 정렬합니다. 처음 실행할 때 계산한 순서를 Redo에서도 그대로 쓴다.
        /// </summary>
        /// <param name="document">대상 문서입니다.</param>
        public void Execute(TableDocument document)
        {
            _order ??= TableRowSorter.ComputeOrder(document, _firstRow, _column, _descending);
            document.ReorderRows(_firstRow, _order);
        }

        /// <summary>
        /// 정렬 전 행 순서로 되돌립니다.
        /// </summary>
        /// <param name="document">대상 문서입니다.</param>
        public void Undo(TableDocument document)
        {
            if (_order == null)
                return;

            // 정렬로 i번째 자리에 온 행은 원래 _order[i]번째였다. 그 행을 다시 _order[i]번째 자리로 보낸다.
            int[] inverse = new int[_order.Length];

            for (int index = 0; index < _order.Length; index++)
                inverse[_order[index]] = index;

            document.ReorderRows(_firstRow, inverse);
        }
    }
}
