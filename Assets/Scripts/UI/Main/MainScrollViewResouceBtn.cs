using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MainScrollViewResouceBtn : MonoBehaviour
{
    [SerializeField] private Image _image = null;
    [SerializeField] private TextMeshProUGUI _valueText = null;
    [SerializeField] private TextMeshProUGUI _changeValueText = null;

    private MainUiManager _mainUiManager = null;
    private ResourceEntry _resourceEntry = null;

    public void OnInitialize(MainUiManager argMainUiManager, ResourceEntry resourceEntry)
    {
        _mainUiManager = argMainUiManager;
        _resourceEntry = resourceEntry;

        _image.sprite = resourceEntry.resourceData.icon;
        _valueText.text = resourceEntry.resourceState.count.ToString();
        UpdateChangeValue();
    }

    public void OnClick()
    {
        
    }

    private void UpdateChangeValue()
    {
        var resourceState = _resourceEntry?.resourceState;
        if (_changeValueText == null || resourceState == null)
        {
            return;
        }

        long delta = resourceState.deltaCount;
        if (delta > 0)
        {
            _changeValueText.text = $"+{delta:N0}";
        }
        else if (delta < 0)
        {
            _changeValueText.text = delta.ToString("N0");
        }
        else
        {
            _changeValueText.text = "0";
        }
    }
}
