using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MarketResourceBtn : MonoBehaviour
{
    [SerializeField] private Image _image = null;
    [SerializeField] private TextMeshProUGUI _nameText = null;
    [SerializeField] private TextMeshProUGUI _changeValueText = null;

    private MarketPanel _marketPanel = null;
    private ResourceEntry _resourceEntry = null;

    public void OnInitialize(MarketPanel argMarketPanel, ResourceEntry resourceEntry)
    {
        _marketPanel = argMarketPanel;
        _resourceEntry = resourceEntry;

        _image.sprite = resourceEntry.resourceData.icon;
        _nameText.text = resourceEntry.resourceData.displayName;
        UpdateChangeValue();
    }

    public void OnClick()
    {
        if (_marketPanel == null || _resourceEntry == null)
        {
            return;
        }

        _marketPanel.HandleResourceButtonClicked(_resourceEntry);
    }

    private void UpdateChangeValue()
    {
        if (_changeValueText == null || _resourceEntry?.resourceState == null)
        {
            return;
        }

        var resourceState = _resourceEntry.resourceState;
        string currentPriceText = resourceState.currentValue.ToString("N0");
        string deltaText = resourceState.priceChangeRate.ToString("+0.##;-0.##;0");

        _changeValueText.text = $"{currentPriceText} ({deltaText})";
        _changeValueText.color = GetDeltaColor(resourceState.priceChangeRate);
    }

    private Color GetDeltaColor(float delta)
    {
        VisualManager visualManager = VisualManager.Instance;
        
        if (delta > 0f)
        {
            return visualManager != null ? visualManager.ProfitColor : Color.blue;
        }

        if (delta < 0f)
        {
            return visualManager != null ? visualManager.LossColor : Color.red;
        }

        return Color.white;
    }
}
