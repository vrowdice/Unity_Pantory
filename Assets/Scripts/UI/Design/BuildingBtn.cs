using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BuildingBtn : MonoBehaviour
{
    [SerializeField] private Image _image = null;
    [SerializeField] private TextMeshProUGUI _text = null;
    private DesignUiManager _designUiManager = null;
    private BuildingEntry _buildingEntry;

    public void Initialize(DesignUiManager argDesignUiManager, BuildingEntry buildingEntry)
    {
        _designUiManager = argDesignUiManager;
        _text.text = buildingEntry.buildingData.displayName;
        _image.sprite = buildingEntry.buildingData.icon;
        _buildingEntry = buildingEntry;
    }

    public void OnClick()
    {
        _designUiManager.SelectBuilding(_buildingEntry);
    }
}
