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

    public void OnInitialize(MainUiManager mainUiManager, ResourceEntry resourceEntry)
    {
        _mainUiManager = mainUiManager;
        _resourceEntry = resourceEntry;
        _image.sprite = resourceEntry.data.icon;
        _valueText.text = resourceEntry.state.count.ToString("N0");

        UpdateChangeValue();
    }

    public void OnClick()
    {
        
    }

    private void UpdateChangeValue()
    {
        var resourceState = _resourceEntry?.state;
        if (_changeValueText == null || resourceState == null)
        {
            return;
        }

        long delta = resourceState.threadDeltaCount;
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
            _changeValueText.text = "";
        }
    }
}
