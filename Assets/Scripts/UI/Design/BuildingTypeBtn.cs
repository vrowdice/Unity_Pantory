using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BuildingTypeBtn : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _text = null;
    [SerializeField] private Image _focusedImage = null;
    private DesignUiManager _designUiManager = null;
    private BuildingType _buildingType = BuildingType.Distribution;

    public BuildingType BuildingType => _buildingType;

    public void Initialize(DesignUiManager argDesignUiManager, BuildingType buildingType)
    {
        _designUiManager = argDesignUiManager;
        _buildingType = buildingType;
        _text.text = buildingType.ToString();
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
