using NKStudio.TabularEditor.Data;

namespace NKStudio.TabularEditor.Commands
{
    /// <summary>
    /// 문서를 변경하는 되돌리기 가능한 작업입니다. 문서 변경은 반드시 이 인터페이스를 통해서만 수행합니다.
    /// </summary>
    public interface ITableCommand
    {
        /// <summary>
        /// Undo 목록에 표시할 작업 이름입니다.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 작업을 문서에 적용합니다.
        /// </summary>
        /// <param name="document">대상 문서입니다.</param>
        void Execute(TableDocument document);

        /// <summary>
        /// 작업을 되돌려 문서를 이전 상태로 복원합니다.
        /// </summary>
        /// <param name="document">대상 문서입니다.</param>
        void Undo(TableDocument document);
    }
}
