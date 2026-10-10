using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace NKStudio.TabularEditor.Data
{
    /// <summary>
    /// 테이블 문서를 파일에서 읽고 파일로 씁니다. 인코딩과 개행 형태를 보존합니다.
    /// </summary>
    public static class TableDocumentIO
    {
        /// <summary>
        /// 프로젝트 상대 경로를 절대 경로로 변환합니다.
        /// </summary>
        /// <param name="projectRelativePath">Assets 또는 Packages로 시작하는 경로입니다.</param>
        /// <returns>절대 경로입니다.</returns>
        public static string GetFullPath(string projectRelativePath)
        {
            if (string.IsNullOrEmpty(projectRelativePath))
                return string.Empty;

            if (Path.IsPathRooted(projectRelativePath))
                return projectRelativePath;

            return Path.GetFullPath(
                Path.Combine(Directory.GetCurrentDirectory(), projectRelativePath));
        }

        /// <summary>
        /// 파일을 읽어 테이블 문서를 만듭니다.
        /// </summary>
        /// <param name="projectRelativePath">읽을 파일의 프로젝트 상대 경로입니다.</param>
        /// <returns>생성된 문서입니다. 파일이 없으면 빈 문서를 반환합니다.</returns>
        public static TableDocument Load(string projectRelativePath)
        {
            return Load(projectRelativePath, out _);
        }

        /// <summary>
        /// 파일을 읽어 테이블 문서를 만들고, 읽은 바이트의 해시도 함께 계산합니다.
        /// Unity API를 쓰지 않으므로 백그라운드 스레드에서 호출해도 된다.
        /// </summary>
        /// <param name="projectRelativePath">읽을 파일의 프로젝트 상대 경로입니다.</param>
        /// <param name="fileHash">읽은 파일 내용의 해시입니다. 파일이 없으면 빈 문자열입니다.</param>
        /// <returns>생성된 문서입니다. 파일이 없으면 빈 문서를 반환합니다.</returns>
        public static TableDocument Load(string projectRelativePath, out string fileHash)
        {
            return Load(projectRelativePath, null, out fileHash);
        }

        /// <summary>
        /// 파일 형식 재정의(.meta에 기록된 인코딩·구분 기호·따옴표)를 적용해 파일을 읽습니다.
        /// 재정의가 비어 있는 항목은 BOM·확장자·기본값으로 정한다. Unity API를 쓰지 않으므로 백그라운드 스레드에서 호출해도 된다.
        /// </summary>
        /// <param name="projectRelativePath">읽을 파일의 프로젝트 상대 경로입니다.</param>
        /// <param name="formatOverride">파일 형식 재정의입니다. null이면 모두 자동입니다.</param>
        /// <param name="fileHash">읽은 파일 내용의 해시입니다. 파일이 없으면 빈 문자열입니다.</param>
        public static TableDocument Load(string projectRelativePath, TableFileFormatOverride formatOverride, out string fileHash)
        {
            fileHash = string.Empty;

            TableDocument document = new();
            document.AssetPath = projectRelativePath;

            TableFormatUtility.TryGetFormat(projectRelativePath, out TableFormat format);
            document.Format = format;

            TableFileOptions options = CreateOptions(format, formatOverride);
            string fullPath = GetFullPath(projectRelativePath);

            if (string.IsNullOrEmpty(fullPath) || !File.Exists(fullPath))
            {
                document.FileOptions = options;
                document.SetContent(null);
                return document;
            }

            byte[] bytes = File.ReadAllBytes(fullPath);

            // 저장 시 외부 변경 감지의 기준이 되므로, 다시 읽지 않고 파싱한 바로 그 바이트로 계산한다.
            fileHash = ComputeHash(bytes);

            // BOM이 있으면 BOM이 우선이다. 없을 때만 재정의 인코딩(예: CP949)으로 읽는다.
            Encoding detected = DetectEncoding(bytes, out int preambleLength);
            Encoding overridden = preambleLength == 0 && formatOverride != null && formatOverride.EncodingCodePage > 0
                ? TableEncodings.TryCreate(formatOverride.EncodingCodePage)
                : null;

            options.Encoding = overridden ?? detected;

            string text = options.Encoding.GetString(
                bytes,
                preambleLength,
                bytes.Length - preambleLength);

            List<List<CellSlot>> rows = DelimitedTextParser.ParseSlots(text, options.Delimiter, options);

            document.FileOptions = options;
            document.SetParsedContent(text, rows, options.Quote);

            return document;
        }

        // 파일 내용과 상관없는 형식(구분 기호·따옴표 규칙)을 확장자와 재정의로 정한다. 인코딩·개행은 파일을 읽으며 정해진다.
        private static TableFileOptions CreateOptions(TableFormat format, TableFileFormatOverride formatOverride)
        {
            return new TableFileOptions
            {
                Delimiter = formatOverride?.Delimiter ?? TableFormatUtility.GetDelimiter(format),
                Quote = formatOverride?.Quote ?? '"',
                QuoteMode = formatOverride?.QuoteMode ?? TableQuoteMode.Minimal,
            };
        }

        /// <summary>
        /// 테이블 문서를 파일로 저장합니다.
        /// </summary>
        /// <param name="document">저장할 문서입니다.</param>
        public static void Save(TableDocument document)
        {
            if (document == null || string.IsNullOrEmpty(document.AssetPath))
                return;

            string fullPath = GetFullPath(document.AssetPath);
            string directory = Path.GetDirectoryName(fullPath);

            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllBytes(fullPath, Serialize(document));
        }

        /// <summary>
        /// 테이블 문서를 파일에 기록할 바이트 배열로 직렬화합니다.
        /// </summary>
        /// <param name="document">직렬화할 문서입니다.</param>
        /// <returns>BOM을 포함한 파일 바이트입니다.</returns>
        public static byte[] Serialize(TableDocument document)
        {
            TableFileOptions options = document.FileOptions ?? new TableFileOptions();
            string text = DelimitedTextWriter.Write(document, options);

            Encoding encoding = options.Encoding ?? new UTF8Encoding(false);
            byte[] preamble = encoding.GetPreamble();
            byte[] body = encoding.GetBytes(text);

            if (preamble.Length == 0)
                return body;

            byte[] result = new byte[preamble.Length + body.Length];
            Buffer.BlockCopy(preamble, 0, result, 0, preamble.Length);
            Buffer.BlockCopy(body, 0, result, preamble.Length, body.Length);

            return result;
        }

        /// <summary>
        /// 파일 내용의 해시를 계산합니다. 외부 변경 감지에 사용합니다.
        /// </summary>
        /// <param name="projectRelativePath">대상 파일의 프로젝트 상대 경로입니다.</param>
        /// <returns>파일 해시 문자열입니다. 파일이 없으면 빈 문자열입니다.</returns>
        public static string ComputeFileHash(string projectRelativePath)
        {
            string fullPath = GetFullPath(projectRelativePath);

            if (string.IsNullOrEmpty(fullPath) || !File.Exists(fullPath))
                return string.Empty;

            return ComputeHash(File.ReadAllBytes(fullPath));
        }

        private static string ComputeHash(byte[] bytes)
        {
            using MD5 md5 = MD5.Create();
            return Convert.ToBase64String(md5.ComputeHash(bytes));
        }

        /// <summary>
        /// 바이트 배열의 BOM을 검사해 인코딩을 판별합니다.
        /// </summary>
        /// <param name="bytes">검사할 파일 바이트입니다.</param>
        /// <param name="preambleLength">판별된 BOM의 바이트 길이입니다.</param>
        /// <returns>판별된 인코딩입니다. BOM이 없으면 BOM 없는 UTF-8입니다.</returns>
        public static Encoding DetectEncoding(byte[] bytes, out int preambleLength)
        {
            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            {
                preambleLength = 3;
                return new UTF8Encoding(true);
            }

            if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
            {
                preambleLength = 2;
                return new UnicodeEncoding(false, true);
            }

            if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
            {
                preambleLength = 2;
                return new UnicodeEncoding(true, true);
            }

            preambleLength = 0;
            return new UTF8Encoding(false);
        }
    }
}
