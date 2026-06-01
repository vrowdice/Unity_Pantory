using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainBlueprintSavedBtn : BtnBase
{
    [SerializeField] private Image _focusedImage = null;
    [SerializeField] private Image _thumbnail = null;
    [SerializeField] private Sprite _fallbackThumbnail = null;
    [SerializeField] private TextMeshProUGUI _label;

    private MainCanvas _mainCanvas;
    private string _blueprintName;
    private List<PlacedBuildingSaveData> _buildings;
    private List<PlacedRoadSaveData> _roads;
    private string _layoutKey;

    public string LayoutKey => _layoutKey;
    public IReadOnlyList<PlacedBuildingSaveData> Buildings => _buildings;

    public void Init(MainCanvas mainCanvas, string layoutKey, string blueprintName, List<PlacedBuildingSaveData> buildings, List<PlacedRoadSaveData> roads, bool isSelected)
    {
        _mainCanvas = mainCanvas;
        _layoutKey = layoutKey;
        _blueprintName = blueprintName;
        _buildings = buildings;
        _roads = roads;
        if (_label != null)
            _label.text = string.IsNullOrEmpty(_blueprintName) ? "Blueprint" : _blueprintName;
        RefreshThumbnail();
        SetSelected(isSelected);
    }

    protected override void HandleClick()
    {
        if (_mainCanvas != null)
            _mainCanvas.ToggleSavedBlueprintPlacement(_layoutKey, _blueprintName, _buildings, _roads);
    }

    public void SetSelected(bool isSelected)
    {
        if (_focusedImage != null)
            _focusedImage.gameObject.SetActive(isSelected);
    }

    private void RefreshThumbnail()
    {
        if (_thumbnail == null)
            return;

        Sprite sprite = BlueprintThumbnailUtils.ResolveDominantSprite(_buildings, _roads);
        if (sprite == null)
            sprite = _fallbackThumbnail;

        _thumbnail.sprite = sprite;
        _thumbnail.gameObject.SetActive(sprite != null);
        if (sprite != null)
            _thumbnail.preserveAspect = true;
    }
}
