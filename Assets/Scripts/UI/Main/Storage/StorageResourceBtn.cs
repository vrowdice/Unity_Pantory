using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StorageResourceBtn : MonoBehaviour
{
    [SerializeField] private Image _resourceIconImage;
    [SerializeField] private TextMeshProUGUI _resourceNameText;
    [SerializeField] private TextMeshProUGUI _resourceCountText;
    private ResourceEntry _resourceEntry;

    public void OnInitialize(ResourceEntry resourceEntry)
{
    _resourceEntry = resourceEntry;
    _resourceIconImage.sprite = _resourceEntry.resourceData.icon;
    _resourceNameText.text = _resourceEntry.resourceData.name;
    _resourceCountText.text = _resourceEntry.resourceState.count.ToString();
}

    public void OnClick()
    {
        Debug.Log("OnClick");
    }
}
