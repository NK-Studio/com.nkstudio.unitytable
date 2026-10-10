using System;
using NKStudio.TabularEditor.Data;

namespace NKStudio.TabularEditor.Commands
{
    /// <summary>
    /// 이어진 행 묶음을 위나 아래로 옮깁니다. 행 참조만 바꾸므로 셀 문자열을 새로 만들지 않습니다.
    /// </summary>
    public sealed class MoveRowsCommand : ITableCommand
    {
        private readonly int _spanStart;
        private readonly int[] _order;
        private readonly int[] _inverse;

        /// <summary>
        /// 행 이동 작업을 생성합니다.
        /// </summary>
        /// <param name="index">옮길 첫 행입니다.</param>
        /// <param name="count">옮길 행 개수입니다.</param>
        /// <param name="delta">옮길 칸 수입니다. 음수면 위로, 양수면 아래로 옮깁니다.</param>
        public MoveRowsCommand(int index, int count, int delta)
        {
            if (count <= 0 || delta == 0)
                throw new ArgumentException("Row count and move distance must be non-zero.");

            // 묶음과 그 자리를 비켜 주는 행들을 합친 구간 안에서 순서만 돌린다.
            // 예) 2~3행을 위로 1칸: 구간 1~3의 순서 [0,1,2] → [1,2,0]
            int shift = Math.Abs(delta);
            _spanStart = delta < 0 ? index + delta : index;
            _order = new int[count + shift];

            for (int position = 0; position < _order.Length; position++)
            {
                _order[position] = delta < 0
                    ? (position + shift) % _order.Length
                    : (position + count) % _order.Length;
            }

            _inverse = new int[_order.Length];

            for (int position = 0; position < _order.Length; position++)
                _inverse[_order[position]] = position;
        }

        /// <summary>
        /// 작업 이름입니다.
        /// </summary>
        public string Name => Localization.Get("undo.moveRows");

        /// <summary>
        /// 행을 옮깁니다.
        /// </summary>
        /// <param name="document">대상 문서입니다.</param>
        public void Execute(TableDocument document)
        {
            document.ReorderRows(_spanStart, _order);
        }

        /// <summary>
        /// 옮긴 행을 원래 자리로 되돌립니다.
        /// </summary>
        /// <param name="document">대상 문서입니다.</param>
        public void Undo(TableDocument document)
        {
            document.ReorderRows(_spanStart, _inverse);
        }
    }
}
