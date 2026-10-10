using System;
using System.Collections.Generic;

namespace NKStudio.TabularEditor.Data
{
    /// <summary>
    /// 새 CSV 파일의 빈 표 내용을 만듭니다. 크기와 파일 형식(구분 기호·따옴표·개행·인코딩)은 Preferences에서 정합니다.
    /// </summary>
    public static class TableTemplateBuilder
    {
        /// <summary>
        /// 빈 셀로 채운 rows x columns 문서를 만듭니다.
        /// </summary>
        public static TableDocument CreateEmptyDocument(int rows, int columns, TableFileOptions options)
        {
            rows = Math.Max(1, rows);
            columns = Math.Max(1, columns);

            List<List<string>> cells = new(rows);

            for (int row = 0; row < rows; row++)
                cells.Add(new List<string>(new string[columns]));

            TableDocument document = new();
            document.SetContent(cells);
            document.FileOptions = (options ?? new TableFileOptions()).Clone();

            return document;
        }

        /// <summary>
        /// 빈 표를 파일 바이트(BOM 포함)로 만듭니다.
        /// </summary>
        public static byte[] CreateFileBytes(int rows, int columns, TableFileOptions options)
        {
            return TableDocumentIO.Serialize(CreateEmptyDocument(rows, columns, options));
        }

        /// <summary>
        /// 빈 표를 개행을 LF로 맞춘 텍스트로 만듭니다. Unity의 템플릿 생성 경로가 개행을 프로젝트 설정으로 바꾸므로
        /// 템플릿과 만들어진 파일을 비교할 때는 이 형태로 맞춘다.
        /// </summary>
        public static string CreateNormalizedText(int rows, int columns, TableFileOptions options)
        {
            TableFileOptions lf = (options ?? new TableFileOptions()).Clone();
            lf.NewLine = "\n";

            return DelimitedTextWriter.Write(CreateEmptyDocument(rows, columns, lf), lf);
        }
    }
}
