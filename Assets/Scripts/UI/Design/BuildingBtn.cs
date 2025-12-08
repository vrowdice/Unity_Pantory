using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BuildingBtn : MonoBehaviour
{
    [SerializeField] private Image _image = null;
    [SerializeField] private Image _focusedImage = null;
    [SerializeField] private TextMeshProUGUI _text = null;
    private DesignUiManager _designUiManager = null;
    private BuildingData _buildingData;

    public void Initialize(DesignUiManager argDesignUiManager, BuildingData buildingData)
    {
        _designUiManager = argDesignUiManager;
        _text.text = buildingData.displayName;
        _image.sprite = buildingData.icon;
        _buildingData = buildingData;
    }

    public void OnClick()
    {
        _designUiManager.SelectBuilding(_buildingData);
    }

    public void SetFocused(bool isFocused)
    {
        if (_focusedImage != null)
        {
            _focusedImage.gameObject.SetActive(isFocused);
        }
    }

    public BuildingData GetBuildingData()
    {
        return _buildingData;
    }
}
