using System.Collections.Generic;
using UnityEngine;

public class SelectResourcePanel : MonoBehaviour
{
    [SerializeField] private GameObject _selectResourceTypeBtnPrefab;
    [SerializeField] private GameObject _selectResourceBtnPrefab;
    [SerializeField] private Transform _resourceTypeScrollViewContentTransform;
    [SerializeField] private Transform _resourceScrollViewContentTransform;

    private GameDataManager _gameDataManager = null;
    private List<ResourceType> _resourceTypes = null;

    /// <summary>
    /// 패널 초기화 (BasePanel에서 호출)
    /// </summary>
    public void OnInitialize(GameDataManager gameDataManager, List<ResourceType> resourceTypes)
    {
        _gameDataManager = gameDataManager;
        _resourceTypes = resourceTypes;

        if (_gameDataManager == null)
        {
            Debug.LogWarning("[ProductionPanel] DataManager is null.");
            return;
        }

        InitializeResourceTypeButtons(resourceTypes);
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
}
