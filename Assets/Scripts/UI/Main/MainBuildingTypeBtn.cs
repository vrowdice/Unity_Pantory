using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MainBuildingTypeBtn : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _text = null;
    [SerializeField] private Image _focusedImage = null;

    private MainCanvas _mainCanvas = null;
    private BuildingType _buildingType = BuildingType.Distribution;

    public BuildingType BuildingType => _buildingType;

    public void Init(MainCanvas host, BuildingType buildingType)
    {
        _mainCanvas = host;
        _buildingType = buildingType;
        if (_text != null) _text.text = buildingType.Localize(LocalizationUtils.TABLE_BUILDING);
    }

    public void OnClick()
    {
        _mainCanvas?.SelectBuildingType(_buildingType);
    }

    public void SetFocused(bool isFocused)
    {
        if (_focusedImage != null) _focusedImage.gameObject.SetActive(isFocused);
    }
}

