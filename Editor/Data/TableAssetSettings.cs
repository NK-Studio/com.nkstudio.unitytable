using System;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace NKStudio.TabularEditor.Data
{
    /// <summary>
    /// 파일에서 다시 알아낼 수 없는 파일 형식입니다. 비어 있는 항목은 자동 판별(BOM·확장자·기본값)을 따릅니다.
    /// </summary>
    public sealed class TableFileFormatOverride
    {
        /// <summary>
        /// BOM으로 알 수 없는 인코딩의 코드 페이지입니다(예: CP949). 0이면 자동입니다.
        /// </summary>
        public int EncodingCodePage { get; set; }

        /// <summary>
        /// 확장자 기본값과 다른 구분 기호입니다. null이면 확장자를 따릅니다.
        /// </summary>
        public char? Delimiter { get; set; }

        /// <summary>
        /// 큰따옴표가 아닌 따옴표 문자입니다. <see cref="TableFileOptions.NoQuote"/>이면 따옴표를 쓰지 않습니다. null이면 큰따옴표입니다.
        /// </summary>
        public char? Quote { get; set; }

        /// <summary>
        /// '최소'가 아닌 감싸는 규칙입니다. null이면 최소입니다.
        /// </summary>
        public TableQuoteMode? QuoteMode { get; set; }

        public bool IsEmpty => EncodingCodePage == 0 && Delimiter == null && Quote == null && QuoteMode == null;

        /// <summary>
        /// 파일 형식 중 파일에서 다시 알아낼 수 없는 부분만 남깁니다. 다 알아낼 수 있으면 빈 값입니다.
        /// </summary>
        /// <param name="options">저장하거나 다시 연 파일 형식입니다.</param>
        /// <param name="extensionFormat">파일 확장자로 정해지는 형식입니다.</param>
        public static TableFileFormatOverride From(TableFileOptions options, TableFormat extensionFormat)
        {
            TableFileFormatOverride result = new();

            if (options == null)
                return result;

            if (TableEncodings.IsDetectable(options.Encoding) == false)
                result.EncodingCodePage = options.Encoding.CodePage;

            if (options.Delimiter != TableFormatUtility.GetDelimiter(extensionFormat))
                result.Delimiter = options.Delimiter;

            if (options.Quote != '"')
                result.Quote = options.Quote;

            if (options.QuoteMode != TableQuoteMode.Minimal)
                result.QuoteMode = options.QuoteMode;

            return result;
        }
    }

    /// <summary>
    /// 파일별 설정(헤더 행 수, 파일에서 알아낼 수 없는 파일 형식)을 그 파일의 .meta(AssetImporter.userData)에 저장합니다.
    /// 개인 설정(EditorPrefs)이 아니라 .meta에 두는 이유는, 어디까지가 헤더인지·어떤 구분 기호로 읽는지는 파일의 성질이라
    /// VCS로 팀원과 함께 공유되어야 하기 때문입니다.
    /// </summary>
    public static class TableAssetSettings
    {
        // 값이 없음을 나타낸다. JsonUtility는 null 값을 쓰지 못해 숫자로 구분한다.
        private const int Unset = -1;

        [Serializable]
        private sealed class Settings
        {
            public int headerRowCount;
            public int encodingCodePage;
            public int delimiter = Unset;
            public int quote = Unset;
            public int quoteMode = Unset;

            public bool IsDefault =>
                headerRowCount <= 0 && encodingCodePage == 0 && delimiter == Unset && quote == Unset && quoteMode == Unset;
        }

        // 다른 도구가 userData를 쓰고 있을 때 우리 것과 구분하는 데 쓴다. 이 패키지가 쓰는 JSON에는 항상 들어 있다.
        private const string HeaderRowCountKey = "\"headerRowCount\"";

        /// <summary>
        /// userData 문자열에서 헤더 행 수를 읽습니다. 비어 있거나 이 패키지의 형식이 아니면 0입니다.
        /// </summary>
        public static int ParseHeaderRowCount(string userData)
        {
            return Math.Max(0, Parse(userData).headerRowCount);
        }

        /// <summary>
        /// 헤더 행 수만 담은 userData 문자열을 만듭니다. 0이면 .meta를 깨끗하게 두도록 빈 문자열입니다.
        /// </summary>
        public static string FormatHeaderRowCount(int headerRowCount)
        {
            return Format(new Settings { headerRowCount = Math.Max(0, headerRowCount) });
        }

        /// <summary>
        /// userData 문자열에서 파일 형식 재정의를 읽습니다. 없으면 빈 재정의입니다.
        /// </summary>
        public static TableFileFormatOverride ParseFileFormat(string userData)
        {
            Settings settings = Parse(userData);

            return new TableFileFormatOverride
            {
                EncodingCodePage = Math.Max(0, settings.encodingCodePage),
                Delimiter = settings.delimiter >= 0 ? (char)settings.delimiter : null,
                Quote = settings.quote >= 0 ? (char)settings.quote : null,
                QuoteMode = Enum.IsDefined(typeof(TableQuoteMode), settings.quoteMode)
                    ? (TableQuoteMode)settings.quoteMode
                    : null,
            };
        }

        /// <summary>
        /// 기존 userData의 헤더 행 수는 유지한 채 파일 형식 재정의만 바꾼 userData 문자열을 만듭니다.
        /// </summary>
        public static string FormatFileFormat(string userData, TableFileFormatOverride format)
        {
            Settings settings = Parse(userData);
            format ??= new TableFileFormatOverride();

            settings.encodingCodePage = format.EncodingCodePage;
            settings.delimiter = format.Delimiter.HasValue ? format.Delimiter.Value : Unset;
            settings.quote = format.Quote.HasValue ? format.Quote.Value : Unset;
            settings.quoteMode = format.QuoteMode.HasValue ? (int)format.QuoteMode.Value : Unset;

            return Format(settings);
        }

        /// <summary>
        /// userData가 비어 있거나 이 패키지가 쓴 값인지 확인합니다. 아니면 덮어쓰면 안 됩니다.
        /// </summary>
        public static bool IsOwnedOrEmpty(string userData)
        {
            if (string.IsNullOrWhiteSpace(userData))
                return true;

            string trimmed = userData.Trim();
            return trimmed.StartsWith("{", StringComparison.Ordinal) && trimmed.Contains(HeaderRowCountKey);
        }

        /// <summary>
        /// 파일의 .meta에 기록된 헤더 행 수를 읽습니다.
        /// </summary>
        public static int LoadHeaderRowCount(string assetPath)
        {
            return ParseHeaderRowCount(ReadUserData(assetPath));
        }

        /// <summary>
        /// 파일의 .meta에 기록된 파일 형식 재정의를 읽습니다.
        /// </summary>
        public static TableFileFormatOverride LoadFileFormat(string assetPath)
        {
            return ParseFileFormat(ReadUserData(assetPath));
        }

        /// <summary>
        /// 헤더 행 수를 파일의 .meta에 기록합니다. 파일 형식 재정의는 그대로 둡니다. 재임포트는 하지 않습니다.
        /// </summary>
        /// <returns>기록했으면 true입니다. 다른 도구의 userData가 있어 기록하지 않았으면 false입니다.</returns>
        public static bool SaveHeaderRowCount(string assetPath, int headerRowCount)
        {
            return WriteUserData(assetPath, "header rows", userData =>
            {
                Settings settings = Parse(userData);
                settings.headerRowCount = Math.Max(0, headerRowCount);
                return Format(settings);
            });
        }

        /// <summary>
        /// 파일 형식 재정의를 파일의 .meta에 기록합니다. 헤더 행 수는 그대로 둡니다. 재임포트는 하지 않습니다.
        /// </summary>
        /// <returns>기록했으면 true입니다. 다른 도구의 userData가 있어 기록하지 않았으면 false입니다.</returns>
        public static bool SaveFileFormat(string assetPath, TableFileFormatOverride format)
        {
            return WriteUserData(assetPath, "file format", userData => FormatFileFormat(userData, format));
        }

        // 우리 형식이 아니거나 읽을 수 없으면 기본값이다.
        private static Settings Parse(string userData)
        {
            Settings settings = new();

            if (string.IsNullOrWhiteSpace(userData) || IsOwnedOrEmpty(userData) == false)
                return settings;

            try
            {
                // FromJson은 빠진 필드의 초기값(Unset)을 보장하지 않으므로, 초기값을 가진 인스턴스에 덮어쓴다.
                // 헤더 행 수만 쓰던 예전 userData도 형식 항목은 '자동'으로 읽힌다.
                JsonUtility.FromJsonOverwrite(userData, settings);
            }
            catch (ArgumentException)
            {
                return new Settings();
            }

            return settings;
        }

        // 모두 기본값이면 .meta를 깨끗하게 두도록 빈 문자열이다. 정해진 항목만 적어 diff를 짧게 유지한다.
        // headerRowCount는 0이어도 항상 적는다. 이 키로 우리 userData인지 알아본다(IsOwnedOrEmpty).
        private static string Format(Settings settings)
        {
            if (settings.IsDefault)
                return string.Empty;

            StringBuilder json = new();
            json.Append("{\"headerRowCount\":").Append(Math.Max(0, settings.headerRowCount));

            if (settings.encodingCodePage > 0)
                json.Append(",\"encodingCodePage\":").Append(settings.encodingCodePage);

            if (settings.delimiter != Unset)
                json.Append(",\"delimiter\":").Append(settings.delimiter);

            if (settings.quote != Unset)
                json.Append(",\"quote\":").Append(settings.quote);

            if (settings.quoteMode != Unset)
                json.Append(",\"quoteMode\":").Append(settings.quoteMode);

            return json.Append('}').ToString();
        }

        private static string ReadUserData(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return string.Empty;

            AssetImporter importer = AssetImporter.GetAtPath(assetPath);
            return importer == null ? string.Empty : importer.userData;
        }

        private static bool WriteUserData(string assetPath, string settingName, Func<string, string> update)
        {
            if (string.IsNullOrEmpty(assetPath))
                return false;

            AssetImporter importer = AssetImporter.GetAtPath(assetPath);

            if (importer == null)
                return false;

            if (IsOwnedOrEmpty(importer.userData) == false)
            {
                Debug.LogWarning(
                    $"[Tabular Editor] Did not save {settingName}: the userData in {assetPath}.meta is used by another tool.");
                return false;
            }

            string userData = update(importer.userData);

            if (importer.userData == userData)
                return true;

            importer.userData = userData;
            EditorUtility.SetDirty(importer);
            AssetDatabase.WriteImportSettingsIfDirty(assetPath);
            return true;
        }
    }
}
