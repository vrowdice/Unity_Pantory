using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 생산 관리 패널
/// </summary>
public class StoragePanel : BasePanel
{
    [SerializeField] private GameObject _storageResourceTypeBtnPrefab;
    [SerializeField] private GameObject _storageResourceBtnPrefab;
    [SerializeField] private Transform _resourceTypeScrollViewContentTransform;
    [SerializeField] private Transform _resourceScrollViewContentTransform;

    private void Start()
    {
        OnResourceTypeClick(ResourceType.raw);
    }

    /// <summary>
    /// 패널 초기화 (BasePanel에서 호출)
    /// </summary>
    protected override void OnInitialize()
    {
        if (_dataManager == null)
        {
            Debug.LogWarning("[ProductionPanel] DataManager is null.");
            return;
        }

        InitializeResourceTypeButtons();
    }

    private void InitializeResourceTypeButtons()
    {
        foreach (var resourceType in EnumUtils.GetAllEnumValues<ResourceType>())
        {
            Instantiate(_storageResourceTypeBtnPrefab, _resourceTypeScrollViewContentTransform).
                GetComponent<StroageResourceTypeBtn>().OnInitialize(this, resourceType);
        }
    }

    public void OnResourceTypeClick(ResourceType resourceType)
    {
        GameObjectUtils.ClearChildren(_resourceScrollViewContentTransform);
        foreach (var resourceEntry in _dataManager.Resource.GetAllResources())
        {
            if(resourceEntry.Value.resourceData.type == resourceType)
            {
                Instantiate(_storageResourceBtnPrefab, _resourceScrollViewContentTransform).
                    GetComponent<StorageResourceBtn>().OnInitialize(this, resourceEntry.Value);
            }
        }
    }
}
