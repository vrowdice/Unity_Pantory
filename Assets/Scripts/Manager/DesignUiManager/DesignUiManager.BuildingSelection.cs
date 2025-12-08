using UnityEngine;
using System.Collections.Generic;

public partial class DesignUiManager
{
    public void SelectBuildingType(BuildingType buildingType)
    {
        if (_dataManager == null)
        {
            return;
        }

        _selectedBuildingType = buildingType;
        _buildingDataList = _dataManager.Building.GetBuildingDataList(buildingType);

        _buildingBtns.Clear();
        var existingButtons = _buildingBtnContent.GetComponentsInChildren<BuildingBtn>();
        foreach (var btn in existingButtons)
        {
            Destroy(btn.gameObject);
        }

        foreach (var buildingData in _buildingDataList)
        {
            var btn = Instantiate(_buildingBtnPrefab, _buildingBtnContent);
            var buildingBtn = btn.GetComponent<BuildingBtn>();

            if (buildingBtn != null)
            {
                buildingBtn.Initialize(this, buildingData);
                _buildingBtns.Add(buildingBtn);
            }
            else
            {
                Debug.LogError("[DesignUiManager] BuildingBtn component not found on prefab.");
                Destroy(btn);
            }
        }

        UpdateBuildingTypeButtonStates();
        UpdateBuildingButtonStates();
    }

    private void UpdateBuildingTypeButtonStates()
    {
        foreach (var btn in _buildingTypeBtns)
        {
            if (btn == null)
                continue;

            bool isActive = btn.BuildingType == _selectedBuildingType;
            btn.SetFocused(isActive);
        }
    }

    private void UpdateBuildingButtonStates()
    {
        foreach (var btn in _buildingBtns)
        {
            if (btn == null)
                continue;

            BuildingData btnBuildingData = btn.GetBuildingData();
            bool isActive = (_selectedBuilding != null && btnBuildingData != null && 
                           _selectedBuilding == btnBuildingData);
            btn.SetFocused(isActive);
        }
    }
}
