using NKStudio.TabularEditor.Data;
using NKStudio.TabularEditor.Window;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace NKStudio.TabularEditor.Import
{
    /// <summary>
    /// 프로젝트 창에서 CSV 또는 TSV 파일을 더블클릭했을 때 테이블 에디터를 엽니다.
    /// </summary>
    public static class TableAssetOpenHandler
    {
        [OnOpenAsset(0)]
        private static bool OnOpenAsset(EntityId instanceId, int line)
        {
            string assetPath = AssetDatabase.GetAssetPath(instanceId);

            if (!TableFormatUtility.TryGetFormat(assetPath, out _))
                return false;

            TableEditorWindow.Open(assetPath);
            return true;
        }
    }
}
