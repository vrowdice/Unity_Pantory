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
        if (_mainScrollViewResouceBtn == null)
        {
            Debug.LogWarning("[MainUiManager] MainScrollViewResouceBtn prefab is not assigned.");
            return;
        }

        ScrollRect scroll = _resouceScrollViewContent.GetComponentInParent<ScrollRect>();
        if (scroll != null)
        {
            scroll.enabled = false;
        }

        _resourceBtns.Clear();

        PoolingManager pool = GameManager.Instance != null ? GameManager.Instance.PoolingManager : null;
        if (pool != null)
        {
            pool.ClearChildrenToPool(_resouceScrollViewContent);
        }
        else
        {
            GameObjectUtils.ClearChildren(_resouceScrollViewContent);
        }

        Dictionary<string, ResourceEntry> resources = DataManager.Resource.GetAllResources();
        foreach (ResourceEntry entry in resources.Values)
        {
            if (entry.state.count == 0 && entry.state.currnetChangeCount == 0)
            {
                continue;
            }

            MainScrollViewResouceBtn resourceBtn = null;
            if (pool != null)
            {
                GameObject btnObj = pool.GetPooledObject(_mainScrollViewResouceBtn);
                btnObj.transform.SetParent(_resouceScrollViewContent, false);
                resourceBtn = btnObj.GetComponent<MainScrollViewResouceBtn>();
            }
            else
            {
                GameObject btnObj = Instantiate(_mainScrollViewResouceBtn, _resouceScrollViewContent);
                resourceBtn = btnObj.GetComponent<MainScrollViewResouceBtn>();
            }

            if (resourceBtn != null)
            {
                resourceBtn.Init(this, entry);
                _resourceBtns.Add(resourceBtn);
            }
        }

        if (scroll != null)
        {
            scroll.enabled = true;
        }
    }
}
