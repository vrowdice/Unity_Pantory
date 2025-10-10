using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 생산 관리 패널
/// </summary>
public class StoragePanel : BasePanel
{
    [SerializeField] private GameObject _storageResourceBtnPrefab;
    [SerializeField] private TMP_Dropdown _resourceCategoryDropdown;
    [SerializeField] private Transform _resourceScrollViewContentTransform;

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

        if (_resourceCategoryDropdown.options.Count == 0)
        {
            EnumUtils.GetAllEnumValues<ResourceType>().ForEach(resourceType =>
            {
                _resourceCategoryDropdown.options.Add(new TMP_Dropdown.OptionData(resourceType.ToString()));
            });

            _resourceCategoryDropdown.onValueChanged.AddListener(OnResourceCategoryDropdownValueChanged);

            OnResourceCategoryDropdownValueChanged(0);
        }
    }

    void OnResourceCategoryDropdownValueChanged(int value)
    {
        _resourceCategoryDropdown.value = value;

        GameObjectUtils.ClearChildren(_resourceScrollViewContentTransform);

        foreach (var resourceEntry in _dataManager.Resource.GetAllResources())
        {
            if(_resourceCategoryDropdown.value == (int)resourceEntry.Value.resourceData.type)
            {
                Instantiate(_storageResourceBtnPrefab, _resourceScrollViewContentTransform).
                    GetComponent<StorageResourceBtn>().OnInitialize(resourceEntry.Value);
            }
        }

        Debug.Log($"[{GetType().Name}] Resource category dropdown value changed to {_resourceCategoryDropdown.value}");
    }

    void Update()
    {
        // 업데이트 로직
    }
}
