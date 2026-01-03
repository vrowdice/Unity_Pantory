using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MarketResourceBtn : MonoBehaviour
{
    [SerializeField] private Image _image = null;
    [SerializeField] private TextMeshProUGUI _nameText = null;
    [SerializeField] private TextMeshProUGUI _changeValueText = null;
    [SerializeField] private TextMeshProUGUI _tradeValueText = null;

    private MarketPanel _marketPanel = null;
    private ResourceEntry _resourceEntry = null;

    public void OnInitialize(MarketPanel argMarketPanel, ResourceEntry resourceEntry)
    {
        _marketPanel = argMarketPanel;
        _resourceEntry = resourceEntry;

        _image.sprite = resourceEntry.data.icon;
        _nameText.text = resourceEntry.data.displayName;
        UpdateChangeValue();
        UpdateTradeValue();
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
        if (_changeValueText == null || _resourceEntry?.state == null)
        {
            return;
        }

        ResourceState resourceState = _resourceEntry.state;
        string currentPriceText = ReplaceUtils.FormatNumber((long)resourceState.value);
        string deltaText = resourceState.currentChangeValue.ToString("+0.##;-0.##;0");

        _changeValueText.text = $"{currentPriceText} ({deltaText})";
        _changeValueText.color = GetDeltaColor(resourceState.currentChangeValue);
    }

    private Color GetDeltaColor(float delta)
    {
        VisualManager visualManager = VisualManager.Instance;
        if (visualManager != null)
        {
            return visualManager.GetDeltaColor(delta);
        }

        return Color.black;
    }

    /// <summary>
    /// 플레이어의 매수/매도 개수를 표시합니다.
    /// </summary>
    private void UpdateTradeValue()
    {
        if (_tradeValueText == null || _resourceEntry?.state == null)
        {
            return;
        }

        var resourceState = _resourceEntry.state;
        long tradeDelta = resourceState.threadDeltaCount;

        if (tradeDelta > 0)
        {
            // 매수: 양수 (녹색)
            _tradeValueText.text = $"+{tradeDelta:N0}";
            _tradeValueText.color = Color.green;
        }
        else if (tradeDelta < 0)
        {
            // 매도: 음수 (빨간색)
            _tradeValueText.text = tradeDelta.ToString("N0");
            _tradeValueText.color = Color.red;
        }
        else
        {
            // 거래 없음
            _tradeValueText.text = "0";
            _tradeValueText.color = Color.black;
        }
    }

    /// <summary>
    /// 외부에서 거래 값 업데이트를 요청할 때 사용합니다.
    /// </summary>
    public void RefreshTradeValue()
    {
        UpdateTradeValue();
    }
}
