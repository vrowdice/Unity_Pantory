using UnityEngine;
using TMPro;
using UnityEngine.Localization;

public class SelectResourceTypeBtn : BtnBase
{
    [SerializeField] private TMP_Text _resourceTypeNameText;
    private ResourceType _resourceType;
    private SelectResourcePopup _selectResourcePanel;

    public void Init(SelectResourcePopup selectResourcePanel, ResourceType resourceType)
    {
        string key = resourceType.ToString();
        LocalizedString localizedString = new LocalizedString("Resource", key);
        _resourceTypeNameText.text = localizedString.GetLocalizedString();
        _resourceType = resourceType;
        _selectResourcePanel = selectResourcePanel;
    }

    protected override void HandleClick()
    {
        _selectResourcePanel.OnResourceTypeClick(_resourceType);
    }
}
