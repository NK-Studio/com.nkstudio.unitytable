using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace NKStudio.TabularEditor.AssetLinks
{
    /// <summary>
    /// 에셋 경로 셀에 마우스를 올려 두면 뜨는 미리보기 카드입니다. 큰 미리보기·이름·종류(텍스처면 크기)·실제 경로를 보여 준다.
    /// 마우스 입력을 받지 않으므로 카드가 셀을 가려도 호버·클릭은 셀로 간다.
    /// <para>
    /// 텍스처(스프라이트 포함)만 실제로 불러와 그림을 보여 준다. 프리팹 등은 불러오지 않고 Project 창 아이콘·종류·경로만 보인다.
    /// 프리팹은 의존 에셋까지 함께 불러와, 처음 마우스를 올릴 때 에디터가 0.5초 넘게 멈췄다(2026-10-10 실측 668ms).
    /// 종류는 불러오지 않고 알 수 있다(<see cref="AssetDatabase.GetMainAssetTypeAtPath"/>).
    /// </para>
    /// </summary>
    public sealed class TableAssetPreviewCard : IDisposable
    {
        private const string CardClassName = "table-editor__asset-card";
        private const string ImageClassName = "table-editor__asset-card-image";
        private const string NameClassName = "table-editor__asset-card-name";
        private const string InfoClassName = "table-editor__asset-card-info";

        // 카드와 셀 사이, 카드와 표 가장자리 사이의 여백(px)이다.
        private const float Gap = 4f;

        // 큰 미리보기는 Unity가 비동기로 만들 수 있다. 이 간격으로 몇 번 다시 물어보고, 그래도 없으면 아이콘으로 둔다.
        private const long PreviewPollIntervalMs = 100;
        private const int MaxPreviewPolls = 30;

        private readonly VisualElement _container;
        private readonly VisualElement _card;
        private readonly Image _image;
        private readonly Label _name;
        private readonly Label _info;
        private readonly Label _path;

        private IVisualElementScheduledItem _previewPoll;
        private UnityEngine.Object _asset;
        private int _pollCount;

        public TableAssetPreviewCard(VisualElement container)
        {
            _container = container ?? throw new ArgumentNullException(nameof(container));

            _card = new VisualElement { pickingMode = PickingMode.Ignore };
            _card.AddToClassList(CardClassName);

            _image = new Image { scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
            _image.AddToClassList(ImageClassName);
            _card.Add(_image);

            _name = new Label { pickingMode = PickingMode.Ignore };
            _name.AddToClassList(NameClassName);
            _card.Add(_name);

            _info = new Label { pickingMode = PickingMode.Ignore };
            _info.AddToClassList(InfoClassName);
            _card.Add(_info);

            _path = new Label { pickingMode = PickingMode.Ignore };
            _path.AddToClassList(InfoClassName);
            _card.Add(_path);

            _card.style.display = DisplayStyle.None;
            _container.Add(_card);
        }

        public bool IsVisible => _card.style.display == DisplayStyle.Flex;

        /// <summary>
        /// cellWorldBound 셀 아래(자리가 없으면 위)에 카드를 띄웁니다.
        /// </summary>
        public void Show(string assetPath, Rect cellWorldBound)
        {
            Type assetType = AssetDatabase.GetMainAssetTypeAtPath(assetPath);

            if (assetType == null)
            {
                Hide();
                return;
            }

            _asset = typeof(Texture).IsAssignableFrom(assetType) ? AssetPathIndex.Load(assetPath) : null;
            _path.text = assetPath;

            if (_asset != null)
            {
                _name.text = _asset.name;
                _info.text = DescribeAsset(_asset);
                _image.image = AssetPreview.GetAssetPreview(_asset) ?? AssetPreview.GetMiniThumbnail(_asset);
            }
            else
            {
                _name.text = System.IO.Path.GetFileNameWithoutExtension(assetPath);
                _info.text = assetType.Name;
                _image.image = AssetPathIndex.GetIcon(assetPath);
            }

            _card.style.display = DisplayStyle.Flex;
            _card.BringToFront();
            Place(cellWorldBound);

            // 카드 크기는 레이아웃이 끝나야 알 수 있어, 아래로 넘치면 다음 프레임에 셀 위로 옮긴다.
            _card.schedule.Execute(() => Place(cellWorldBound));

            _pollCount = 0;
            _previewPoll?.Pause();

            if (_asset != null)
                _previewPoll = _card.schedule.Execute(PollPreview).Every(PreviewPollIntervalMs);
        }

        public void Hide()
        {
            _previewPoll?.Pause();
            _asset = null;
            _card.style.display = DisplayStyle.None;
        }

        public void Dispose()
        {
            Hide();
            _card.RemoveFromHierarchy();
        }

        private void PollPreview()
        {
            if (_asset == null || _pollCount++ >= MaxPreviewPolls)
            {
                _previewPoll?.Pause();
                return;
            }

            Texture2D preview = AssetPreview.GetAssetPreview(_asset);

            if (preview == null)
                return;

            _image.image = preview;
            _previewPoll?.Pause();
        }

        private void Place(Rect cellWorldBound)
        {
            Rect cell = new(_container.WorldToLocal(cellWorldBound.position), cellWorldBound.size);
            float containerWidth = _container.resolvedStyle.width;
            float containerHeight = _container.resolvedStyle.height;
            float cardWidth = _card.resolvedStyle.width;
            float cardHeight = _card.resolvedStyle.height;

            float left = cell.xMin;
            float top = cell.yMax + Gap;

            if (float.IsNaN(cardWidth) == false && float.IsNaN(containerWidth) == false)
                left = Mathf.Clamp(left, Gap, Mathf.Max(Gap, containerWidth - cardWidth - Gap));

            if (float.IsNaN(cardHeight) == false && float.IsNaN(containerHeight) == false && top + cardHeight > containerHeight - Gap)
                top = Mathf.Max(Gap, cell.yMin - cardHeight - Gap);

            _card.style.left = left;
            _card.style.top = top;
        }

        // 텍스처는 종류 이름에 픽셀 크기를 붙인다.
        private static string DescribeAsset(UnityEngine.Object asset)
        {
            string type = asset.GetType().Name;

            return asset is Texture texture ? $"{type} · {texture.width}×{texture.height}" : type;
        }
    }
}
