using System.Collections.Generic;
using System.Text;
using NKStudio.TabularEditor.Data;
using NKStudio.TabularEditor.Selection;
using UnityEditor;

namespace NKStudio.TabularEditor.Window
{
    /// <summary>
    /// 선택 범위를 시스템 클립보드와 주고받습니다.
    /// 클립보드 포맷은 편집 중인 파일 형식과 무관하게 항상 TSV라서 Excel, 구글 시트와 호환됩니다.
    /// </summary>
    public static class TableClipboard
    {
        /// <summary>
        /// 선택 범위를 TSV 문자열로 클립보드에 복사합니다.
        /// </summary>
        /// <param name="document">복사할 문서입니다.</param>
        /// <param name="selection">복사할 선택 범위입니다.</param>
        public static void Copy(TableDocument document, CellSelection selection)
        {
            if (document == null || selection == null)
                return;

            StringBuilder builder = new();

            for (int row = selection.MinRow; row <= selection.MaxRow; row++)
            {
                if (row > selection.MinRow)
                    builder.Append('\n');

                for (int column = selection.MinColumn; column <= selection.MaxColumn; column++)
                {
                    if (column > selection.MinColumn)
                        builder.Append(TableFormatUtility.ClipboardDelimiter);

                    DelimitedTextWriter.AppendField(
                        builder,
                        document.GetCell(row, column),
                        TableFormatUtility.ClipboardDelimiter);
                }
            }

            EditorGUIUtility.systemCopyBuffer = builder.ToString();
        }

        /// <summary>
        /// 클립보드에 붙여넣을 내용이 있는지 확인합니다.
        /// </summary>
        /// <returns>내용이 있으면 true입니다.</returns>
        public static bool HasContent()
        {
            return !string.IsNullOrEmpty(EditorGUIUtility.systemCopyBuffer);
        }

        /// <summary>
        /// 클립보드 내용을 TSV로 파싱해 셀 값 배열로 반환합니다.
        /// </summary>
        /// <returns>행 우선으로 배치된 셀 값입니다. 클립보드가 비어 있으면 null입니다.</returns>
        public static string[][] ReadValues()
        {
            string text = EditorGUIUtility.systemCopyBuffer;

            if (string.IsNullOrEmpty(text))
                return null;

            List<List<string>> rows = DelimitedTextParser.Parse(
                text,
                TableFormatUtility.ClipboardDelimiter,
                null);

            if (rows.Count == 0)
                return null;

            string[][] values = new string[rows.Count][];

            for (int index = 0; index < rows.Count; index++)
                values[index] = rows[index].ToArray();

            return values;
        }

        /// <summary>
        /// 선택 범위를 빈 문자열로 채울 값 배열을 만듭니다.
        /// </summary>
        /// <param name="selection">비울 선택 범위입니다.</param>
        /// <returns>빈 문자열로 채워진 값 배열입니다.</returns>
        public static string[][] CreateEmptyValues(CellSelection selection)
        {
            int rowCount = selection.MaxRow - selection.MinRow + 1;
            int columnCount = selection.MaxColumn - selection.MinColumn + 1;

            string[][] values = new string[rowCount][];

            for (int row = 0; row < rowCount; row++)
            {
                values[row] = new string[columnCount];

                for (int column = 0; column < columnCount; column++)
                    values[row][column] = string.Empty;
            }

            return values;
        }
    }
}
