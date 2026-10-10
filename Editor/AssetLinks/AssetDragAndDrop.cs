using System.Collections.Generic;
using UnityEditor;

namespace NKStudio.TabularEditor.AssetLinks
{
    /// <summary>
    /// Project 창에서 끌어 오는 에셋을 다룹니다. UnityEditor.DragAndDrop을 그리드 코드에서 떼어 둔다.
    /// </summary>
    public static class AssetDragAndDrop
    {
        /// <summary>
        /// 끌고 있는 에셋의 경로(Assets/…)를 끈 순서대로 돌려줍니다. 폴더·씬 오브젝트는 뺀다.
        /// 스프라이트처럼 하위 에셋을 끌어도 그 파일 경로가 된다.
        /// </summary>
        public static List<string> GetDraggedAssetPaths()
        {
            List<string> paths = new();

            foreach (UnityEngine.Object dragged in DragAndDrop.objectReferences)
                Add(paths, AssetDatabase.GetAssetPath(dragged));

            if (paths.Count == 0)
            {
                foreach (string path in DragAndDrop.paths)
                    Add(paths, path);
            }

            return paths;
        }

        public static void SetAccepted(bool isAccepted)
        {
            DragAndDrop.visualMode = isAccepted ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;
        }

        public static void Accept()
        {
            DragAndDrop.AcceptDrag();
        }

        private static void Add(List<string> paths, string path)
        {
            if (string.IsNullOrEmpty(path) || AssetDatabase.IsValidFolder(path) || paths.Contains(path))
                return;

            paths.Add(path);
        }
    }
}
