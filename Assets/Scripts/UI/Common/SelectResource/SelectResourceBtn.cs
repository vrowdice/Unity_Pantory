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

        _resourceIconImage.sprite = _resourceEntry.data.icon;
        _resourceNameText.text = _resourceEntry.data.displayName;
        _resourceCountText.text = _resourceEntry.state.count.ToString();
    }

    public void OnClick()
    {
        if (_selectResourcePanel != null && _resourceEntry != null)
        {
            _selectResourcePanel.OnResourceSelected(_resourceEntry);
        }
        else
        {
            Debug.LogWarning("[SelectResourceBtn] SelectResourcePanel or ResourceEntry is null.");
        }
    }
}
