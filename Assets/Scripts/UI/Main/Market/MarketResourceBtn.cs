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
        string currentPriceText = ReplaceUtils.FormatNumber((long)resourceState.currentValue);
        string deltaText = resourceState.priceChangeRate.ToString("+0.##;-0.##;0");

        _changeValueText.text = $"{currentPriceText} ({deltaText})";
        _changeValueText.color = GetDeltaColor(resourceState.priceChangeRate);
    }

    private Color GetDeltaColor(float delta)
    {
        VisualManager visualManager = VisualManager.Instance;
        if (visualManager != null)
        {
            return visualManager.GetDeltaColor(delta);
        }
        
        // VisualManager가 없을 경우 기본값 반환
        return Color.white;
    }
}
