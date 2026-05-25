using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class TutorialCanvas
{
    [Header("Resouce ScrollView")]
    [SerializeField] private GameObject _resourceScrollView;
    [SerializeField] private GameObject _mainScrollViewResouceBtn;
    [SerializeField] private Transform _resouceScrollViewContent;

    private readonly List<MainScrollViewResouceBtn> _resourceBtns = new List<MainScrollViewResouceBtn>();

    private void RefreshResourceScrollView()
    {
        if (_resouceScrollViewContent == null)
            return;

        ScrollRect scroll = _resouceScrollViewContent.GetComponentInParent<ScrollRect>();
        if (scroll != null)
            scroll.enabled = false;

        _resourceBtns.Clear();

        PoolingManager pool = GameManager.PoolingManager;
        pool.ClearChildrenToPool(_resouceScrollViewContent);

        Dictionary<string, ResourceEntry> resources = DataManager.Resource.GetAllResources();
        foreach (ResourceEntry entry in resources.Values)
        {
            if (entry.state.count == 0 && entry.state.currnetChangeCount == 0)
                continue;

            GameObject btnObj = pool.GetPooledObject(_mainScrollViewResouceBtn);
            btnObj.transform.SetParent(_resouceScrollViewContent, false);
            MainScrollViewResouceBtn resourceBtn = btnObj.GetComponent<MainScrollViewResouceBtn>();
            resourceBtn.Init(entry);
            _resourceBtns.Add(resourceBtn);
        }

        if (scroll != null)
            scroll.enabled = true;
    }

    public GameObject FindResourceScrollView()
    {
        if (_resourceScrollView != null)
            return _resourceScrollView;

        if (_resouceScrollViewContent == null)
            return null;

        ScrollRect scroll = _resouceScrollViewContent.GetComponentInParent<ScrollRect>();
        return scroll != null ? scroll.gameObject : _resouceScrollViewContent.gameObject;
    }
}
