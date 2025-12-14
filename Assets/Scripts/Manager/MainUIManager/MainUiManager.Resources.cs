using System.Collections.Generic;
using UnityEngine;

public partial class MainUiManager
{
    [Header("Resouce ScrollView")]
    [SerializeField] private GameObject _mainScrollViewResouceBtn;
    [SerializeField] private Transform _resouceScrollViewContent;

    // Resources
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

            // 플레이어 재고가 있는 자원만 표시
            if(entry.resourceState.playerInventory == 0)
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
}

