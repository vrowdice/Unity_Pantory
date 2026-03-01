using System.Collections.Generic;
using UnityEngine;

public partial class MainCanvas
{
    [Header("Resouce ScrollView")]
    [SerializeField] private GameObject _mainScrollViewResouceBtn;
    [SerializeField] private Transform _resouceScrollViewContent;

    private readonly List<MainScrollViewResouceBtn> _resourceBtns = new List<MainScrollViewResouceBtn>();

    private void RefreshResourceScrollView()
    {
        GameObjectUtils.ClearChildren(_resouceScrollViewContent);
        _resourceBtns.Clear();

        if (_mainScrollViewResouceBtn == null)
        {
            Debug.LogWarning("[MainUiManager] MainScrollViewResouceBtn prefab is not assigned.");
            return;
        }

        Dictionary<string, ResourceEntry> resources = DataManager.Resource.GetAllResources();
        foreach (ResourceEntry entry in resources.Values)
        {
            if(entry.state.count == 0 && entry.state.currnetChangeCount == 0)
            {
                continue;
            }

            GameObject btnObj = Instantiate(_mainScrollViewResouceBtn, _resouceScrollViewContent);
            MainScrollViewResouceBtn resourceBtn = btnObj.GetComponent<MainScrollViewResouceBtn>();
            if (resourceBtn != null)
            {
                resourceBtn.Init(this, entry);
                _resourceBtns.Add(resourceBtn);
            }
        }
    }
}
