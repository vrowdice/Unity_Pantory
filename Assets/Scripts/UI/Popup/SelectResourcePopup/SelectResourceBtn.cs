using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SelectResourceBtn : EntryListBtnBase
{
    [SerializeField] private Image _resourceIconImage;
    [SerializeField] private TextMeshProUGUI _resourceNameText;
    [SerializeField] private TextMeshProUGUI _resourceCountText;

    private SelectResourcePopup _selectResourcePanel;
    private ResourceEntry _resourceEntry;

    public void Init(SelectResourcePopup selectResourcePanel, ResourceEntry resourceEntry)
    {
        _selectResourcePanel = selectResourcePanel;
        _resourceEntry = resourceEntry;

        _resourceIconImage.sprite = _resourceEntry.data.icon;
        _resourceNameText.text = _resourceEntry.data.id.Localize(LocalizationUtils.TABLE_RESOURCE);
        Refresh();
    }

    public override void Refresh()
    {
        if (_resourceEntry == null)
            return;

        _resourceCountText.text = _resourceEntry.state.count.ToString();
    }

    protected override void HandleClick()
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
