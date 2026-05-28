using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MainScrollViewResouceBtn : BtnBase
{
    [SerializeField] private Image _image = null;
    [SerializeField] private TextMeshProUGUI _valueText = null;
    [SerializeField] private TextMeshProUGUI _changeValueText = null;

    private MainCanvas _mainUiManager = null;
    private ResourceEntry _resourceEntry = null;

    public string ResourceId => _resourceEntry?.data?.id;

    public void Init(ResourceEntry resourceEntry)
    {
        _resourceEntry = resourceEntry;
        RefreshDisplay();
    }

    public void Init(MainCanvas mainUiManager, ResourceEntry resourceEntry)
    {
        _mainUiManager = mainUiManager;
        Init(resourceEntry);
    }

    public void Refresh(ResourceEntry resourceEntry)
    {
        _resourceEntry = resourceEntry;
        RefreshDisplay();
    }

    protected override void HandleClick()
    {
    }

    private void RefreshDisplay()
    {
        if (_resourceEntry == null)
            return;

        _image.sprite = _resourceEntry.data.icon;
        _valueText.text = _resourceEntry.state.count.ToString("N0");
        UpdateChangeValue();
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
