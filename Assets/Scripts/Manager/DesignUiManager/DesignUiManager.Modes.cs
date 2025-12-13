using UnityEngine;
using UnityEngine.UI;

public partial class DesignUiManager
{
    [Header("Mode References")]
    [SerializeField] private Image _deselectBuildingBtnImage;
    [SerializeField] private Image _removalModeBtnImage;

    private BuildingData _selectedBuilding;
    private bool _isRemovalMode;

    public void SelectBuilding(BuildingData buildingData)
    {
        _selectedBuilding = buildingData;
        _isRemovalMode = false;

        if (_buildingTileManager != null)
        {
            if (_buildingTileManager.IsRemovalMode)
            {
                _buildingTileManager.CancelRemovalMode();
            }

            _buildingTileManager.StartPlacementMode(buildingData);
        }

        UpdateModeBtnImages(true, false);
        UpdateBuildingButtonStates();
    }

    public void DeselectBuilding()
    {
        _selectedBuilding = null;
        _isRemovalMode = false;

        _buildingTileManager.CancelPlacementMode();
        UpdateModeBtnImages(false, false);
        UpdateBuildingButtonStates();
    }

    public void StartRemovalMode()
    {
        _isRemovalMode = true;
        _selectedBuilding = null;

        _buildingTileManager.CancelPlacementMode();
        _buildingTileManager.StartRemovalMode();

        UpdateModeBtnImages(false, true);
        UpdateBuildingButtonStates();
    }

    public void CancelRemovalMode()
    {
        _isRemovalMode = false;
        _buildingTileManager.CancelRemovalMode();
        UpdateModeBtnImages(false, false);
        UpdateBuildingButtonStates();
    }

    public void RemovalModeToggle()
    {
        if (_isRemovalMode)
        {
            CancelRemovalMode();
        }
        else
        {
            StartRemovalMode();
        }
    }
}
