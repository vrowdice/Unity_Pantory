using System.Collections.Generic;
using UnityEngine;

public partial class MainUiManager
{
    private void RefreshResourceScrollView()
    {
        if (_dataManager == null || _resouceScrollViewContent == null)
        {
            return;
        }

        GameObjectUtils.ClearChildren(_resouceScrollViewContent);
        _resourceBtns.Clear();

        if (_mainScrollViewResouceBtn == null)
        {
            Debug.LogWarning("[MainUiManager] MainScrollViewResouceBtn prefab is not assigned.");
            return;
        }

        Dictionary<string, ResourceEntry> resources = _dataManager.Resource.GetAllResources();
        if (resources == null || resources.Count == 0)
        {
            return;
        }

        foreach (var entry in resources.Values)
        {
            if (entry == null)
            {
                continue;
            }

            if(entry.resourceState.deltaCount == 0 && entry.resourceState.count == 0)
            {
                continue;
            }

            GameObject btnObj = Instantiate(_mainScrollViewResouceBtn, _resouceScrollViewContent);
            MainScrollViewResouceBtn resourceBtn = btnObj.GetComponent<MainScrollViewResouceBtn>();
            if (resourceBtn != null)
            {
                resourceBtn.OnInitialize(this, entry);
                _resourceBtns.Add(resourceBtn);
            }
        }
    }

    private void UpdateResourceSummary()
    {
        RefreshResourceScrollView();
    }
}

