using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SelectResourceBtn : MonoBehaviour
{
    [SerializeField] private Image _resourceIconImage;
    [SerializeField] private TextMeshProUGUI _resourceNameText;
    [SerializeField] private TextMeshProUGUI _resourceCountText;

    private SelectResourcePanel _selectResourcePanel;
    private ResourceEntry _resourceEntry;

    public void OnInitialize(SelectResourcePanel selectResourcePanel, ResourceEntry resourceEntry)
    {
        _selectResourcePanel = selectResourcePanel;
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
