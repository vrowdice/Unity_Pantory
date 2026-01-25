using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StorageResourceBtn : MonoBehaviour
{
    [SerializeField] private Image _resourceIconImage;
    [SerializeField] private TextMeshProUGUI _resourceNameText;
    [SerializeField] private TextMeshProUGUI _resourceCountText;

    private StoragePanel _storagePanel;
    private ResourceEntry _resourceEntry;

    public void Init(StoragePanel storagePanel, ResourceEntry resourceEntry)
    {
        _storagePanel = storagePanel;
        _resourceEntry = resourceEntry;

        _resourceIconImage.sprite = _resourceEntry.data.icon;
        _resourceNameText.text = _resourceEntry.data.id.Localize(LocalizationUtils.TABLE_RESOURCE_DISPLAY_NAME);
        _resourceCountText.text = _resourceEntry.state.count.ToString("N0");
    }

    public void OnClick()
    {
        Debug.Log("OnClick");
    }
}
