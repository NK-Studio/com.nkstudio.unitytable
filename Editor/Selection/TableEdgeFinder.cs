using NKStudio.TabularEditor.Data;

namespace NKStudio.TabularEditor.Selection
{
    /// <summary>
    /// Ctrl/Cmd+방향키가 도착할 셀을 찾습니다. Excel과 같은 규칙입니다.
    /// <list type="bullet">
    /// <item>지금 칸과 다음 칸에 모두 값이 있으면, 값이 이어지는 마지막 칸으로 갑니다.</item>
    /// <item>아니면 다음으로 값이 있는 칸으로 갑니다.</item>
    /// <item>그런 칸이 없으면 표 끝으로 갑니다.</item>
    /// </list>
    /// 예) 한 행이 [a, b, c, _, _, f]일 때 0열에서 오른쪽 → 2열, 2열에서 → 5열, 5열에서 → 5열(표 끝).
    /// </summary>
    public static class TableEdgeFinder
    {
        /// <summary>
        /// from에서 (rowDelta, columnDelta) 방향으로 이동할 셀을 반환합니다. 방향은 한 축만 ±1이어야 합니다.
        /// </summary>
        public static CellCoord FindEdge(TableDocument document, CellCoord from, int rowDelta, int columnDelta)
        {
            int maxRow = document.RowCount - 1;
            int maxColumn = document.ColumnCount - 1;

            CellCoord next = new(from.Row + rowDelta, from.Column + columnDelta);

            if (IsInside(next, maxRow, maxColumn) == false)
                return from;

            if (HasValue(document, from) && HasValue(document, next))
            {
                CellCoord last = next;

                while (true)
                {
                    CellCoord candidate = new(last.Row + rowDelta, last.Column + columnDelta);

                    if (IsInside(candidate, maxRow, maxColumn) == false || HasValue(document, candidate) == false)
                        return last;

                    last = candidate;
                }
            }

            CellCoord current = next;

            while (HasValue(document, current) == false)
            {
                CellCoord candidate = new(current.Row + rowDelta, current.Column + columnDelta);

                if (IsInside(candidate, maxRow, maxColumn) == false)
                    return current;

                current = candidate;
            }

            return current;
        }

        private static bool IsInside(CellCoord coord, int maxRow, int maxColumn)
        {
            return coord.Row >= 0 && coord.Row <= maxRow && coord.Column >= 0 && coord.Column <= maxColumn;
        }

        private static bool HasValue(TableDocument document, CellCoord coord)
        {
            // 길이 힌트는 문자열을 만들지 않아 싸지만, 따옴표만 있는 빈 셀("")도 0보다 크게 나온다. 그때만 실제 값을 본다.
            if (document.GetCellLengthHint(coord.Row, coord.Column) == 0)
                return false;

            return document.GetCell(coord.Row, coord.Column).Length > 0;
        }
    }
}
