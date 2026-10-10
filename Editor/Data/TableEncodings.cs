using System;
using System.Collections.Generic;
using System.Text;

namespace NKStudio.TabularEditor.Data
{
    /// <summary>
    /// 파일 형식 대화상자에서 고를 수 있는 인코딩 목록입니다.
    /// BOM으로 알아낼 수 없는 인코딩(UTF-8 BOM 없음 이외의 레거시 코드 페이지)은 .meta에 코드 페이지로 기록해 다시 열 때 쓴다.
    /// </summary>
    public static class TableEncodings
    {
        /// <summary>
        /// 대화상자에 보일 인코딩 하나입니다.
        /// </summary>
        public sealed class Entry
        {
            internal Entry(string label, int codePage, bool hasBom)
            {
                Label = label;
                CodePage = codePage;
                HasBom = hasBom;
            }

            public string Label { get; }

            public int CodePage { get; }

            /// <summary>
            /// 저장할 때 파일 앞에 BOM을 쓰는지 여부입니다. UTF-8·UTF-16만 의미가 있다.
            /// </summary>
            public bool HasBom { get; }

            public Encoding Create()
            {
                return CodePage switch
                {
                    Utf8CodePage => new UTF8Encoding(HasBom),
                    Utf16LeCodePage => new UnicodeEncoding(false, HasBom),
                    Utf16BeCodePage => new UnicodeEncoding(true, HasBom),
                    _ => Encoding.GetEncoding(CodePage),
                };
            }
        }

        public const int Utf8CodePage = 65001;
        public const int Utf16LeCodePage = 1200;
        public const int Utf16BeCodePage = 1201;

        // 한국 팀이 엑셀에서 저장한 CSV(CP949)를 자주 만나므로 한국어 인코딩을 UTF 계열 바로 뒤에 둔다.
        private static readonly Entry[] AllEntries =
        {
            new("UTF-8", Utf8CodePage, false),
            new("UTF-8 with BOM", Utf8CodePage, true),
            new("UTF-16 LE", Utf16LeCodePage, true),
            new("UTF-16 BE", Utf16BeCodePage, true),
            new("한국어 (CP949)", 949, false),
            new("한국어 (EUC-KR)", 51949, false),
            new("Windows 1252", 1252, false),
            new("ISO 8859-1", 28591, false),
            new("ISO 8859-15", 28605, false),
            new("Mac Roman", 10000, false),
            new("Windows 1251", 1251, false),
            new("KOI8-R", 20866, false),
            new("Windows 1250", 1250, false),
            new("일본어 (Shift_JIS)", 932, false),
            new("일본어 (EUC-JP)", 51932, false),
            new("중국어 간체 (GBK)", 936, false),
            new("중국어 간체 (GB18030)", 54936, false),
            new("중국어 번체 (Big5)", 950, false),
        };

        public static IReadOnlyList<Entry> Entries => AllEntries;

        /// <summary>
        /// 인코딩에 해당하는 목록 위치를 찾습니다. 목록에 없으면 -1입니다.
        /// </summary>
        public static int IndexOf(Encoding encoding)
        {
            if (encoding == null)
                return 0;

            bool hasBom = encoding.GetPreamble().Length > 0;

            for (int index = 0; index < AllEntries.Length; index++)
            {
                Entry entry = AllEntries[index];

                if (entry.CodePage != encoding.CodePage)
                    continue;

                // BOM은 UTF 계열에서만 구분한다. 레거시 코드 페이지는 BOM이 없다.
                if (IsUnicode(entry.CodePage) && entry.HasBom != hasBom)
                    continue;

                return index;
            }

            return -1;
        }

        /// <summary>
        /// 파일 앞의 BOM만으로 다시 알아낼 수 있는 인코딩인지 여부입니다. 아니면 .meta에 기록해야 다음에도 같은 인코딩으로 읽는다.
        /// BOM 없는 UTF-8은 기본값이라 기록하지 않아도 된다.
        /// </summary>
        public static bool IsDetectable(Encoding encoding)
        {
            if (encoding == null)
                return true;

            if (encoding.CodePage == Utf8CodePage)
                return true;

            return IsUnicode(encoding.CodePage) && encoding.GetPreamble().Length > 0;
        }

        /// <summary>
        /// 코드 페이지로 인코딩을 만듭니다. 이 런타임이 지원하지 않으면 null입니다.
        /// </summary>
        public static Encoding TryCreate(int codePage)
        {
            foreach (Entry entry in AllEntries)
            {
                if (entry.CodePage == codePage && entry.HasBom == false)
                    return entry.Create();
            }

            try
            {
                return Encoding.GetEncoding(codePage);
            }
            catch (Exception exception) when (exception is ArgumentException or NotSupportedException)
            {
                return null;
            }
        }

        private static bool IsUnicode(int codePage)
        {
            return codePage is Utf8CodePage or Utf16LeCodePage or Utf16BeCodePage;
        }
    }
}
