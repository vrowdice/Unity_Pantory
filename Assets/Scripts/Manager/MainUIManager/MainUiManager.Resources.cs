using System.Collections.Generic;
using UnityEngine;

public partial class MainUiManager
{
    [Header("Resouce ScrollView")]
    [SerializeField] private GameObject _mainScrollViewResouceBtn;
    [SerializeField] private Transform _resouceScrollViewContent;

    private readonly List<MainScrollViewResouceBtn> _resourceBtns = new List<MainScrollViewResouceBtn>();

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

        Dictionary<string, ResourceEntry> resources = _dataManager.GetAllResources();
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

