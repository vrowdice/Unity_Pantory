using UnityEngine;
using TMPro;

public class StroageResourceTypeBtn : MonoBehaviour
{
    [SerializeField] private TMP_Text _resourceTypeNameText;
    private ResourceType _resourceType;
    private StoragePanel _storagePanel;

    public void Init(StoragePanel storagePanel, ResourceType resourceType)
    {
        _resourceTypeNameText.text = resourceType.ToString();
        _resourceType = resourceType;
        _storagePanel = storagePanel;
    }

    public void OnClick()
    {
        _storagePanel.OnResourceTypeClick(_resourceType);
    }
}
