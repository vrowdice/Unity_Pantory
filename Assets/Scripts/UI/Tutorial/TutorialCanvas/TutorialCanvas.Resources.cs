using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class TutorialCanvas
{
    [Header("Resouce ScrollView")]
    [SerializeField] private GameObject _resourceScrollView;
    [SerializeField] private GameObject _mainScrollViewResouceBtn;
    [SerializeField] private Transform _resouceScrollViewContent;

    private readonly Dictionary<string, MainScrollViewResouceBtn> _resourceBtnMap = new Dictionary<string, MainScrollViewResouceBtn>();

    private void RefreshResourceScrollView()
    {
        if (_resouceScrollViewContent == null)
            return;

        List<ResourceEntry> entriesToCreate = new List<ResourceEntry>();
        HashSet<string> visibleResourceIds = new HashSet<string>();

        foreach (KeyValuePair<string, ResourceEntry> pair in DataManager.Resource.GetAllResources())
        {
            ResourceEntry entry = pair.Value;
            if (entry.state.count == 0 && entry.state.currnetChangeCount == 0)
                continue;

            visibleResourceIds.Add(pair.Key);

            if (_resourceBtnMap.TryGetValue(pair.Key, out MainScrollViewResouceBtn existingBtn))
            {
                existingBtn.Refresh(entry);
                continue;
            }

            entriesToCreate.Add(entry);
        }

        List<string> removeIds = new List<string>();
        foreach (KeyValuePair<string, MainScrollViewResouceBtn> pair in _resourceBtnMap)
        {
            if (!visibleResourceIds.Contains(pair.Key))
                removeIds.Add(pair.Key);
        }

        foreach (string resourceId in removeIds)
        {
            MainScrollViewResouceBtn removedBtn = _resourceBtnMap[resourceId];
            _resourceBtnMap.Remove(resourceId);
            if (removedBtn != null)
                GameManager.PoolingManager.ReturnToPool(removedBtn.gameObject);
        }

        if (entriesToCreate.Count == 0)
        {
            StaggeredSpawnUtils.Stop(this, ref _resourceScrollCoroutine);
            return;
        }

        StaggeredSpawnUtils.Restart(this, ref _resourceScrollCoroutine, CreateResourceButtonsRoutine(entriesToCreate));
    }

    private IEnumerator CreateResourceButtonsRoutine(List<ResourceEntry> entriesToCreate)
    {
        PoolingManager pool = GameManager.PoolingManager;

        yield return StaggeredSpawnUtils.ForEachFrame(entriesToCreate.Count, i =>
        {
            ResourceEntry entry = entriesToCreate[i];
            if (entry?.data == null || _resourceBtnMap.ContainsKey(entry.data.id))
                return;

            GameObject btnObj = pool.GetPooledObject(_mainScrollViewResouceBtn);
            btnObj.transform.SetParent(_resouceScrollViewContent, false);
            MainScrollViewResouceBtn resourceBtn = btnObj.GetComponent<MainScrollViewResouceBtn>();
            resourceBtn.Init(entry);
            _resourceBtnMap.Add(entry.data.id, resourceBtn);
        });
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
