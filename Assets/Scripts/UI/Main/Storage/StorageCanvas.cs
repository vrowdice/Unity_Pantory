using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 창고(플레이어 인벤토리) 관리 패널
/// </summary>
public class StorageCanvas : MainCanvasPanelBase
{
    [SerializeField] private GameObject _storageResourceBtnPrefab;
    [SerializeField] private Transform _resourceTypeScrollViewContentTransform;
    [SerializeField] private Transform _resourceScrollViewContentTransform;

    private ResourceType? _currentResourceType = null;
    private List<ActionBtn> _categoryButtons = new List<ActionBtn>();
    private Coroutine _categoryButtonCoroutine;
    private Coroutine _resourceListCoroutine;

    /// <summary>
    /// 패널 초기화 (BasePanel에서 호출)
    /// </summary>
    public override void Init(MainCanvas argUIManager)
    {
        Init((IBuildScenePanelHost)argUIManager);
    }

    public override void Init(IBuildScenePanelHost panelHost)
    {
        base.Init(panelHost);

        _dataManager.Resource.OnResourceChanged -= RefreshCurrentResourceTypeList;
        _dataManager.Resource.OnResourceChanged += RefreshCurrentResourceTypeList;

        InitializeResourceTypeButtons();
        RefreshCurrentResourceTypeList();
    }

    protected override void OnDisable()
    {
        StaggeredSpawnUtils.Stop(this, ref _categoryButtonCoroutine);
        StaggeredSpawnUtils.Stop(this, ref _resourceListCoroutine);
        base.OnDisable();

        if (_dataManager != null)
            _dataManager.Resource.OnResourceChanged -= RefreshCurrentResourceTypeList;
    }

    /// <summary>
    /// 리소스 타입 버튼 초기화
    /// </summary>
    private void InitializeResourceTypeButtons()
    {
        int targetCount = EnumUtils.GetAllEnumValues<ResourceType>().Count + 1;
        if (_resourceTypeScrollViewContentTransform.childCount == targetCount)
        {
            _categoryButtons.Clear();
            foreach (Transform child in _resourceTypeScrollViewContentTransform)
            {
                ActionBtn btn = child.GetComponent<ActionBtn>();
                if (btn != null)
                {
                    _categoryButtons.Add(btn);
                }
            }
            UpdateCategoryHighlight();
            return;
        }

        _gameManager.PoolingManager.ClearChildrenToPool(_resourceTypeScrollViewContentTransform);
        _categoryButtons.Clear();

        List<(ResourceType? type, string label)> categoryDefs = new List<(ResourceType? type, string label)>
        {
            (null, LocalizationUtils.Localize("All"))
        };

        foreach (ResourceType resourceType in EnumUtils.GetAllEnumValues<ResourceType>())
            categoryDefs.Add((resourceType, resourceType.Localize(LocalizationUtils.TABLE_RESOURCE)));

        StaggeredSpawnUtils.Restart(this, ref _categoryButtonCoroutine, CreateCategoryButtonsRoutine(categoryDefs));
        UpdateCategoryHighlight();
    }

    private IEnumerator CreateCategoryButtonsRoutine(List<(ResourceType? type, string label)> categoryDefs)
    {
        yield return StaggeredSpawnUtils.ForEachFrame(categoryDefs.Count, i =>
        {
            (ResourceType? type, string label) def = categoryDefs[i];
            CreateCategoryButton(def.type, def.label);
        });
    }

    /// <summary>
    /// 카테고리 버튼 생성
    /// </summary>
    private void CreateCategoryButton(ResourceType? type, string label)
    {
        GameObject btnObj = _gameManager.PoolingManager.GetPooledObject(_panelUIManager.ActionBtnPrefab);
        btnObj.transform.SetParent(_resourceTypeScrollViewContentTransform, false);
        ActionBtn btn = btnObj.GetComponent<ActionBtn>();
        
        ResourceType? capturedType = type;
        btn.Init(label, () => {
            OnResourceTypeClick(capturedType);
            UpdateCategoryHighlight();
        });

        _categoryButtons.Add(btn);
    }

    /// <summary>
    /// 카테고리 버튼 하이라이트 업데이트
    /// </summary>
    private void UpdateCategoryHighlight()
    {
        if (_categoryButtons.Count == 0) return;

        _categoryButtons[0].SetHighlight(_currentResourceType == null);

        List<ResourceType> types = EnumUtils.GetAllEnumValues<ResourceType>();
        for (int i = 0; i < types.Count && i + 1 < _categoryButtons.Count; i++)
        {
            _categoryButtons[i + 1].SetHighlight(_currentResourceType == types[i]);
        }
    }

    /// <summary>
    /// 리소스 타입 버튼 클릭 시 호출
    /// </summary>
    /// <param name="resourceType">선택된 리소스 타입</param>
    public void OnResourceTypeClick(ResourceType? resourceType)
    {
        _currentResourceType = resourceType;
        RefreshCurrentResourceTypeList();
        UpdateCategoryHighlight();
    }

    /// <summary>
    /// 현재 선택된 타입 기준으로 리소스 리스트를 다시 그림
    /// </summary>
    private void RefreshCurrentResourceTypeList()
    {
        StaggeredSpawnUtils.Restart(this, ref _resourceListCoroutine, RefreshCurrentResourceTypeListRoutine());
    }

    private IEnumerator RefreshCurrentResourceTypeListRoutine()
    {
        if (!gameObject.activeInHierarchy)
            yield break;

        ScrollRect scroll = _resourceScrollViewContentTransform.GetComponentInParent<ScrollRect>();
        if (scroll != null)
            scroll.enabled = false;

        _gameManager.PoolingManager.ClearChildrenToPool(_resourceScrollViewContentTransform);

        List<ResourceEntry> entries = new List<ResourceEntry>();
        foreach (KeyValuePair<string, ResourceEntry> resourceEntry in _dataManager.Resource.GetAllResources())
        {
            if (_currentResourceType == null || resourceEntry.Value.data.type == _currentResourceType.Value)
                entries.Add(resourceEntry.Value);
        }

        yield return StaggeredSpawnUtils.ForEachFrame(entries.Count, i =>
        {
            ResourceEntry entry = entries[i];
            GameObject btnObj = _gameManager.PoolingManager.GetPooledObject(_storageResourceBtnPrefab);
            btnObj.transform.SetParent(_resourceScrollViewContentTransform, false);
            btnObj.GetComponent<StorageResourceBtn>().Init(this, entry);
        });

        if (scroll != null)
            scroll.enabled = true;
    }
}
