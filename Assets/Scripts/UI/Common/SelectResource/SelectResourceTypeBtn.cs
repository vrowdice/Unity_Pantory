using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SelectResourceTypeBtn : MonoBehaviour
{
    [SerializeField] private TMP_Text _resourceTypeNameText;
    private ResourceType _resourceType;
    private SelectResourcePanel _selectResourcePanel;

    public void OnInitialize(SelectResourcePanel selectResourcePanel, ResourceType resourceType)
    {
        _resourceTypeNameText.text = resourceType.ToString();
        _resourceType = resourceType;
        _selectResourcePanel = selectResourcePanel;
    }

    public void OnClick()
    {
        _selectResourcePanel.OnResourceTypeClick(_resourceType);
    }
}
