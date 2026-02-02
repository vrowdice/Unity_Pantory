using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BuildingTypeBtn : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _text = null;
    [SerializeField] private Image _focusedImage = null;
    private DesignCanvas _designUiManager = null;
    private BuildingType _buildingType = BuildingType.Distribution;

    public BuildingType BuildingType => _buildingType;

    public void Initialize(DesignCanvas argDesignUiManager, BuildingType buildingType)
    {
        _designUiManager = argDesignUiManager;
        _buildingType = buildingType;
        _text.text = buildingType.Localize(LocalizationUtils.TABLE_BUILDING_TYPE);
    }

    public void OnClick()
    {
        _designUiManager.SelectBuildingType(_buildingType);
    }

    public void SetFocused(bool isFocused)
    {
        if (_focusedImage != null)
        {
            _focusedImage.gameObject.SetActive(isFocused);
        }
    }
}
