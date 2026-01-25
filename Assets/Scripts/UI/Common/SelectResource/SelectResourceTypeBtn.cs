using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Localization;

public class SelectResourceTypeBtn : MonoBehaviour
{
    [SerializeField] private TMP_Text _resourceTypeNameText;
    private ResourceType _resourceType;
    private SelectResourcePopup _selectResourcePanel;

    public void Init(SelectResourcePopup selectResourcePanel, ResourceType resourceType)
    {
        string key = resourceType.ToString();
        LocalizedString localizedString = new LocalizedString("ResourceType", key);
        _resourceTypeNameText.text = localizedString.GetLocalizedString();
        _resourceType = resourceType;
        _selectResourcePanel = selectResourcePanel;
    }

    public void OnClick()
    {
        _selectResourcePanel.OnResourceTypeClick(_resourceType);
    }
}
