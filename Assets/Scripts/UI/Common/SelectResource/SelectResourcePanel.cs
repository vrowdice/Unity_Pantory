using System.Collections.Generic;
using UnityEngine;
using System;

public class SelectResourcePanel : MonoBehaviour
{
    [SerializeField] private GameObject _selectResourceTypeBtnPrefab;
    [SerializeField] private GameObject _selectResourceBtnPrefab;
    [SerializeField] private Transform _resourceTypeScrollViewContentTransform;
    [SerializeField] private Transform _resourceScrollViewContentTransform;

    private DataManager _dataManager = null;
    private List<ResourceType> _resourceTypes = null;
    private Action<ResourceEntry> _onResourceSelected = null;
    private List<ResourceData> _producibleResources = null; // 생산 가능한 자원 목록 (옵션)

    /// <summary>
    /// 패널 초기화
    /// </summary>
    /// <param name="gameDataManager">게임 데이터 매니저</param>
    /// <param name="resourceTypes">허용된 자원 타입 목록</param>
    /// <param name="onResourceSelected">자원 선택 콜백</param>
    /// <param name="producibleResources">생산 가능한 자원 목록 (null이면 해당 타입의 모든 자원 표시)</param>
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
        
        // 첫 번째 자원 타입으로 자동 표시
        if (resourceTypes != null && resourceTypes.Count > 0)
        {
            OnResourceTypeClick(resourceTypes[0]);
        }
    }

    private void InitializeResourceTypeButtons(List<ResourceType> resourceTypes)
    {
        foreach (var resourceType in resourceTypes)
        {
            Instantiate(_selectResourceTypeBtnPrefab, _resourceTypeScrollViewContentTransform).
                GetComponent<SelectResourceTypeBtn>().OnInitialize(this, resourceType);
        }
    }

    public void OnResourceTypeClick(ResourceType resourceType)
    {
        GameObjectUtils.ClearChildren(_resourceScrollViewContentTransform);
        
        // 생산 가능한 자원 목록이 있으면 그것만 표시, 없으면 해당 타입의 모든 자원 표시
        if (_producibleResources != null && _producibleResources.Count > 0)
        {
            // 생산 가능한 자원 중에서 해당 타입인 것만 필터링하여 표시
            foreach (var producibleResource in _producibleResources)
            {
                if (producibleResource != null && producibleResource.type == resourceType)
                {
                    ResourceEntry resourceEntry = _dataManager.Resource.GetResourceEntry(producibleResource.id);
                    if (resourceEntry != null)
                    {
                        Instantiate(_selectResourceBtnPrefab, _resourceScrollViewContentTransform).
                            GetComponent<SelectResourceBtn>().OnInitialize(this, resourceEntry);
                    }
                }
            }
        }
        else
        {
            // 생산 가능한 자원 목록이 없으면 해당 타입의 모든 자원 표시
            foreach (var resourceEntry in _dataManager.Resource.GetAllResources())
            {
                if (resourceEntry.Value.data.type == resourceType)
                {
                    Instantiate(_selectResourceBtnPrefab, _resourceScrollViewContentTransform).
                        GetComponent<SelectResourceBtn>().OnInitialize(this, resourceEntry.Value);
                }
            }
        }
    }

    /// <summary>
    /// 자원이 선택되었을 때 호출되는 메서드
    /// </summary>
    /// <param name="selectedResource">선택된 자원</param>
    public void OnResourceSelected(ResourceEntry selectedResource)
    {
        if (_onResourceSelected != null)
        {
            _onResourceSelected.Invoke(selectedResource);
            Debug.Log($"[SelectResourcePanel] Resource selected: {selectedResource.data.displayName}");
        }
        else
        {
            Debug.LogWarning("[SelectResourcePanel] No callback registered for resource selection.");
        }

        // 패널 닫기
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 패널을 닫습니다.
    /// </summary>
    public void ClosePanel()
    {
        Destroy(gameObject);
    }
}
