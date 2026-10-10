using System;
using UnityEditor;
using UnityEngine;

namespace NKStudio.TabularEditor.Data
{
    /// <summary>
    /// 파일별 보기 설정(헤더 행 수)을 그 파일의 .meta(AssetImporter.userData)에 저장합니다.
    /// 개인 설정(EditorPrefs)이 아니라 .meta에 두는 이유는, 어디까지가 헤더인지는 파일의 성질이라
    /// VCS로 팀원과 함께 공유되어야 하기 때문입니다.
    /// </summary>
    public static class TableAssetSettings
    {
        [Serializable]
        private sealed class Settings
        {
            public int headerRowCount;
        }

        // 다른 도구가 userData를 쓰고 있을 때 우리 것과 구분하는 데 쓴다.
        private const string HeaderRowCountKey = "\"headerRowCount\"";

        /// <summary>
        /// userData 문자열에서 헤더 행 수를 읽습니다. 비어 있거나 이 패키지의 형식이 아니면 0입니다.
        /// </summary>
        public static int ParseHeaderRowCount(string userData)
        {
            if (string.IsNullOrWhiteSpace(userData) || IsOwnedOrEmpty(userData) == false)
                return 0;

            try
            {
                Settings settings = JsonUtility.FromJson<Settings>(userData);
                return settings == null ? 0 : Math.Max(0, settings.headerRowCount);
            }
            catch (ArgumentException)
            {
                return 0;
            }
        }

        /// <summary>
        /// 헤더 행 수를 userData 문자열로 만듭니다. 0이면 .meta를 깨끗하게 두도록 빈 문자열입니다.
        /// </summary>
        public static string FormatHeaderRowCount(int headerRowCount)
        {
            if (headerRowCount <= 0)
                return string.Empty;

            return JsonUtility.ToJson(new Settings { headerRowCount = headerRowCount });
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
            if (string.IsNullOrEmpty(assetPath))
                return 0;

            AssetImporter importer = AssetImporter.GetAtPath(assetPath);
            return importer == null ? 0 : ParseHeaderRowCount(importer.userData);
        }

        /// <summary>
        /// 헤더 행 수를 파일의 .meta에 기록합니다. 재임포트는 하지 않습니다.
        /// </summary>
        /// <returns>기록했으면 true입니다. 다른 도구의 userData가 있어 기록하지 않았으면 false입니다.</returns>
        public static bool SaveHeaderRowCount(string assetPath, int headerRowCount)
        {
            if (string.IsNullOrEmpty(assetPath))
                return false;

            AssetImporter importer = AssetImporter.GetAtPath(assetPath);

            if (importer == null)
                return false;

            if (IsOwnedOrEmpty(importer.userData) == false)
            {
                Debug.LogWarning(
                    $"[Tabular Editor] {assetPath}.meta의 userData를 다른 도구가 쓰고 있어 헤더 행 설정을 저장하지 않았습니다.");
                return false;
            }

            string userData = FormatHeaderRowCount(headerRowCount);

            if (importer.userData == userData)
                return true;

            importer.userData = userData;
            EditorUtility.SetDirty(importer);
            AssetDatabase.WriteImportSettingsIfDirty(assetPath);
            return true;
        }
    }
}
