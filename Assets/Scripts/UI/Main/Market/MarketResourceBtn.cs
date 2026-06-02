using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 시장 리스트 내 개별 리소스 항목을 관리하고 UI를 갱신하는 컴포넌트입니다.
/// </summary>
public class MarketResourceBtn : EntryListBtnBase
{
    [Header("UI Components")]
    [SerializeField] private Image _image = null;
    [SerializeField] private TextMeshProUGUI _nameText = null;
    [SerializeField] private TextMeshProUGUI _deltaText = null;
    [SerializeField] private TextMeshProUGUI _tradeValueText = null;

    private MarketCanvas _marketPanel = null;
    private ResourceEntry _resourceEntry = null;

    public void Init(MarketCanvas argMarketPanel, ResourceEntry resourceEntry)
    {
        _marketPanel = argMarketPanel;
        _resourceEntry = resourceEntry;

        _image.sprite = resourceEntry.data.icon;
        _nameText.text = resourceEntry.data.id.Localize(LocalizationUtils.TABLE_RESOURCE);

        Refresh();
    }

    public override void Refresh()
    {
        UpdateChangeValue();
        UpdateTradeValue();
    }

    protected override void HandleClick()
    {
        _marketPanel.OnResourceButtonClicked(_resourceEntry);
    }

    private void UpdateChangeValue()
    {
        ResourceState resourceState = _resourceEntry.state;
        long delta = resourceState.currentChangeValue;

        string priceText = ReplaceUtils.FormatNumber(resourceState.currentValue);
        string deltaSymbol = delta > 0 ? "+" : "";
        string deltaValueText = ReplaceUtils.FormatNumber(delta);

        _deltaText.text = $"{priceText} ({deltaSymbol}{deltaValueText})";
        _deltaText.color = _marketPanel.VisualManager.GetDeltaColor(delta);
    }

    private void UpdateTradeValue()
    {
        ResourceState resourceState = _resourceEntry.state;
        long tradeDelta = resourceState.marketDeltaCount;

        string deltaSymbol = tradeDelta > 0 ? "+" : "";
        string tradeText = tradeDelta == 0 ? "0" : $"{deltaSymbol}{ReplaceUtils.FormatNumberWithCommas(tradeDelta)}";

        _tradeValueText.text = tradeText;
        _tradeValueText.color = _marketPanel.VisualManager.GetDeltaColor(tradeDelta);
    }
}
