using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 창고(플레이어 인벤토리) 관리 패널
/// </summary>
public class StoragePanel : BasePanel
{
    [SerializeField] private GameObject _storageResourceBtnPrefab;
    [SerializeField] private Transform _resourceTypeScrollViewContentTransform;
    [SerializeField] private Transform _resourceScrollViewContentTransform;

    private ResourceType _currentResourceType = ResourceType.raw;

    /// <summary>
    /// 패널 초기화 (BasePanel에서 호출)
    /// </summary>
    protected override void OnInitialize()
    {
        if (_dataManager == null)
        {
            Debug.LogWarning("[StoragePanel] DataManager is null.");
            return;
        }

        InitializeResourceTypeButtons();
        RefreshCurrentResourceTypeList();
    }

    private void Start()
    {
        _dataManager.Resource.OnResourceChanged += RefreshCurrentResourceTypeList;
    }

    private void OnDestroy()
    {
        _dataManager.Resource.OnResourceChanged -= RefreshCurrentResourceTypeList;
    }

    /// <summary>
    /// 리소스 타입 버튼 초기화
    /// GameManager의 공통 ActionBtn 프리팹을 사용하여 타입별 버튼을 생성합니다.
    /// 패널이 다시 열릴 때 중복 생성을 막기 위해 먼저 자식들을 정리합니다.
    /// </summary>
    private void InitializeResourceTypeButtons()
    {
        if (_gameManager?.ActionBtnPrefab == null || _resourceTypeScrollViewContentTransform == null)
        {
            Debug.LogWarning("[StoragePanel] ActionBtnPrefab or _resourceTypeScrollViewContentTransform is null.");
            return;
        }

        GameObjectUtils.ClearChildren(_resourceTypeScrollViewContentTransform);

        foreach (var resourceType in EnumUtils.GetAllEnumValues<ResourceType>())
        {
            var btnObj = Instantiate(_gameManager.ActionBtnPrefab, _resourceTypeScrollViewContentTransform);
            var btn = btnObj.GetComponent<ActionBtn>();
            if (btn != null)
            {
                var capturedType = resourceType;
                btn.OnInitialize(capturedType.ToString(), () => OnResourceTypeClick(capturedType));
            }
        }
    }

    /// <summary>
    /// 리소스 타입 버튼 클릭 시 호출
    /// </summary>
    /// <param name="resourceType">선택된 리소스 타입</param>
    public void OnResourceTypeClick(ResourceType resourceType)
    {
        _currentResourceType = resourceType;
        RefreshCurrentResourceTypeList();
    }

    /// <summary>
    /// 현재 선택된 타입 기준으로 리소스 리스트를 다시 그림
    /// (타입 버튼 클릭 및 자원 변화 이벤트에서 공통 사용)
    /// </summary>
    private void RefreshCurrentResourceTypeList()
    {
        if (_dataManager == null || _dataManager.Resource == null)
        {
            return;
        }

        if (_resourceScrollViewContentTransform != null)
        {
            GameObjectUtils.ClearChildren(_resourceScrollViewContentTransform);
        }

        foreach (var resourceEntry in _dataManager.Resource.GetAllResources())
        {
            if (resourceEntry.Value.resourceData.type == _currentResourceType)
            {
                Instantiate(_storageResourceBtnPrefab, _resourceScrollViewContentTransform)
                    .GetComponent<StorageResourceBtn>()
                    .OnInitialize(this, resourceEntry.Value);
            }
        }
    }
}
