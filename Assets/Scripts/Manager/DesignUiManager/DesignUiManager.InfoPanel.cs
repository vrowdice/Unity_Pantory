using UnityEngine;

public partial class DesignUiManager
{
    public void ShowBuildingInfo(BuildingData buildingData, BuildingState buildingState)
    {
        if (_buildingInfoPanel != null)
        {
            _buildingInfoPanel.gameObject.SetActive(true);
            _buildingInfoPanel.ShowBuildingInfo(buildingData, buildingState, this);
            Debug.Log($"[DesignUiManager] Showing building info for: {buildingData.displayName}");
        }
        else
        {
            Debug.LogWarning("[DesignUiManager] BuildingInfoPanel is not assigned!");
        }
    }

    public void HideBuildingInfo()
    {
        _buildingInfoPanel?.gameObject.SetActive(false);
    }

    public void RotateBuildingLeft()
    {
        if (_buildingTileManager == null)
        {
            return;
        }

        _buildingTileManager.RotateBuildingLeft();
    }

    public void RotateBuildingRight()
    {
        if (_buildingTileManager == null)
        {
            return;
        }

        _buildingTileManager.RotateBuildingRight();
    }
}
