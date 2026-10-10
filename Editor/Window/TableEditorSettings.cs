using System;
using NKStudio.TabularEditor.Data;
using UnityEditor;

namespace NKStudio.TabularEditor.Window
{
    /// <summary>
    /// Preferences > Tabular Editor의 사용자별 설정입니다. 보기 취향이라 프로젝트가 아니라 EditorPrefs에 저장하므로
    /// 같은 프로젝트를 쓰는 팀원마다 다르게 둘 수 있습니다. 파일마다 달라야 하는 것(헤더 행·파일 형식)은 .meta에 둡니다.
    /// </summary>
    public static class TableEditorSettings
    {
        private const string Prefix = "NKStudio.TabularEditor.";

        public const int DefaultFontSize = 12;
        public const int MinFontSize = 8;
        public const int MaxFontSize = 32;

        public const int DefaultAutoFitScanRows = 3000;
        public const int MaxAutoFitScanRows = 1_000_000;
        public const int DefaultAutoFitMaxWidthPercent = 70;

        public const int DefaultNewTableSize = 5;
        public const int MaxNewTableRows = 10_000;
        public const int MaxNewTableColumns = 1_000;

        /// <summary>
        /// 설정이 바뀌면 호출됩니다. 열려 있는 모든 창이 함께 갱신됩니다.
        /// </summary>
        public static event Action Changed;

        // 표시

        /// <summary>
        /// 표의 글꼴 크기(px)입니다. 행 높이·행 번호 폭 등이 이 크기에 비례합니다.
        /// </summary>
        public static int FontSize
        {
            get => Math.Clamp(EditorPrefs.GetInt(Prefix + "FontSize", DefaultFontSize), MinFontSize, MaxFontSize);
            set => SetInt("FontSize", Math.Clamp(value, MinFontSize, MaxFontSize));
        }

        /// <summary>
        /// Ctrl(macOS는 Cmd) + 마우스 휠로 글꼴 크기를 바꿀지 여부입니다.
        /// </summary>
        public static bool MouseWheelZoom
        {
            get => EditorPrefs.GetBool(Prefix + "MouseWheelZoom", true);
            set => SetBool("MouseWheelZoom", value);
        }

        /// <summary>
        /// 에셋 경로 열(값이 Resources 경로 등으로 에셋을 가리키는 열)에 아이콘·미리보기를 보일지 여부입니다.
        /// </summary>
        public static bool AssetPathPreview
        {
            get => EditorPrefs.GetBool(Prefix + "AssetPathPreview", true);
            set => SetBool("AssetPathPreview", value);
        }

        // 열 너비 자동 맞춤

        public static bool AutoFitOnOpen
        {
            get => EditorPrefs.GetBool(Prefix + "AutoFitOnOpen", true);
            set => SetBool("AutoFitOnOpen", value);
        }

        public static bool AutoFitOnEdit
        {
            get => EditorPrefs.GetBool(Prefix + "AutoFitOnEdit", true);
            set => SetBool("AutoFitOnEdit", value);
        }

        /// <summary>
        /// 자동 맞춤에서 위에서부터 훑을 본문 행 수입니다. 헤더 행은 항상 봅니다.
        /// </summary>
        public static int AutoFitScanRows
        {
            get => Math.Clamp(EditorPrefs.GetInt(Prefix + "AutoFitScanRows", DefaultAutoFitScanRows), 1, MaxAutoFitScanRows);
            set => SetInt("AutoFitScanRows", Math.Clamp(value, 1, MaxAutoFitScanRows));
        }

        /// <summary>
        /// 자동 맞춤 열의 최대 너비입니다. 표 영역 너비의 백분율(1~100)입니다.
        /// </summary>
        public static int AutoFitMaxWidthPercent
        {
            get => Math.Clamp(EditorPrefs.GetInt(Prefix + "AutoFitMaxWidthPercent", DefaultAutoFitMaxWidthPercent), 1, 100);
            set => SetInt("AutoFitMaxWidthPercent", Math.Clamp(value, 1, 100));
        }

        // 새 CSV 파일

        public static int NewTableRows
        {
            get => Math.Clamp(EditorPrefs.GetInt(Prefix + "NewTableRows", DefaultNewTableSize), 1, MaxNewTableRows);
            set => SetInt("NewTableRows", Math.Clamp(value, 1, MaxNewTableRows));
        }

        public static int NewTableColumns
        {
            get => Math.Clamp(EditorPrefs.GetInt(Prefix + "NewTableColumns", DefaultNewTableSize), 1, MaxNewTableColumns);
            set => SetInt("NewTableColumns", Math.Clamp(value, 1, MaxNewTableColumns));
        }

        /// <summary>
        /// 새 CSV 파일의 인코딩입니다. <see cref="TableEncodings.Entries"/>의 위치로 저장합니다.
        /// </summary>
        public static int NewTableEncodingIndex
        {
            get => Math.Clamp(EditorPrefs.GetInt(Prefix + "NewTableEncodingIndex", 0), 0, TableEncodings.Entries.Count - 1);
            set => SetInt("NewTableEncodingIndex", Math.Clamp(value, 0, TableEncodings.Entries.Count - 1));
        }

        public static char NewTableDelimiter
        {
            get => (char)EditorPrefs.GetInt(Prefix + "NewTableDelimiter", ',');
            set => SetInt("NewTableDelimiter", value);
        }

        /// <summary>
        /// 새 CSV 파일의 따옴표 문자입니다. <see cref="TableFileOptions.NoQuote"/>이면 따옴표를 쓰지 않습니다.
        /// </summary>
        public static char NewTableQuote
        {
            get => (char)EditorPrefs.GetInt(Prefix + "NewTableQuote", '"');
            set => SetInt("NewTableQuote", value);
        }

        public static TableQuoteMode NewTableQuoteMode
        {
            get
            {
                int value = EditorPrefs.GetInt(Prefix + "NewTableQuoteMode", (int)TableQuoteMode.Minimal);
                return Enum.IsDefined(typeof(TableQuoteMode), value) ? (TableQuoteMode)value : TableQuoteMode.Minimal;
            }
            set => SetInt("NewTableQuoteMode", (int)value);
        }

        public static bool NewTableUsesCrlf
        {
            get => EditorPrefs.GetBool(Prefix + "NewTableUsesCrlf", false);
            set => SetBool("NewTableUsesCrlf", value);
        }

        public static bool NewTableEndsWithNewLine
        {
            get => EditorPrefs.GetBool(Prefix + "NewTableEndsWithNewLine", true);
            set => SetBool("NewTableEndsWithNewLine", value);
        }

        /// <summary>
        /// 새 CSV 파일을 만들 때 쓸 파일 형식입니다.
        /// </summary>
        public static TableFileOptions CreateNewTableOptions()
        {
            return new TableFileOptions
            {
                Encoding = TableEncodings.Entries[NewTableEncodingIndex].Create(),
                Delimiter = NewTableDelimiter,
                Quote = NewTableQuote,
                QuoteMode = NewTableQuoteMode,
                NewLine = NewTableUsesCrlf ? "\r\n" : "\n",
                EndsWithNewLine = NewTableEndsWithNewLine,
            };
        }

        /// <summary>
        /// 모든 설정을 기본값으로 되돌립니다. 테마는 따로 둡니다.
        /// </summary>
        public static void ResetToDefaults()
        {
            foreach (string key in new[]
                     {
                         "FontSize", "MouseWheelZoom", "AutoFitOnOpen", "AutoFitOnEdit", "AutoFitScanRows",
                         "AutoFitMaxWidthPercent", "NewTableRows", "NewTableColumns", "NewTableEncodingIndex",
                         "NewTableDelimiter", "NewTableQuote", "NewTableQuoteMode", "NewTableUsesCrlf",
                         "NewTableEndsWithNewLine", "AssetPathPreview",
                     })
            {
                EditorPrefs.DeleteKey(Prefix + key);
            }

            Changed?.Invoke();
        }

        private static void SetInt(string key, int value)
        {
            if (EditorPrefs.HasKey(Prefix + key) && EditorPrefs.GetInt(Prefix + key) == value)
                return;

            EditorPrefs.SetInt(Prefix + key, value);
            Changed?.Invoke();
        }

        private static void SetBool(string key, bool value)
        {
            if (EditorPrefs.HasKey(Prefix + key) && EditorPrefs.GetBool(Prefix + key) == value)
                return;

            EditorPrefs.SetBool(Prefix + key, value);
            Changed?.Invoke();
        }
    }
}
