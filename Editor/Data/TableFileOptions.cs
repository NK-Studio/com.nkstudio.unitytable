using System.Text;

namespace NKStudio.TabularEditor.Data
{
    /// <summary>
    /// 원본 파일의 인코딩과 개행 형태를 보존해 저장 시 그대로 재현하기 위한 정보입니다.
    /// </summary>
    public sealed class TableFileOptions
    {
        /// <summary>
        /// 파일을 읽고 쓸 때 사용하는 인코딩입니다.
        /// </summary>
        public Encoding Encoding { get; set; } = new UTF8Encoding(false);

        /// <summary>
        /// 저장 시 사용할 개행 문자열입니다.
        /// </summary>
        public string NewLine { get; set; } = "\n";

        /// <summary>
        /// 파일이 개행으로 끝나는지 여부입니다.
        /// </summary>
        public bool EndsWithNewLine { get; set; }

        /// <summary>
        /// 현재 설정을 복사한 새 인스턴스를 반환합니다.
        /// </summary>
        /// <returns>복사된 파일 옵션입니다.</returns>
        public TableFileOptions Clone()
        {
            TableFileOptions clone = new();
            clone.Encoding = Encoding;
            clone.NewLine = NewLine;
            clone.EndsWithNewLine = EndsWithNewLine;

            return clone;
        }
    }
}
