using System.Collections.Generic;
using UnityEngine;
using System;

public class SelectResourcePanel : MonoBehaviour
{
    [SerializeField] private GameObject _selectResourceTypeBtnPrefab;
    [SerializeField] private GameObject _selectResourceBtnPrefab;
    [SerializeField] private Transform _resourceTypeScrollViewContentTransform;
    [SerializeField] private Transform _resourceScrollViewContentTransform;

    private GameDataManager _gameDataManager = null;
    private List<ResourceType> _resourceTypes = null;
    private Action<ResourceEntry> _onResourceSelected = null;

    /// <summary>
    /// �г� �ʱ�ȭ (BasePanel���� ȣ��)
    /// </summary>
    public void OnInitialize(GameDataManager gameDataManager, List<ResourceType> resourceTypes, Action<ResourceEntry> onResourceSelected = null)
    {
        _gameDataManager = gameDataManager;
        _resourceTypes = resourceTypes;
        _onResourceSelected = onResourceSelected;

        if (_gameDataManager == null)
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
        foreach (var resourceEntry in _gameDataManager.Resource.GetAllResources())
        {
            if (resourceEntry.Value.resourceData.type == resourceType)
            {
                Instantiate(_selectResourceBtnPrefab, _resourceScrollViewContentTransform).
                    GetComponent<SelectResourceBtn>().OnInitialize(this ,resourceEntry.Value);
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
            Debug.Log($"[SelectResourcePanel] Resource selected: {selectedResource.resourceData.displayName}");
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
