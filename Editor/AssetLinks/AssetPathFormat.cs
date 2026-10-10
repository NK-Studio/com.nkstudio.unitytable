using System;
using System.Collections.Generic;

namespace NKStudio.TabularEditor.AssetLinks
{
    public enum AssetPathStyle
    {
        /// <summary>
        /// Resources 기준 경로, 확장자 없음. 예) Art/Sprite/Buff/spr_buff_1000
        /// </summary>
        Resources,

        /// <summary>
        /// 프로젝트 경로. 예) Assets/Art/spr_buff_1000.png
        /// </summary>
        Project,
    }

    /// <summary>
    /// Project 창에서 끌어다 놓은 에셋을 셀 값으로 바꾸는 규칙입니다. 열이 이미 쓰는 형식을 따른다.
    /// </summary>
    public static class AssetPathFormat
    {
        private const string ResourcesSegment = "/Resources/";

        /// <summary>
        /// 열의 값이 어떤 형식으로 에셋을 가리키는지 고릅니다. 경로처럼 생긴 값(/ 포함) 중 Assets/·Packages/로 시작하는 값이
        /// 더 많으면 프로젝트 경로, 아니면 Resources 경로다. 경로가 하나도 없으면 null(형식이 정해지지 않은 열)이다.
        /// </summary>
        public static AssetPathStyle? Detect(IEnumerable<string> values)
        {
            int projectCount = 0;
            int resourcesCount = 0;

            foreach (string value in values)
            {
                if (MissingAssets.LooksLikePath(value) == false)
                    continue;

                if (IsProjectPath(value))
                    projectCount++;
                else
                    resourcesCount++;

                if (projectCount + resourcesCount >= AssetColumnDetector.SampleCount)
                    break;
            }

            if (projectCount + resourcesCount == 0)
                return null;

            return projectCount > resourcesCount ? AssetPathStyle.Project : AssetPathStyle.Resources;
        }

        /// <summary>
        /// 에셋 경로(Assets/…)를 셀 값으로 바꿉니다. Resources 형식인데 에셋이 Resources 폴더 밖에 있으면 null이다
        /// (그 값은 Resources.Load로 불러올 수 없다).
        /// </summary>
        public static string ToCellValue(string assetPath, AssetPathStyle style)
        {
            if (string.IsNullOrEmpty(assetPath))
                return null;

            if (style == AssetPathStyle.Project)
                return assetPath;

            int resourcesIndex = assetPath.LastIndexOf(ResourcesSegment, StringComparison.Ordinal);

            if (resourcesIndex < 0)
                return null;

            string relative = assetPath.Substring(resourcesIndex + ResourcesSegment.Length);
            int slash = relative.LastIndexOf('/');
            int dot = relative.LastIndexOf('.');

            return dot > slash ? relative.Substring(0, dot) : relative;
        }

        private static bool IsProjectPath(string value)
        {
            string trimmed = value.Trim();
            return trimmed.StartsWith("Assets/", StringComparison.Ordinal) || trimmed.StartsWith("Packages/", StringComparison.Ordinal);
        }
    }
}
