using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>
/// 자원 타입별로 자원 목록을 필터링하여 사용자에게 선택 인터페이스를 제공하는 패널 클래스입니다.
/// </summary>
public class SelectResourcePanel : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject _selectResourceTypeBtnPrefab;
    [SerializeField] private GameObject _selectResourceBtnPrefab;

    [Header("Scroll Content")]
    [SerializeField] private Transform _resourceTypeScrollViewContentTransform;
    [SerializeField] private Transform _resourceScrollViewContentTransform;

    private DataManager _dataManager;
    private List<ResourceType> _resourceTypes;
    private Action<ResourceEntry> _onResourceSelected;
    private List<ResourceData> _producibleResources;

    /// <summary>
    /// 패널을 초기화하고 자원 타입 버튼 리스트를 생성합니다.
    /// </summary>
    /// <param name="gameDataManager">게임 데이터 매니저</param>
    /// <param name="resourceTypes">표시할 자원 타입 목록</param>
    /// <param name="onResourceSelected">자원 선택 시 실행될 콜백</param>
    /// <param name="producibleResources">생산 가능한 자원 목록 (null일 경우 모든 자원 표시)</param>
    public void OnInitialize(DataManager gameDataManager, List<ResourceType> resourceTypes, Action<ResourceEntry> onResourceSelected = null, List<ResourceData> producibleResources = null)
    {
        _dataManager = gameDataManager;
        _resourceTypes = resourceTypes;
        _onResourceSelected = onResourceSelected;
        _producibleResources = producibleResources;

        if (_dataManager == null)
        {
            Debug.LogWarning("[SelectResourcePanel] DataManager is null.");
            return;
        }

        InitializeResourceTypeButtons(resourceTypes);

        // 첫 번째 자원 타입으로 목록 자동 표시
        if (resourceTypes != null && resourceTypes.Count > 0)
        {
            OnResourceTypeClick(resourceTypes[0]);
        }
    }

    /// <summary>
    /// 상단 탭에 자원 타입 버튼들을 생성합니다.
    /// </summary>
    private void InitializeResourceTypeButtons(List<ResourceType> resourceTypes)
    {
        if (resourceTypes == null) return;

        GameObjectUtils.ClearChildren(_resourceTypeScrollViewContentTransform);

        foreach (ResourceType resourceType in resourceTypes)
        {
            GameObject btnObj = Instantiate(_selectResourceTypeBtnPrefab, _resourceTypeScrollViewContentTransform);
            SelectResourceTypeBtn btnScript = btnObj.GetComponent<SelectResourceTypeBtn>();

            if (btnScript != null)
            {
                btnScript.OnInitialize(this, resourceType);
            }
        }
    }

    /// <summary>
    /// 특정 자원 타입을 클릭했을 때 해당 카테고리의 자원 목록을 표시합니다.
    /// </summary>
    /// <param name="resourceType">선택된 자원 타입</param>
    public void OnResourceTypeClick(ResourceType resourceType)
    {
        GameObjectUtils.ClearChildren(_resourceScrollViewContentTransform);

        // 특정 생산 가능 목록이 지정된 경우
        if (_producibleResources != null && _producibleResources.Count > 0)
        {
            foreach (ResourceData producibleResource in _producibleResources)
            {
                if (producibleResource != null && producibleResource.type == resourceType)
                {
                    ResourceEntry resourceEntry = _dataManager.Resource.GetResourceEntry(producibleResource.id);
                    CreateResourceButton(resourceEntry);
                }
            }
        }
        else // 전체 자원에서 필터링하는 경우
        {
            Dictionary<string, ResourceEntry> resources = _dataManager.Resource.GetAllResources();
            foreach (KeyValuePair<string, ResourceEntry> pair in resources)
            {
                if (pair.Value.data.type == resourceType)
                {
                    CreateResourceButton(pair.Value);
                }
            }
        }
    }

    /// <summary>
    /// 리소스 버튼 오브젝트를 생성하고 초기화합니다.
    /// </summary>
    private void CreateResourceButton(ResourceEntry resourceEntry)
    {
        if (resourceEntry == null) return;

        GameObject btnObj = Instantiate(_selectResourceBtnPrefab, _resourceScrollViewContentTransform);
        SelectResourceBtn btnScript = btnObj.GetComponent<SelectResourceBtn>();

        if (btnScript != null)
        {
            btnScript.OnInitialize(this, resourceEntry);
        }
    }

    /// <summary>
    /// 사용자가 자원을 최종 선택했을 때 콜백을 실행하고 패널을 닫습니다.
    /// </summary>
    /// <param name="selectedResource">선택된 자원 엔트리</param>
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

        gameObject.SetActive(false);
    }

    /// <summary>
    /// 패널 오브젝트를 파괴합니다.
    /// </summary>
    public void ClosePanel()
    {
        Destroy(gameObject);
    }
}