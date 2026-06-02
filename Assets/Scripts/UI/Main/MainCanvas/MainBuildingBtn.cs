using UnityEngine;
using UnityEngine.UI;
using Evo.UI;
using TMPro;

public class MainBuildingBtn : BtnBase
{
    [SerializeField] private Image _image = null;
    [SerializeField] private Image _focusedImage = null;
    [SerializeField] private Image _deactivatedImage = null;
    [SerializeField] private Evo.UI.Button _buildingHelpButton = null;
    [SerializeField] private TextMeshProUGUI _text = null;
    [SerializeField] private TextMeshProUGUI _placedCountText = null;

    private MainCanvas _host = null;
    private BuildingData _buildingData;
    private bool _isSelected = false;

    public BuildingData BuildingData => _buildingData;

    public void Init(MainCanvas host, BuildingData buildingData, MainRunner runner)
    {
        _host = host;
        _buildingData = buildingData;

        if (_text != null) _text.text = buildingData.id.Localize(LocalizationUtils.TABLE_BUILDING);
        if (_image != null) _image.sprite = buildingData.icon;
        if (_deactivatedImage != null) _deactivatedImage.gameObject.SetActive(false);

        BindClick(_buildingHelpButton, OnClickBuildingHelp);

        RefreshPlacedCount(runner);
        EnsureClickBound();
    }

    public void RefreshPlacedCount(MainRunner runner)
    {
        if (_placedCountText == null || _buildingData == null)
        {
            if (_placedCountText != null)
                _placedCountText.gameObject.SetActive(false);
            return;
        }

        if (!_buildingData.usePlacedCountLimit)
        {
            _placedCountText.gameObject.SetActive(false);
            return;
        }

        int current = runner.GridHandler.CountPlacedBuildingsWithId(_buildingData.id);
        int max = DataManager.Instance.Building.GetMaxPlacedCount(_buildingData);
        _placedCountText.text = $"{current}/{max}";
        _placedCountText.gameObject.SetActive(true);
    }

    protected override void HandleClick()
    {
        _host?.SelectBuilding(_buildingData, _isSelected);
    }

    private void OnClickBuildingHelp()
    {
        if (_buildingData == null) return;
        UIManager.Instance?.ShowBuildingHelpPopup(_buildingData);
    }

    public void SetSelected(bool isFocused)
    {
        _isSelected = isFocused;
        if (_focusedImage != null) _focusedImage.gameObject.SetActive(isFocused);
    }
}
