using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 자원 타입별로 자원 목록을 필터링하여 사용자에게 선택 인터페이스를 제공하는 패널 클래스입니다.
/// </summary>
public class SelectResourcePopup : PopupBase
{
    [Header("Prefabs")]
    [SerializeField] private GameObject _selectResourceTypeBtnPrefab;
    [SerializeField] private GameObject _selectResourceBtnPrefab;

    [Header("Scroll Content")]
    [SerializeField] private Transform _resourceTypeScrollViewContentTransform;
    [SerializeField] private Transform _resourceScrollViewContentTransform;

    private List<ResourceType> _resourceTypes;
    private Action<ResourceEntry> _onResourceSelected;
    private List<ResourceData> _producibleResources;
    private Coroutine _typeButtonCoroutine;
    private Coroutine _resourceListCoroutine;

    public void Init(DataManager gameDataManager, List<ResourceType> resourceTypes, Action<ResourceEntry> onResourceSelected = null, List<ResourceData> producibleResources = null)
    {
        base.Init();

        SetDataManager(gameDataManager);
        _resourceTypes = resourceTypes;
        _onResourceSelected = onResourceSelected;
        _producibleResources = producibleResources;

        StaggeredSpawnUtils.Restart(this, ref _typeButtonCoroutine, InitializeResourceTypeButtonsRoutine(resourceTypes));
        if (resourceTypes != null && resourceTypes.Count > 0)
        {
            OnResourceTypeClick(resourceTypes[0]);
        }

        Show();
    }

    public override void Close()
    {
        StaggeredSpawnUtils.Stop(this, ref _typeButtonCoroutine);
        StaggeredSpawnUtils.Stop(this, ref _resourceListCoroutine);
        base.Close();
    }

    private IEnumerator InitializeResourceTypeButtonsRoutine(List<ResourceType> resourceTypes)
    {
        if (resourceTypes == null)
            yield break;

        GameObjectUtils.ClearChildren(_resourceTypeScrollViewContentTransform);

        yield return StaggeredSpawnUtils.ForEachFrame(resourceTypes.Count, i =>
        {
            ResourceType resourceType = resourceTypes[i];
            GameObject btnObj = Instantiate(_selectResourceTypeBtnPrefab, _resourceTypeScrollViewContentTransform);
            SelectResourceTypeBtn btnScript = btnObj.GetComponent<SelectResourceTypeBtn>();

            if (btnScript != null)
                btnScript.Init(this, resourceType);
        });
    }

    public void OnResourceTypeClick(ResourceType resourceType)
    {
        StaggeredSpawnUtils.Restart(this, ref _resourceListCoroutine, PopulateResourceListRoutine(resourceType));
    }

    private IEnumerator PopulateResourceListRoutine(ResourceType resourceType)
    {
        GameObjectUtils.ClearChildren(_resourceScrollViewContentTransform);

        List<ResourceEntry> entries = new List<ResourceEntry>();
        if (_producibleResources != null && _producibleResources.Count > 0)
        {
            foreach (ResourceData producibleResource in _producibleResources)
            {
                if (producibleResource != null && producibleResource.type == resourceType)
                {
                    ResourceEntry resourceEntry = _dataManager.Resource.GetResourceEntry(producibleResource.id);
                    if (resourceEntry != null)
                        entries.Add(resourceEntry);
                }
            }
        }
        else
        {
            Dictionary<string, ResourceEntry> resources = _dataManager.Resource.GetAllResources();
            foreach (KeyValuePair<string, ResourceEntry> pair in resources)
            {
                if (pair.Value.data.type == resourceType)
                    entries.Add(pair.Value);
            }
        }

        yield return StaggeredSpawnUtils.ForEachFrame(entries.Count, i =>
        {
            CreateResourceButton(entries[i]);
        });
    }

    private void CreateResourceButton(ResourceEntry resourceEntry)
    {
        if (resourceEntry == null)
            return;

        GameObject btnObj = Instantiate(_selectResourceBtnPrefab, _resourceScrollViewContentTransform);
        SelectResourceBtn btnScript = btnObj.GetComponent<SelectResourceBtn>();

        if (btnScript != null)
            btnScript.Init(this, resourceEntry);
    }

    public void OnResourceSelected(ResourceEntry selectedResource)
    {
        if (_onResourceSelected != null)
        {
            _onResourceSelected.Invoke(selectedResource);
        }
        else
        {
            Debug.LogWarning("[SelectResourcePanel] No callback registered for resource selection.");
        }

        Close();
    }

    public void ClosePanel()
    {
        CloseAndDestroy();
    }
}
