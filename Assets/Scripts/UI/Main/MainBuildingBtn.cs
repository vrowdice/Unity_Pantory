using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MainBuildingBtn : MonoBehaviour
{
    [SerializeField] private Image _image = null;
    [SerializeField] private Image _focusedImage = null;
    [SerializeField] private Image _deactivatedImage = null;
    [SerializeField] private TextMeshProUGUI _text = null;
    [SerializeField] private TextMeshProUGUI _placedCountText = null;

    private MainCanvas _host = null;
    private BuildingData _buildingData;
    private bool _isSelected = false;
    private bool _isUnlocked = false;

    public BuildingData BuildingData => _buildingData;

    public void Initialize(MainCanvas host, BuildingData buildingData, bool isUnlocked, MainRunner runner)
    {
        _host = host;
        _buildingData = buildingData;
        _isUnlocked = isUnlocked;

        if (_text != null) _text.text = buildingData.id.Localize(LocalizationUtils.TABLE_BUILDING);
        if (_image != null) _image.sprite = buildingData.icon;
        if (_deactivatedImage != null) _deactivatedImage.gameObject.SetActive(!isUnlocked);

        RefreshPlacedCount(runner);
    }

    public void RefreshPlacedCount(MainRunner runner)
    {
        if (_placedCountText == null || _buildingData == null)
            return;

        if (!_buildingData.usePlacedCountLimit)
        {
            _placedCountText.gameObject.SetActive(false);
            return;
        }

        int current = runner.GridHandler.CountPlacedBuildingsWithId(_buildingData.id);
        int max = DataManager.Instance.Building.GetMaxPlacedCount(_buildingData);
        _placedCountText.text = $"{current} / {max}";
        _placedCountText.gameObject.SetActive(true);
    }
    public void OnClick()
    {
        _host?.SelectBuilding(_buildingData, _isSelected, _isUnlocked);
    }

    public void SetSelected(bool isFocused)
    {
        _isSelected = isFocused;
        if (_focusedImage != null) _focusedImage.gameObject.SetActive(isFocused);
    }
}

