using System.Collections.Generic;
using NKStudio.TabularEditor.Data;

namespace NKStudio.TabularEditor.Commands
{
    /// <summary>
    /// 지정한 위치의 행을 제거합니다. 되돌리기를 위해 제거된 값을 보관합니다.
    /// </summary>
    public sealed class RemoveRowsCommand : ITableCommand
    {
        private readonly int _index;
        private readonly int _count;

        private List<string[]> _removed;

        /// <summary>
        /// 행 제거 작업을 생성합니다.
        /// </summary>
        /// <param name="index">제거 시작 위치입니다.</param>
        /// <param name="count">제거할 행 개수입니다.</param>
        public RemoveRowsCommand(int index, int count)
        {
            _index = index;
            _count = count;
        }

        /// <summary>
        /// 작업 이름입니다.
        /// </summary>
        public string Name => "행 삭제";

        /// <summary>
        /// 행을 제거하고 제거된 값을 기록합니다.
        /// </summary>
        /// <param name="document">대상 문서입니다.</param>
        public void Execute(TableDocument document)
        {
            _removed = document.RemoveRows(_index, _count);
        }

        /// <summary>
        /// 제거한 행을 원래 위치에 복원합니다.
        /// </summary>
        /// <param name="document">대상 문서입니다.</param>
        public void Undo(TableDocument document)
        {
            if (_removed == null || _removed.Count == 0)
                return;

            document.InsertRows(_index, _removed, _removed.Count);
        }
    }
}
