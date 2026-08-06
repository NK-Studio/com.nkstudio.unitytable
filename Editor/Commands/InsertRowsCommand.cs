using System.Collections.Generic;
using NKStudio.TabularEditor.Data;

namespace NKStudio.TabularEditor.Commands
{
    /// <summary>
    /// 지정한 위치에 행을 삽입합니다.
    /// </summary>
    public sealed class InsertRowsCommand : ITableCommand
    {
        private readonly int _index;
        private readonly int _count;
        private readonly IReadOnlyList<string[]> _values;

        /// <summary>
        /// 행 삽입 작업을 생성합니다.
        /// </summary>
        /// <param name="index">삽입 위치입니다.</param>
        /// <param name="count">삽입할 행 개수입니다.</param>
        /// <param name="values">삽입할 행의 값입니다. null이면 빈 행을 삽입합니다.</param>
        public InsertRowsCommand(int index, int count, IReadOnlyList<string[]> values = null)
        {
            _index = index;
            _count = values?.Count ?? count;
            _values = values;
        }

        /// <summary>
        /// 작업 이름입니다.
        /// </summary>
        public string Name => "행 삽입";

        /// <summary>
        /// 행을 삽입합니다.
        /// </summary>
        /// <param name="document">대상 문서입니다.</param>
        public void Execute(TableDocument document)
        {
            document.InsertRows(_index, _values, _count);
        }

        /// <summary>
        /// 삽입한 행을 제거합니다.
        /// </summary>
        /// <param name="document">대상 문서입니다.</param>
        public void Undo(TableDocument document)
        {
            document.RemoveRows(_index, _count);
        }
    }
}
