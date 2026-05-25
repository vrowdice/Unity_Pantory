using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MainBuildingTypeBtn : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _text = null;
    [SerializeField] private Image _focusedImage = null;

    private IBuildingTypeSelectHost _host = null;
    private BuildingType _buildingType = BuildingType.Distribution;

    public BuildingType BuildingType => _buildingType;

    public void Init(IBuildingTypeSelectHost host, BuildingType buildingType)
    {
        _host = host;
        _buildingType = buildingType;
        if (_text != null) _text.text = buildingType.Localize(LocalizationUtils.TABLE_BUILDING);
    }

    public void Init(MainCanvas host, BuildingType buildingType)
    {
        Init((IBuildingTypeSelectHost)host, buildingType);
    }

    public void OnClick()
    {
        _host?.SelectBuildingType(_buildingType);
    }

    public void SetFocused(bool isFocused)
    {
        if (_focusedImage != null) _focusedImage.gameObject.SetActive(isFocused);
    }
}

