using System;
using UnityEditor;
using UnityEngine.UIElements;

namespace NKStudio.TabularEditor.Window
{
    public enum TableEditorThemeStyle
    {
        Unity,
        AndroidStudio,
    }

    /// <summary>
    /// 테이블 에디터의 테마 설정입니다. 프로젝트가 아니라 EditorPrefs(사용자 Preferences)에 저장하므로
    /// 같은 프로젝트를 쓰는 팀원마다 다르게 고를 수 있습니다.
    /// </summary>
    public static class TableEditorTheme
    {
        private const string StyleKey = "NKStudio.TabularEditor.ThemeStyle";

        // Unity 테마는 기본 USS 그대로라 클래스가 필요 없다. Android Studio 테마만 이 클래스 아래에서 색을 덮어쓴다.
        private const string AndroidStudioClass = "table-editor--android-studio";
        private const string DarkClass = "table-editor--dark";
        private const string LightClass = "table-editor--light";

        /// <summary>
        /// 테마가 바뀌면 호출됩니다. 열려 있는 모든 창이 함께 갱신됩니다.
        /// </summary>
        public static event Action Changed;

        public static TableEditorThemeStyle Style
        {
            get => (TableEditorThemeStyle)EditorPrefs.GetInt(StyleKey, (int)TableEditorThemeStyle.Unity);
            set
            {
                if (Style == value)
                    return;

                EditorPrefs.SetInt(StyleKey, (int)value);
                Changed?.Invoke();
            }
        }

        /// <summary>
        /// 다크/라이트는 항상 Unity 에디터 스킨을 따릅니다.
        /// </summary>
        public static bool IsDark => EditorGUIUtility.isProSkin;

        public static string DisplayName(TableEditorThemeStyle style)
        {
            return style == TableEditorThemeStyle.Unity ? "Unity" : "Android Studio";
        }

        /// <summary>
        /// element 아래에 현재 테마를 적용합니다.
        /// </summary>
        public static void Apply(VisualElement element)
        {
            if (element == null)
                return;

            bool dark = IsDark;
            element.EnableInClassList(AndroidStudioClass, Style == TableEditorThemeStyle.AndroidStudio);
            element.EnableInClassList(DarkClass, dark);
            element.EnableInClassList(LightClass, !dark);
        }
    }
}
