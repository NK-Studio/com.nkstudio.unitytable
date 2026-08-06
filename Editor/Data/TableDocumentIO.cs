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
            TableDocument document = new();
            document.AssetPath = projectRelativePath;

            TableFormatUtility.TryGetFormat(projectRelativePath, out TableFormat format);
            document.Format = format;

            string fullPath = GetFullPath(projectRelativePath);

            if (string.IsNullOrEmpty(fullPath) || !File.Exists(fullPath))
            {
                document.SetContent(null);
                return document;
            }

            byte[] bytes = File.ReadAllBytes(fullPath);
            TableFileOptions options = new();
            options.Encoding = DetectEncoding(bytes, out int preambleLength);

            string text = options.Encoding.GetString(
                bytes,
                preambleLength,
                bytes.Length - preambleLength);

            char delimiter = TableFormatUtility.GetDelimiter(document.Format);
            List<List<string>> rows = DelimitedTextParser.Parse(text, delimiter, options);

            document.FileOptions = options;
            document.SetContent(rows);

            return document;
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
            char delimiter = TableFormatUtility.GetDelimiter(document.Format);

            string text = DelimitedTextWriter.Write(
                document.GetRows(),
                delimiter,
                options.NewLine,
                options.EndsWithNewLine);

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

            using MD5 md5 = MD5.Create();
            byte[] hash = md5.ComputeHash(File.ReadAllBytes(fullPath));

            return Convert.ToBase64String(hash);
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
