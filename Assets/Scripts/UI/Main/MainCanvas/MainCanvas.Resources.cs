using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class MainCanvas
{
    [Header("Resouce ScrollView")]
    [SerializeField] private GameObject _mainScrollViewResouceBtn;
    [SerializeField] private Transform _resouceScrollViewContent;

    private readonly List<MainScrollViewResouceBtn> _resourceBtns = new List<MainScrollViewResouceBtn>();

    private void RefreshResourceScrollView()
    {
        ScrollRect scroll = _resouceScrollViewContent.GetComponentInParent<ScrollRect>();
        scroll.enabled = false;

        _resourceBtns.Clear();

        PoolingManager pool = GameManager.Instance.PoolingManager;
        pool.ClearChildrenToPool(_resouceScrollViewContent);

        Dictionary<string, ResourceEntry> resources = DataManager.Resource.GetAllResources();
        foreach (ResourceEntry entry in resources.Values)
        {
            if (entry.state.count == 0 && entry.state.currnetChangeCount == 0)
                continue;

            GameObject btnObj = pool.GetPooledObject(_mainScrollViewResouceBtn);
            btnObj.transform.SetParent(_resouceScrollViewContent, false);
            MainScrollViewResouceBtn resourceBtn = btnObj.GetComponent<MainScrollViewResouceBtn>();
            resourceBtn.Init(this, entry);
            _resourceBtns.Add(resourceBtn);
        }

        scroll.enabled = true;
    }
}
