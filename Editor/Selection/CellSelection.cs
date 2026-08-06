using System;

namespace NKStudio.TabularEditor.Selection
{
    /// <summary>
    /// 활성 셀과 선택 범위를 관리합니다. anchor는 범위의 고정점, focus는 활성 셀입니다.
    /// </summary>
    public sealed class CellSelection
    {
        private CellCoord _anchor;
        private CellCoord _focus;

        /// <summary>
        /// 범위 확장의 기준이 되는 고정 셀입니다.
        /// </summary>
        public CellCoord Anchor => _anchor;

        /// <summary>
        /// 현재 활성 셀입니다. 편집과 스크롤 추적의 대상입니다.
        /// </summary>
        public CellCoord Focus => _focus;

        /// <summary>
        /// 선택 범위의 첫 행입니다.
        /// </summary>
        public int MinRow => Math.Min(_anchor.Row, _focus.Row);

        /// <summary>
        /// 선택 범위의 마지막 행입니다.
        /// </summary>
        public int MaxRow => Math.Max(_anchor.Row, _focus.Row);

        /// <summary>
        /// 선택 범위의 첫 열입니다.
        /// </summary>
        public int MinColumn => Math.Min(_anchor.Column, _focus.Column);

        /// <summary>
        /// 선택 범위의 마지막 열입니다.
        /// </summary>
        public int MaxColumn => Math.Max(_anchor.Column, _focus.Column);

        /// <summary>
        /// 선택 범위가 셀 하나인지 여부입니다.
        /// </summary>
        public bool IsSingleCell => _anchor.Equals(_focus);

        /// <summary>
        /// 현재 선택이 셀, 행 전체, 열 전체 중 무엇을 대상으로 하는지 나타냅니다.
        /// </summary>
        public CellSelectionKind Kind { get; private set; } = CellSelectionKind.Cells;

        /// <summary>
        /// 선택 상태가 바뀌었을 때 호출됩니다.
        /// </summary>
        public event Action Changed;

        /// <summary>
        /// 선택을 지정한 셀 하나로 축소합니다.
        /// </summary>
        /// <param name="coord">활성 셀 좌표입니다.</param>
        public void SetActive(CellCoord coord)
        {
            if (_anchor.Equals(coord) && _focus.Equals(coord) && Kind == CellSelectionKind.Cells)
                return;

            _anchor = coord;
            _focus = coord;
            Kind = CellSelectionKind.Cells;
            Changed?.Invoke();
        }

        /// <summary>
        /// anchor를 유지한 채 활성 셀을 옮겨 범위를 확장합니다.
        /// </summary>
        /// <param name="coord">확장할 셀 좌표입니다.</param>
        public void ExtendTo(CellCoord coord)
        {
            if (_focus.Equals(coord) && Kind == CellSelectionKind.Cells)
                return;

            _focus = coord;
            Kind = CellSelectionKind.Cells;
            Changed?.Invoke();
        }

        /// <summary>
        /// 지정한 범위 전체를 선택합니다.
        /// </summary>
        /// <param name="firstRow">시작 행입니다.</param>
        /// <param name="firstColumn">시작 열입니다.</param>
        /// <param name="lastRow">마지막 행입니다.</param>
        /// <param name="lastColumn">마지막 열입니다.</param>
        /// <param name="kind">선택 대상 종류입니다. 지정하지 않으면 셀 범위입니다.</param>
        public void SetRange(
            int firstRow,
            int firstColumn,
            int lastRow,
            int lastColumn,
            CellSelectionKind kind = CellSelectionKind.Cells)
        {
            _anchor = new CellCoord(firstRow, firstColumn);
            _focus = new CellCoord(lastRow, lastColumn);
            Kind = kind;
            Changed?.Invoke();
        }

        /// <summary>
        /// 지정한 셀이 선택 범위 안에 있는지 확인합니다.
        /// </summary>
        /// <param name="row">행 인덱스입니다.</param>
        /// <param name="column">열 인덱스입니다.</param>
        /// <returns>범위 안이면 true입니다.</returns>
        public bool Contains(int row, int column)
        {
            return row >= MinRow
                && row <= MaxRow
                && column >= MinColumn
                && column <= MaxColumn;
        }

        /// <summary>
        /// 문서 크기가 줄었을 때 선택 좌표를 유효 범위로 보정합니다.
        /// </summary>
        /// <param name="minRow">허용되는 최소 행입니다.</param>
        /// <param name="rowCount">전체 행 개수입니다.</param>
        /// <param name="columnCount">전체 열 개수입니다.</param>
        public void Clamp(int minRow, int rowCount, int columnCount)
        {
            int maxRow = Math.Max(minRow, rowCount - 1);
            int maxColumn = Math.Max(0, columnCount - 1);

            CellCoord anchor = new(
                Math.Clamp(_anchor.Row, minRow, maxRow),
                Math.Clamp(_anchor.Column, 0, maxColumn));

            CellCoord focus = new(
                Math.Clamp(_focus.Row, minRow, maxRow),
                Math.Clamp(_focus.Column, 0, maxColumn));

            if (anchor.Equals(_anchor) && focus.Equals(_focus))
                return;

            _anchor = anchor;
            _focus = focus;
            Changed?.Invoke();
        }
    }
}
