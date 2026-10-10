using System;
using System.IO;
using NKStudio.TabularEditor.Data;
using NKStudio.TabularEditor.Window;
using UnityEditor;

namespace NKStudio.TabularEditor.Import
{
    /// <summary>
    /// 프로젝트 창의 Create 메뉴에서 새 CSV 파일을 만듭니다. 크기와 파일 형식은 Preferences > Tabular Editor의 '새 CSV 파일'을 따릅니다.
    /// </summary>
    public static class TableAssetCreateMenu
    {
        private const string DefaultFileName = "NewTable.csv";

        // Unity 템플릿 생성 경로에 넘길 임시 템플릿이다. 프로젝트의 Temp 폴더라 에셋으로 임포트되지 않는다.
        private const string TemplateDirectory = "Temp/TabularEditor";

        // 이름 입력을 취소하면 파일이 만들어지지 않아 요청이 남는다. 오래된 요청이 엉뚱한 파일에 적용되지 않게 버린다.
        private static readonly TimeSpan PendingLifetime = TimeSpan.FromMinutes(10);

        private static PendingCreation _pending;

        private sealed class PendingCreation
        {
            public string NormalizedText;
            public int Rows;
            public int Columns;
            public TableFileOptions Options;
            public DateTime CreatedAtUtc;
        }

        // Unity가 C# 스크립트를 만들 때와 같은 경로라, 파일이 생기자마자 이름을 입력받는 인라인 편집을 그대로 쓴다.
        // 메뉴 경로는 내부 이름(Scripting)으로 지정해야 한국어 에디터의 "스크립팅" 하위 메뉴에 합쳐진다.
        [MenuItem("Assets/Create/Scripting/CSV File")]
        private static void CreateCsvFile()
        {
            int rows = TableEditorSettings.NewTableRows;
            int columns = TableEditorSettings.NewTableColumns;
            TableFileOptions options = TableEditorSettings.CreateNewTableOptions();
            string text = TableTemplateBuilder.CreateNormalizedText(rows, columns, options);

            Directory.CreateDirectory(TemplateDirectory);
            string templatePath = Path.Combine(TemplateDirectory, "NewTable.csv.txt");
            File.WriteAllText(templatePath, text);

            // TRAP: 템플릿 생성 경로는 UTF-8로만 쓰고 개행을 프로젝트 설정(Line Endings For New Scripts)으로 바꾼다.
            // 고른 인코딩·개행이 그대로 남도록, 파일이 임포트되면 아래 후처리기가 그 형식으로 다시 쓴다.
            _pending = new PendingCreation
            {
                NormalizedText = text,
                Rows = rows,
                Columns = columns,
                Options = options,
                CreatedAtUtc = DateTime.UtcNow,
            };

            ProjectWindowUtil.CreateScriptAssetFromTemplateFile(Path.GetFullPath(templatePath), DefaultFileName);
        }

        /// <summary>
        /// 새로 임포트된 CSV가 방금 메뉴로 만든 파일이면, 고른 형식으로 다시 쓰고 .meta에 형식을 남깁니다.
        /// </summary>
        internal static void OnCsvImported(string assetPath)
        {
            PendingCreation pending = _pending;

            if (pending == null)
                return;

            if (DateTime.UtcNow - pending.CreatedAtUtc > PendingLifetime)
            {
                _pending = null;
                return;
            }

            string fullPath = TableDocumentIO.GetFullPath(assetPath);

            if (!File.Exists(fullPath))
                return;

            // 같은 순간 다른 CSV가 임포트될 수 있으므로, 내용이 방금 만든 템플릿과 같을 때만 우리 파일로 본다.
            string written = File.ReadAllText(fullPath).Replace("\r\n", "\n");

            if (written != pending.NormalizedText)
                return;

            _pending = null;

            // 임포트 콜백 안에서 바로 파일을 바꾸면 임포트가 겹친다. 이번 임포트가 끝난 뒤에 쓴다.
            EditorApplication.delayCall += () =>
            {
                File.WriteAllBytes(fullPath, TableTemplateBuilder.CreateFileBytes(pending.Rows, pending.Columns, pending.Options));
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

                TableFormatUtility.TryGetFormat(assetPath, out TableFormat format);
                TableAssetSettings.SaveFileFormat(assetPath, TableFileFormatOverride.From(pending.Options, format));
            };
        }
    }

    /// <summary>
    /// 메뉴로 만든 CSV가 임포트되는 순간을 잡아 <see cref="TableAssetCreateMenu"/>에 넘깁니다.
    /// </summary>
    internal sealed class TableAssetCreatePostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            foreach (string assetPath in importedAssets)
            {
                if (assetPath.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                    TableAssetCreateMenu.OnCsvImported(assetPath);
            }
        }
    }
}
