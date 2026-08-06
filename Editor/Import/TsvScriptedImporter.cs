using System.IO;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace NKStudio.TabularEditor.Import
{
    /// <summary>
    /// Unity가 기본 지원하지 않는 .tsv 파일을 TextAsset으로 임포트합니다.
    /// </summary>
    [ScriptedImporter(1, "tsv")]
    public sealed class TsvScriptedImporter : ScriptedImporter
    {
        /// <summary>
        /// TSV 파일을 TextAsset으로 변환해 임포트 결과에 등록합니다.
        /// </summary>
        /// <param name="context">임포트 컨텍스트입니다.</param>
        public override void OnImportAsset(AssetImportContext context)
        {
            string text = File.ReadAllText(context.assetPath);

            TextAsset textAsset = new(text);
            textAsset.name = Path.GetFileNameWithoutExtension(context.assetPath);

            context.AddObjectToAsset("TSV", textAsset);
            context.SetMainObject(textAsset);
        }
    }
}
