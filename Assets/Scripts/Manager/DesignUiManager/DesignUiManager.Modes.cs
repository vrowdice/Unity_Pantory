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
        Debug.Log($"[DesignUiManager] Building selected: {buildingData.displayName}");
    }

    public void DeselectBuilding()
    {
        _selectedBuilding = null;
        _isRemovalMode = false;

        _buildingTileManager?.CancelPlacementMode();

        UpdateModeBtnImages(false, false);
        UpdateBuildingButtonStates();
        Debug.Log("[DesignUiManager] Building deselected");
    }

    public void StartRemovalMode()
    {
        _isRemovalMode = true;
        _selectedBuilding = null;

        if (_buildingTileManager != null)
        {
            if (_buildingTileManager.IsPlacementMode)
            {
                _buildingTileManager.CancelPlacementMode();
            }

            _buildingTileManager.StartRemovalMode();
        }

        UpdateModeBtnImages(false, true);
        Debug.Log("[DesignUiManager] Removal mode started");
    }

    public void CancelRemovalMode()
    {
        _isRemovalMode = false;

        _buildingTileManager?.CancelRemovalMode();

        UpdateModeBtnImages(false, false);
        Debug.Log("[DesignUiManager] Removal mode cancelled");
    }

    public void RemovalModeToggle()
    {
        if (_isRemovalMode)
        {
            CancelRemovalMode();
            Debug.Log("[DesignUiManager] Removal mode toggled OFF");
        }
        else
        {
            StartRemovalMode();
            Debug.Log("[DesignUiManager] Removal mode toggled ON");
        }
    }
}
