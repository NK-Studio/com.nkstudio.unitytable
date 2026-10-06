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
#if UNITY_6000_3_OR_NEWER
        [OnOpenAsset(0)]
        private static bool OnOpenAsset(EntityId entityId, int line)
        {
            return TryOpen(AssetDatabase.GetAssetPath(entityId));
        }
#else
        // Unity 6.2 이하에는 EntityId가 없으므로 기존 instanceID(int) 기반 시그니처를 사용합니다.
        [OnOpenAsset(0)]
        private static bool OnOpenAsset(int instanceId, int line)
        {
            return TryOpen(AssetDatabase.GetAssetPath(instanceId));
        }
#endif

        private static bool TryOpen(string assetPath)
        {
            if (!TableFormatUtility.TryGetFormat(assetPath, out _))
                return false;

            TableEditorWindow.Open(assetPath);
            return true;
        }
    }
}
