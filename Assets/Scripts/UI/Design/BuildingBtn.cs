using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BuildingBtn : MonoBehaviour
{
    [SerializeField] private Image _image = null;
    [SerializeField] private Image _focusedImage = null;
    [SerializeField] private Image _deactivatedImage = null;
    [SerializeField] private TextMeshProUGUI _text = null;

    private DesignCanvas _designUiManager = null;
    private BuildingData _buildingData;
    private bool _isSelected = false;
    private bool _isUnlocked = false;

    public BuildingData BuildingData => _buildingData;

    public void Initialize(DesignCanvas argDesignUiManager, BuildingData buildingData, bool isUnlocked)
    {
        _designUiManager = argDesignUiManager;
        _text.text = buildingData.displayName;
        _image.sprite = buildingData.icon;
        _buildingData = buildingData;
        _isUnlocked = isUnlocked;
        _deactivatedImage.gameObject.SetActive(!isUnlocked);
    }

    public void OnClick()
    {
        _designUiManager.SelectBuilding(_buildingData, _isSelected, _isUnlocked);
    }

    public void SetSelected(bool isFocused)
    {
        _isSelected = isFocused;
        _focusedImage.gameObject.SetActive(isFocused);
    }
}
