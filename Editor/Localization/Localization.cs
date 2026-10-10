using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace NKStudio.TabularEditor
{
    public enum Language
    {
        English,
        Korean,
    }

    /// <summary>
    /// 창 문구의 한국어/영어 전환. 선택은 EditorPrefs(사용자 Preferences)에 저장해 개발자마다 다르게 쓸 수 있다.
    /// 처음에는 OS 언어를 따른다(한국어면 한국어, 그 밖에는 영어).
    /// - UXML: text·tooltip·placeholder-text에 "@키"를 쓰면 <see cref="Localize"/>가 번역해 넣는다.
    /// - 코드: <see cref="Bind"/>로 문구를 그리는 함수를 요소에 묶어 두면 언어가 바뀔 때 <see cref="Refresh"/>가 다시 그린다.
    /// </summary>
    public static class Localization
    {
        private const string LanguageKey = "NKStudio.TabularEditor.Language";

        public static event Action Changed;

        private static readonly ConditionalWeakTable<VisualElement, Action> Renderers = new();

        // 테스트가 EditorPrefs를 건드리지 않고 언어를 고정할 때 쓴다(<see cref="Override"/>).
        private static Language? _override;

        public static Language Current
        {
            get
            {
                if (_override.HasValue)
                    return _override.Value;

                int stored = EditorPrefs.GetInt(LanguageKey, -1);

                if (stored >= 0)
                    return (Language)stored;

                return Application.systemLanguage == SystemLanguage.Korean ? Language.Korean : Language.English;
            }
            set
            {
                if (EditorPrefs.GetInt(LanguageKey, -1) == (int)value)
                    return;

                EditorPrefs.SetInt(LanguageKey, (int)value);
                Changed?.Invoke();
            }
        }

        public static string DisplayName(Language language)
        {
            return language == Language.Korean ? "한국어" : "English";
        }

        /// <summary>
        /// 키의 문구를 현재 언어로 반환합니다. 없는 키면 키를 그대로 보여 줘 빠진 번역이 눈에 띄게 한다.
        /// </summary>
        public static string Get(string key)
        {
            if (Strings.Table.TryGetValue(key, out (string en, string ko) pair) == false)
                return key;

            return Current == Language.Korean ? pair.ko : pair.en;
        }

        public static string Format(string key, params object[] args)
        {
            return string.Format(Get(key), args);
        }

        /// <summary>
        /// 개수에 따라 단수/복수 문구를 고릅니다. 키 뒤에 ".one"(1개)·".other"(그 밖) 두 항목을 둔다.
        /// 예) Count("count.row", 1) → "1 row", Count("count.row", 3) → "3 rows" / 한국어 "1 행", "3 행"
        /// </summary>
        public static string Count(string key, int count)
        {
            return Format(count == 1 ? key + ".one" : key + ".other", count);
        }

        /// <summary>
        /// 요소를 render로 그리고, 언어가 바뀌면 <see cref="Refresh"/>에서 다시 그립니다. 요소마다 하나만 묶입니다.
        /// </summary>
        public static void Bind(VisualElement element, Action render)
        {
            if (element == null)
                return;

            Renderers.Remove(element);
            Renderers.Add(element, render);
            render();
        }

        public static void BindText(TextElement element, string key)
        {
            Bind(element, () => element.text = Get(key));
        }

        public static void BindTooltip(VisualElement element, string key)
        {
            Bind(element, () => element.tooltip = Get(key));
        }

        public static void BindPlaceholder(TextField field, string key)
        {
            Bind(field, () => field.textEdition.placeholder = Get(key));
        }

        /// <summary>
        /// UXML에 적힌 "@키" 문구(text·tooltip·placeholder)를 찾아 묶는다.
        /// </summary>
        public static void Localize(VisualElement root)
        {
            root.Query<VisualElement>().ForEach(element =>
            {
                string textKey = element is TextElement textElement ? KeyOf(textElement.text) : null;
                string tooltipKey = KeyOf(element.tooltip);
                string placeholderKey = element is TextField field ? KeyOf(field.textEdition.placeholder) : null;

                if (textKey == null && tooltipKey == null && placeholderKey == null)
                    return;

                Bind(element, () =>
                {
                    if (textKey != null)
                        ((TextElement)element).text = Get(textKey);

                    if (tooltipKey != null)
                        element.tooltip = Get(tooltipKey);

                    if (placeholderKey != null)
                        ((TextField)element).textEdition.placeholder = Get(placeholderKey);
                });
            });
        }

        /// <summary>
        /// root 아래의 묶인 문구를 현재 언어로 다시 그린다.
        /// </summary>
        public static void Refresh(VisualElement root)
        {
            root.Query<VisualElement>().ForEach(element =>
            {
                if (Renderers.TryGetValue(element, out Action render))
                    render();
            });
        }

        /// <summary>
        /// 영어나 한국어 문구가 비어 있는 키, 또는 ".one"/".other" 중 한쪽만 있는 키를 돌려줍니다(테스트용).
        /// </summary>
        public static List<string> FindIncompleteKeys()
        {
            List<string> incomplete = new();

            foreach (KeyValuePair<string, (string en, string ko)> entry in Strings.Table)
            {
                if (string.IsNullOrEmpty(entry.Value.en) || string.IsNullOrEmpty(entry.Value.ko))
                    incomplete.Add(entry.Key);

                string pairKey = PluralPairOf(entry.Key);

                if (pairKey != null && Strings.Table.ContainsKey(pairKey) == false)
                    incomplete.Add(entry.Key);
            }

            return incomplete;
        }

        private static string PluralPairOf(string key)
        {
            if (key.EndsWith(".one", StringComparison.Ordinal))
                return key.Substring(0, key.Length - ".one".Length) + ".other";

            if (key.EndsWith(".other", StringComparison.Ordinal))
                return key.Substring(0, key.Length - ".other".Length) + ".one";

            return null;
        }

        /// <summary>
        /// using 블록 동안 언어를 고정합니다. EditorPrefs에는 쓰지 않습니다(테스트용).
        /// </summary>
        public static IDisposable Override(Language language)
        {
            return new OverrideScope(language);
        }

        private static string KeyOf(string value)
        {
            if (string.IsNullOrEmpty(value) || value.StartsWith("@", StringComparison.Ordinal) == false)
                return null;

            return value.Substring(1);
        }

        private sealed class OverrideScope : IDisposable
        {
            private readonly Language? _previous;

            public OverrideScope(Language language)
            {
                _previous = _override;
                _override = language;
            }

            public void Dispose()
            {
                _override = _previous;
            }
        }
    }
}
