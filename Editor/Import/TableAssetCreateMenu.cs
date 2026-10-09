using UnityEditor;

namespace NKStudio.TabularEditor.Import
{
    /// <summary>
    /// 프로젝트 창의 Create 메뉴에서 새 CSV 파일을 만듭니다.
    /// </summary>
    public static class TableAssetCreateMenu
    {
        private const string TemplatePath = "Packages/com.nkstudio.unitytable/Editor/Templates/NewTable.csv.txt";
        private const string DefaultFileName = "NewTable.csv";

        // Unity가 C# 스크립트를 만들 때와 같은 경로라, 파일이 생기자마자 이름을 입력받는 인라인 편집을 그대로 쓴다.
        // 메뉴 경로는 내부 이름(Scripting)으로 지정해야 한국어 에디터의 "스크립팅" 하위 메뉴에 합쳐진다.
        [MenuItem("Assets/Create/Scripting/CSV File")]
        private static void CreateCsvFile()
        {
            // git URL로 설치하면 패키지가 Library/PackageCache에 있어, 템플릿은 실제 디스크 경로로 넘겨야 한다.
            ProjectWindowUtil.CreateScriptAssetFromTemplateFile(
                FileUtil.GetPhysicalPath(TemplatePath),
                DefaultFileName);
        }
    }
}
