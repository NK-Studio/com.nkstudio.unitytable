using System;
using System.Collections.Generic;
using NKStudio.TabularEditor.Data;

namespace NKStudio.TabularEditor.Commands
{
    /// <summary>
    /// 이어진 열 묶음을 왼쪽이나 오른쪽으로 옮깁니다.
    /// </summary>
    public sealed class MoveColumnsCommand : ITableCommand
    {
        private readonly int _index;
        private readonly int _count;
        private readonly int _delta;

        /// <summary>
        /// 열 이동 작업을 생성합니다.
        /// </summary>
        /// <param name="index">옮길 첫 열입니다.</param>
        /// <param name="count">옮길 열 개수입니다.</param>
        /// <param name="delta">옮길 칸 수입니다. 음수면 왼쪽으로, 양수면 오른쪽으로 옮깁니다.</param>
        public MoveColumnsCommand(int index, int count, int delta)
        {
            if (count <= 0 || delta == 0)
                throw new ArgumentException("Column count and move distance must be non-zero.");

            _index = index;
            _count = count;
            _delta = delta;
        }

        /// <summary>
        /// 작업 이름입니다.
        /// </summary>
        public string Name => Localization.Get("undo.moveColumns");

        /// <summary>
        /// 열을 옮깁니다.
        /// </summary>
        /// <param name="document">대상 문서입니다.</param>
        public void Execute(TableDocument document)
        {
            Move(document, _index, _index + _delta);
        }

        /// <summary>
        /// 옮긴 열을 원래 자리로 되돌립니다.
        /// </summary>
        /// <param name="document">대상 문서입니다.</param>
        public void Undo(TableDocument document)
        {
            Move(document, _index + _delta, _index);
        }

        // 문서에 열 순서를 바꾸는 API가 없어 꺼냈다가 다시 넣는다. 옮길 수 있는 경우엔 열이 둘 이상이라
        // '마지막 한 열은 남긴다'는 RemoveColumns 규칙에 걸리지 않는다.
        private void Move(TableDocument document, int from, int to)
        {
            List<string[]> removed = document.RemoveColumns(from, _count);
            document.InsertColumns(to, removed, removed.Count);
        }
    }
}
