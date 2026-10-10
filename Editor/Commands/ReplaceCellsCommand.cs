using System.Collections.Generic;
using NKStudio.TabularEditor.Data;

namespace NKStudio.TabularEditor.Commands
{
    /// <summary>
    /// 흩어진 여러 셀의 값을 한 번에 바꿉니다. 모두 바꾸기가 Undo 한 번으로 되돌아가도록 하나의 작업으로 묶습니다.
    /// 직사각형 범위는 <see cref="SetCellsCommand"/>를 씁니다.
    /// </summary>
    public sealed class ReplaceCellsCommand : ITableCommand
    {
        private readonly int[] _rows;
        private readonly int[] _columns;
        private readonly string[] _newValues;

        private string[] _oldValues;

        /// <summary>
        /// 셀 교체 작업을 생성합니다.
        /// </summary>
        /// <param name="name">Undo에 표시할 작업 이름입니다.</param>
        /// <param name="edits">바꿀 셀 좌표와 새 값입니다.</param>
        public ReplaceCellsCommand(string name, IReadOnlyList<(int Row, int Column, string Value)> edits)
        {
            Name = name;

            int count = edits?.Count ?? 0;
            _rows = new int[count];
            _columns = new int[count];
            _newValues = new string[count];

            for (int index = 0; index < count; index++)
            {
                _rows[index] = edits[index].Row;
                _columns[index] = edits[index].Column;
                _newValues[index] = edits[index].Value ?? string.Empty;
            }
        }

        public string Name { get; }

        /// <summary>
        /// 바꿀 셀 개수입니다.
        /// </summary>
        public int Count => _newValues.Length;

        /// <summary>
        /// 셀 값을 바꿉니다. 처음 실행할 때 이전 값을 기록합니다.
        /// </summary>
        public void Execute(TableDocument document)
        {
            if (_oldValues == null)
            {
                _oldValues = new string[_newValues.Length];

                for (int index = 0; index < _newValues.Length; index++)
                    _oldValues[index] = document.GetCell(_rows[index], _columns[index]);
            }

            Apply(document, _newValues);
        }

        /// <summary>
        /// 이전 값을 복원합니다.
        /// </summary>
        public void Undo(TableDocument document)
        {
            if (_oldValues == null)
                return;

            Apply(document, _oldValues);
        }

        private void Apply(TableDocument document, string[] values)
        {
            for (int index = 0; index < values.Length; index++)
                document.SetCell(_rows[index], _columns[index], values[index]);
        }
    }
}
