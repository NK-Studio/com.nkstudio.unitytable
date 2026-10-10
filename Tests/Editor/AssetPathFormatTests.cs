using NKStudio.TabularEditor.AssetLinks;
using NUnit.Framework;

namespace NKStudio.TabularEditor.Tests
{
    /// <summary>
    /// 끌어다 놓은 에셋을 열 형식에 맞는 셀 값으로 바꾸는 규칙을 검증합니다.
    /// </summary>
    public sealed class AssetPathFormatTests
    {
        [Test]
        public void Test_Detect_ResourcesColumn()
        {
            Assert.AreEqual(AssetPathStyle.Resources, AssetPathFormat.Detect(new[] { "IconPath", "Art/Sprite/a", "Art/Sprite/b" }));
        }

        [Test]
        public void Test_Detect_ProjectColumn()
        {
            Assert.AreEqual(AssetPathStyle.Project, AssetPathFormat.Detect(new[] { "Assets/Art/a.png", "Assets/Art/b.png", "Art/c" }));
        }

        [Test]
        public void Test_Detect_NoPathsIsUndecided()
        {
            Assert.IsNull(AssetPathFormat.Detect(new[] { "", "None", "Cat" }));
        }

        [Test]
        public void Test_ToCellValue_ResourcesStripsFolderAndExtension()
        {
            Assert.AreEqual(
                "Art/Sprite/Buff/spr_buff_1000",
                AssetPathFormat.ToCellValue("Assets/Resources/Art/Sprite/Buff/spr_buff_1000.png", AssetPathStyle.Resources));
        }

        [Test]
        public void Test_ToCellValue_NestedResourcesUsesLastResourcesFolder()
        {
            Assert.AreEqual(
                "Prefab/UI/Button",
                AssetPathFormat.ToCellValue("Assets/Resources/NanaliUI/Resources/Prefab/UI/Button.prefab", AssetPathStyle.Resources));
        }

        [Test]
        public void Test_ToCellValue_OutsideResourcesIsNullForResourcesStyle()
        {
            Assert.IsNull(AssetPathFormat.ToCellValue("Assets/Art/spr.png", AssetPathStyle.Resources));
        }

        [Test]
        public void Test_ToCellValue_ProjectStyleKeepsPath()
        {
            Assert.AreEqual("Assets/Art/spr.png", AssetPathFormat.ToCellValue("Assets/Art/spr.png", AssetPathStyle.Project));
        }
    }
}
