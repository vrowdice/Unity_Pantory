using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 시장 리스트 내 개별 리소스 항목을 관리하고 UI를 갱신하는 컴포넌트입니다.
/// </summary>
public class MarketResourceBtn : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private Image _image = null;
    [SerializeField] private TextMeshProUGUI _nameText = null;
    [SerializeField] private TextMeshProUGUI _deltaText = null;
    [SerializeField] private TextMeshProUGUI _tradeValueText = null;

    private MarketPanel _marketPanel = null;
    private ResourceEntry _resourceEntry = null;

    /// <summary>
    /// 버튼을 초기화하고 데이터를 연결합니다.
    /// </summary>
    /// <param name="argMarketPanel">부모 마켓 패널 참조</param>
    /// <param name="resourceEntry">표시할 리소스 엔트리 데이터</param>
    public void Init(MarketPanel argMarketPanel, ResourceEntry resourceEntry)
    {
        _marketPanel = argMarketPanel;
        _resourceEntry = resourceEntry;

        _image.sprite = resourceEntry.data.icon;
        _nameText.text = resourceEntry.data.displayName;

        RefreshAllUI();
    }

    /// <summary>
    /// 가격 정보와 거래 수량 정보를 포함한 모든 UI를 최신화합니다.
    /// </summary>
    public void RefreshAllUI()
    {
        UpdateChangeValue();
        UpdateTradeValue();
    }

    /// <summary>
    /// 버튼 클릭 시 호출되어 상세 정보 패널을 갱신합니다.
    /// </summary>
    public void OnClick()
    {
        _marketPanel.HandleResourceButtonClicked(_resourceEntry);
    }

    /// <summary>
    /// 현재 가격 및 가격 변동치를 UI에 표시합니다.
    /// </summary>
    private void UpdateChangeValue()
    {
        ResourceState resourceState = _resourceEntry.state;

        float delta = resourceState.currentChangeValue;
        string deltaSymbol = delta > 0 ? "+" : "";
        string deltaText = $"{deltaSymbol}{delta:F2}";

        _deltaText.text = $"{resourceState.currentValue.ToString()} ({deltaText})";
        _deltaText.color = VisualManager.Instance.GetDeltaColor(delta);
    }

    /// <summary>
    /// 플레이어가 입력한 매수/매도 수량을 UI에 표시합니다.
    /// </summary>
    private void UpdateTradeValue()
    {
        ResourceState resourceState = _resourceEntry.state;
        long tradeDelta = resourceState.threadDeltaCount;

        if (tradeDelta > 0)
        {
            _tradeValueText.text = $"+{tradeDelta:N0}";
            _tradeValueText.color = Color.green;
        }
        else if (tradeDelta < 0)
        {
            _tradeValueText.text = tradeDelta.ToString("N0");
            _tradeValueText.color = Color.red;
        }
        else
        {
            _tradeValueText.text = "0";
            _tradeValueText.color = Color.black;
        }
    }

    /// <summary>
    /// 외부에서 거래 수량 UI만 업데이트가 필요할 때 호출합니다.
    /// </summary>
    public void RefreshTradeValue()
    {
        UpdateTradeValue();
    }
}