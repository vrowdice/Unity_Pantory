using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public partial class DesignCanvas : CanvasBase
{
    [SerializeField] private ThreadSaveInfoPanel _saveInfoPanel;
    [SerializeField] private DesignRunner _designRunner;

    public DesignRunner DesignRunner => _designRunner;
    public BuildingData SelectedBuilding => _selectedBuilding;
    public bool IsRemovalMode => _isRemovalMode;

    public void Init(DesignRunner designRunner)
    {
        base.Init();
        _designRunner = designRunner;

        foreach (var buildingType in EnumUtils.GetAllEnumValues<BuildingType>())
        {
            var btn = Instantiate(_buildingTypeBtnPrefab, _buildingTypeBtnContent);
            var buildingTypeBtn = btn.GetComponent<BuildingTypeBtn>();
            if (buildingTypeBtn != null)
            {
                buildingTypeBtn.Initialize(this, buildingType);
                _buildingTypeBtns.Add(buildingTypeBtn);
            }
        }

        _saveInfoPanel.Init(DataManager);
        InitializeThreadTitle();

        SelectBuildingType(BuildingType.Distribution);
    }

    override public void UpdateAllMainText()
    {
        
    }

    public void UpdateModeBtnImages(bool isPlacementMode, bool isRemovalMode)
    {
        _deselectBuildingBtnImage.color = isPlacementMode ? VisualManager.Instance.ValidColor : Color.white;
        _removalModeBtnImage.color = isRemovalMode ? VisualManager.Instance.InvalidColor : Color.white;
    }
}
