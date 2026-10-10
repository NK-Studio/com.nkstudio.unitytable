namespace NKStudio.TabularEditor.Window
{
    /// <summary>
    /// 에디터 밖에서 파일이 바뀌었을 때 무엇을 할지입니다.
    /// </summary>
    public enum ExternalChangeAction
    {
        /// <summary>내용이 그대로라 할 일이 없다(우리가 저장한 직후 등).</summary>
        Ignore,

        /// <summary>잃을 편집이 없으니 화면을 유지한 채 바로 다시 읽는다.</summary>
        Reload,

        /// <summary>저장 안 한 편집이나 입력 중인 글자가 있어, 알림 바로 사용자에게 맡긴다.</summary>
        Notify,
    }

    /// <summary>
    /// 외부 변경을 감지했을 때의 처리 규칙입니다.
    /// </summary>
    public static class ExternalChangeDecision
    {
        /// <param name="isContentChanged">파일 해시가 마지막으로 읽거나 저장한 때와 다른지 여부입니다.</param>
        /// <param name="hasUnsavedChanges">저장하지 않은 편집이 있는지 여부입니다.</param>
        /// <param name="isEditing">셀에 글자를 입력하는 중인지 여부입니다.</param>
        public static ExternalChangeAction Decide(bool isContentChanged, bool hasUnsavedChanges, bool isEditing)
        {
            if (isContentChanged == false)
                return ExternalChangeAction.Ignore;

            if (hasUnsavedChanges || isEditing)
                return ExternalChangeAction.Notify;

            return ExternalChangeAction.Reload;
        }
    }
}
