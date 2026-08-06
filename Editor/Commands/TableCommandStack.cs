using System;
using System.Collections.Generic;
using NKStudio.TabularEditor.Data;

namespace NKStudio.TabularEditor.Commands
{
    /// <summary>
    /// 윈도우 내부의 Undo/Redo 스택입니다. Unity 전역 Undo 시스템과 완전히 분리되어 있습니다.
    /// </summary>
    public sealed class TableCommandStack
    {
        private const int MaxDepth = 200;
        private const int UnreachableSavedIndex = -1;

        private readonly List<ITableCommand> _commands = new();

        private int _index;
        private int _savedIndex;

        /// <summary>
        /// 되돌릴 작업이 있는지 여부입니다.
        /// </summary>
        public bool CanUndo => _index > 0;

        /// <summary>
        /// 다시 실행할 작업이 있는지 여부입니다.
        /// </summary>
        public bool CanRedo => _index < _commands.Count;

        /// <summary>
        /// 마지막 저장 시점과 현재 상태가 다른지 여부입니다.
        /// </summary>
        public bool IsDirty => _index != _savedIndex;

        /// <summary>
        /// 스택 상태가 바뀌었을 때 호출됩니다.
        /// </summary>
        public event Action Changed;

        /// <summary>
        /// 새 작업을 실행하고 스택에 쌓습니다. 실행 지점 이후의 Redo 기록은 폐기됩니다.
        /// </summary>
        /// <param name="document">대상 문서입니다.</param>
        /// <param name="command">실행할 작업입니다.</param>
        public void Execute(TableDocument document, ITableCommand command)
        {
            if (document == null || command == null)
                return;

            command.Execute(document);

            if (_index < _commands.Count)
            {
                _commands.RemoveRange(_index, _commands.Count - _index);

                // 폐기된 구간에 저장 지점이 있었다면 다시는 깨끗한 상태로 돌아갈 수 없다.
                if (_savedIndex > _index)
                    _savedIndex = UnreachableSavedIndex;
            }

            _commands.Add(command);
            _index++;

            TrimToMaxDepth();
            Changed?.Invoke();
        }

        /// <summary>
        /// 마지막 작업을 되돌립니다.
        /// </summary>
        /// <param name="document">대상 문서입니다.</param>
        /// <returns>되돌린 작업이 있으면 true입니다.</returns>
        public bool Undo(TableDocument document)
        {
            if (document == null || !CanUndo)
                return false;

            _index--;
            _commands[_index].Undo(document);
            Changed?.Invoke();

            return true;
        }

        /// <summary>
        /// 되돌린 작업을 다시 실행합니다.
        /// </summary>
        /// <param name="document">대상 문서입니다.</param>
        /// <returns>다시 실행한 작업이 있으면 true입니다.</returns>
        public bool Redo(TableDocument document)
        {
            if (document == null || !CanRedo)
                return false;

            _commands[_index].Execute(document);
            _index++;
            Changed?.Invoke();

            return true;
        }

        /// <summary>
        /// 현재 위치를 저장 지점으로 표시합니다. 이 위치로 돌아오면 변경 없음 상태가 됩니다.
        /// </summary>
        public void MarkSaved()
        {
            _savedIndex = _index;
            Changed?.Invoke();
        }

        /// <summary>
        /// 스택을 모두 비우고 저장 지점을 초기화합니다.
        /// </summary>
        public void Clear()
        {
            _commands.Clear();
            _index = 0;
            _savedIndex = 0;
            Changed?.Invoke();
        }

        private void TrimToMaxDepth()
        {
            if (_commands.Count <= MaxDepth)
                return;

            int removeCount = _commands.Count - MaxDepth;
            _commands.RemoveRange(0, removeCount);
            _index -= removeCount;

            if (_savedIndex != UnreachableSavedIndex)
            {
                _savedIndex -= removeCount;

                if (_savedIndex < 0)
                    _savedIndex = UnreachableSavedIndex;
            }
        }
    }
}
