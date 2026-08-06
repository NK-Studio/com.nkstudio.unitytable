using System;
using NKStudio.TabularEditor.Data;

namespace NKStudio.TabularEditor.Commands
{
    /// <summary>
    /// 직사각형 범위의 셀 값을 한 번에 교체합니다. 범위가 문서 밖으로 넘치면 행과 열을 함께 늘립니다.
    /// 셀 편집, 붙여넣기, 범위 비우기가 모두 이 작업 하나로 표현됩니다.
    /// </summary>
    public sealed class SetCellsCommand : ITableCommand
    {
        private readonly int _originRow;
        private readonly int _originColumn;
        private readonly string[][] _newValues;
        private readonly int _width;

        private string[][] _oldValues;
        private int _previousRowCount = -1;
        private int _previousColumnCount = -1;
        private bool _captured;

        /// <summary>
        /// 셀 교체 작업을 생성합니다.
        /// </summary>
        /// <param name="name">Undo에 표시할 작업 이름입니다.</param>
        /// <param name="originRow">범위의 시작 행입니다.</param>
        /// <param name="originColumn">범위의 시작 열입니다.</param>
        /// <param name="newValues">행 우선으로 배치된 새 값입니다.</param>
        public SetCellsCommand(string name, int originRow, int originColumn, string[][] newValues)
        {
            Name = name;
            _originRow = Math.Max(0, originRow);
            _originColumn = Math.Max(0, originColumn);
            _newValues = newValues ?? Array.Empty<string[]>();

            foreach (string[] row in _newValues)
            {
                if (row != null && row.Length > _width)
                    _width = row.Length;
            }
        }

        /// <summary>
        /// 작업 이름입니다.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 셀 값을 교체합니다. 최초 실행 시 이전 값과 이전 크기를 기록합니다.
        /// </summary>
        /// <param name="document">대상 문서입니다.</param>
        public void Execute(TableDocument document)
        {
            if (_newValues.Length == 0 || _width == 0)
                return;

            if (!_captured)
            {
                _previousRowCount = document.RowCount;
                _previousColumnCount = document.ColumnCount;
                _captured = true;

                EnsureSize(document);
                _oldValues = CaptureRegion(document);
            }
            else
            {
                EnsureSize(document);
            }

            ApplyValues(document, _newValues);
        }

        /// <summary>
        /// 이전 셀 값을 복원하고 늘어난 행과 열을 되돌립니다.
        /// </summary>
        /// <param name="document">대상 문서입니다.</param>
        public void Undo(TableDocument document)
        {
            if (!_captured || _oldValues == null)
                return;

            ApplyValues(document, _oldValues);

            if (document.RowCount > _previousRowCount)
                document.RemoveRows(_previousRowCount, document.RowCount - _previousRowCount);

            if (document.ColumnCount > _previousColumnCount)
                document.RemoveColumns(_previousColumnCount, document.ColumnCount - _previousColumnCount);
        }

        private void EnsureSize(TableDocument document)
        {
            int requiredRowCount = _originRow + _newValues.Length;

            if (document.RowCount < requiredRowCount)
                document.InsertRows(document.RowCount, null, requiredRowCount - document.RowCount);

            int requiredColumnCount = _originColumn + _width;

            if (document.ColumnCount < requiredColumnCount)
                document.InsertColumns(document.ColumnCount, null, requiredColumnCount - document.ColumnCount);
        }

        private string[][] CaptureRegion(TableDocument document)
        {
            string[][] region = new string[_newValues.Length][];

            for (int rowOffset = 0; rowOffset < region.Length; rowOffset++)
            {
                region[rowOffset] = new string[_width];

                for (int columnOffset = 0; columnOffset < _width; columnOffset++)
                {
                    region[rowOffset][columnOffset] = document.GetCell(
                        _originRow + rowOffset,
                        _originColumn + columnOffset);
                }
            }

            return region;
        }

        private void ApplyValues(TableDocument document, string[][] values)
        {
            for (int rowOffset = 0; rowOffset < values.Length; rowOffset++)
            {
                string[] row = values[rowOffset];

                for (int columnOffset = 0; columnOffset < _width; columnOffset++)
                {
                    bool hasValue = row != null && columnOffset < row.Length;

                    document.SetCell(
                        _originRow + rowOffset,
                        _originColumn + columnOffset,
                        hasValue ? row[columnOffset] : string.Empty);
                }
            }
        }
    }
}
