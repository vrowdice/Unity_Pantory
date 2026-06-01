using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MainScrollViewResouceBtn : EntryListBtnBase
{
    [SerializeField] private Image _image = null;
    [SerializeField] private TextMeshProUGUI _valueText = null;
    [SerializeField] private TextMeshProUGUI _changeValueText = null;

    private ResourceEntry _resourceEntry = null;

    public string ResourceId => _resourceEntry?.data?.id;

    public void Init(ResourceEntry resourceEntry)
    {
        _resourceEntry = resourceEntry;

        if (_resourceEntry?.data != null)
            _image.sprite = _resourceEntry.data.icon;

        Refresh();
    }

    public void Init(MainCanvas mainUiManager, ResourceEntry resourceEntry)
    {
        Init(resourceEntry);
    }

    public override void Refresh()
    {
        if (_resourceEntry == null)
            return;

        _valueText.text = _resourceEntry.state.count.ToString("N0");
        UpdateChangeValue();
    }

    protected override void HandleClick()
    {
    }

    private void UpdateChangeValue()
    {
        ResourceState resourceState = _resourceEntry.state;
        int delta = resourceState.currnetChangeCount;

        if (delta > 0)
            _changeValueText.text = $"+{delta:N0}";
        else if (delta < 0)
            _changeValueText.text = delta.ToString("N0");
        else
            _changeValueText.text = "";
    }
}
