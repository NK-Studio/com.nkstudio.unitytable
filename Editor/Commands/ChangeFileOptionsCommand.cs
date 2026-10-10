using NKStudio.TabularEditor.Data;

namespace NKStudio.TabularEditor.Commands
{
    /// <summary>
    /// 셀 값은 그대로 두고 저장할 파일 형식(인코딩·구분 기호·따옴표·개행)만 바꿉니다.
    /// 파일 형식 대화상자의 '적용'이 이 작업으로 실행되어, 저장 전까지는 Undo로 되돌릴 수 있고 문서가 '저장 안 함'이 됩니다.
    /// </summary>
    public sealed class ChangeFileOptionsCommand : ITableCommand
    {
        private readonly TableFileOptions _newOptions;
        private TableFileOptions _oldOptions;

        public ChangeFileOptionsCommand(TableFileOptions newOptions)
        {
            _newOptions = newOptions.Clone();
        }

        public string Name => "파일 형식 변경";

        public void Execute(TableDocument document)
        {
            _oldOptions ??= (document.FileOptions ?? new TableFileOptions()).Clone();
            document.FileOptions = _newOptions.Clone();
        }

        public void Undo(TableDocument document)
        {
            if (_oldOptions != null)
                document.FileOptions = _oldOptions.Clone();
        }
    }
}
