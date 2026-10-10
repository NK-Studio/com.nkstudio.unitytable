using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NKStudio.TabularEditor.AssetLinks
{
    /// <summary>
    /// 셀 값을 에셋 경로로 해석합니다.
    /// <list type="bullet">
    /// <item>Resources 기준 경로(확장자 없음): <c>Art/Sprite/Buff/spr_buff_1000</c> → 어느 Resources 폴더든 그 아래의 <c>spr_buff_1000.*</c></item>
    /// <item>프로젝트 경로: <c>Assets/…/foo.png</c>, <c>Packages/…</c> → 그 에셋</item>
    /// </list>
    /// Resources 경로는 처음 한 번 전체 에셋 목록을 훑어 색인을 만들고, 에셋이 바뀌면 다시 만든다.
    /// 존재 여부만 볼 때는 에셋을 불러오지 않는다(미리보기를 띄울 때만 불러온다).
    /// </summary>
    public static class AssetPathIndex
    {
        private const string ResourcesSegment = "/Resources/";

        // Resources.Load는 대소문자를 가리는 플랫폼이 있어, 미리보기도 같은 글자일 때만 맞다고 본다.
        private static Dictionary<string, string> _resourcesPaths;

        /// <summary>
        /// 에셋이 추가·삭제·이동되어 해석 결과가 바뀔 수 있을 때 호출됩니다.
        /// </summary>
        public static event Action Changed;

        public static bool TryResolve(string value, out string assetPath)
        {
            assetPath = null;

            if (string.IsNullOrWhiteSpace(value))
                return false;

            string trimmed = value.Trim();

            if (trimmed.StartsWith("Assets/", StringComparison.Ordinal) || trimmed.StartsWith("Packages/", StringComparison.Ordinal))
            {
                string guid = AssetDatabase.AssetPathToGUID(trimmed, AssetPathToGUIDOptions.OnlyExistingAssets);

                if (string.IsNullOrEmpty(guid) || AssetDatabase.IsValidFolder(trimmed))
                    return false;

                assetPath = trimmed;
                return true;
            }

            _resourcesPaths ??= BuildResourcesIndex();
            return _resourcesPaths.TryGetValue(trimmed, out assetPath);
        }

        /// <summary>
        /// 에셋을 불러옵니다. 미리보기·Ping처럼 실제 오브젝트가 필요할 때만 쓴다.
        /// </summary>
        public static UnityEngine.Object Load(string assetPath)
        {
            return string.IsNullOrEmpty(assetPath) ? null : AssetDatabase.LoadMainAssetAtPath(assetPath);
        }

        /// <summary>
        /// Project 창의 작은 아이콘입니다. 텍스처는 그림 썸네일이 나온다(Unity가 아직 만들지 않았으면 종류 아이콘).
        /// </summary>
        public static Texture GetIcon(string assetPath)
        {
            return AssetDatabase.GetCachedIcon(assetPath);
        }

        /// <summary>
        /// 찾을 수 없는 경로 셀에 붙이는 Unity 기본 경고 아이콘입니다. large면 미리보기 카드용 큰 아이콘입니다.
        /// </summary>
        public static Texture GetMissingIcon(bool large)
        {
            return EditorGUIUtility.IconContent(large ? "console.warnicon" : "console.warnicon.sml").image;
        }

        /// <summary>
        /// 찾을 수 없는 값이 어디를 찾아봤는지 설명합니다. 예) Resources/Art/Sprite/x.* (Resources 경로) 또는 Assets/a.png
        /// </summary>
        public static string DescribeLookup(string value)
        {
            string trimmed = value?.Trim() ?? string.Empty;
            bool isProjectPath = trimmed.StartsWith("Assets/", StringComparison.Ordinal) || trimmed.StartsWith("Packages/", StringComparison.Ordinal);

            return isProjectPath
                ? Localization.Format("asset.missingProject", trimmed)
                : Localization.Format("asset.missingResources", trimmed);
        }

        /// <summary>
        /// Project 창에서 에셋 위치를 깜빡여 보여 줍니다. Unity 선택은 바꾸지 않는다.
        /// </summary>
        public static void Ping(string assetPath)
        {
            UnityEngine.Object asset = Load(assetPath);

            if (asset != null)
                EditorGUIUtility.PingObject(asset);
        }

        internal static void Invalidate()
        {
            _resourcesPaths = null;
            Changed?.Invoke();
        }

        private static Dictionary<string, string> BuildResourcesIndex()
        {
            Dictionary<string, string> paths = new(StringComparer.Ordinal);

            foreach (string path in AssetDatabase.GetAllAssetPaths())
            {
                int resourcesIndex = path.LastIndexOf(ResourcesSegment, StringComparison.Ordinal);

                if (resourcesIndex < 0)
                    continue;

                string relative = path.Substring(resourcesIndex + ResourcesSegment.Length);
                int slash = relative.LastIndexOf('/');
                int dot = relative.LastIndexOf('.');

                // 확장자가 없으면 폴더일 수 있다. 폴더는 Resources.Load 대상이 아니다.
                if (dot <= slash)
                {
                    if (AssetDatabase.IsValidFolder(path))
                        continue;
                }
                else
                {
                    relative = relative.Substring(0, dot);
                }

                // 같은 경로가 여러 Resources 폴더에 있으면 Resources.Load처럼 어느 하나가 잡힌다. 처음 것을 쓴다.
                paths.TryAdd(relative, path);
            }

            return paths;
        }
    }

    /// <summary>
    /// 에셋이 바뀌면 색인을 버립니다. 표 파일(.csv·.tsv) 저장만으로는 색인을 다시 만들지 않는다.
    /// </summary>
    internal sealed class AssetPathIndexPostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            if (HasNonTableChange(importedAssets) || HasNonTableChange(deletedAssets) || HasNonTableChange(movedAssets))
                AssetPathIndex.Invalidate();
        }

        private static bool HasNonTableChange(string[] paths)
        {
            foreach (string path in paths)
            {
                if (path.EndsWith(".csv", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".tsv", StringComparison.OrdinalIgnoreCase))
                    continue;

                return true;
            }

            return false;
        }
    }
}
