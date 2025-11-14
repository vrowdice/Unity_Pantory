using UnityEngine;

public partial class DesignUiManager
{
    public void SelectBuildingType(BuildingType buildingType)
    {
        if (_dataManager == null)
        {
            return;
        }

        _buildingDataList = _dataManager.GetBuildingDataList(buildingType);

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
            }
            else
            {
                Debug.LogError("[DesignUiManager] BuildingBtn component not found on prefab.");
                Destroy(btn);
            }
        }
    }
}
