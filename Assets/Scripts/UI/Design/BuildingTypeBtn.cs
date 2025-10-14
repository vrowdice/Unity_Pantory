using UnityEngine;
using TMPro;

public class BuildingTypeBtn : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI _text = null;
    private DesignUiManager _designUiManager = null;
    private BuildingType _buildingType = BuildingType.Load;
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
}
