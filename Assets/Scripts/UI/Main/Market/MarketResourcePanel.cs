using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Evo.UI;

/// <summary>
/// 시장의 리소스 상세 정보 및 거래 입력을 담당하는 패널 클래스입니다.
/// </summary>
public class MarketResourcePanel : MonoBehaviour
{
    private DataManager _dataManager;
    private MarketCanvas _marketPanel;

    [Header("Components")]
    [SerializeField] private LineChart _lineChart;
    [SerializeField] private TMP_InputField _resouceTradeInputField;

    [Header("Info Panel UI")]
    [SerializeField] private Image _resouceImage;
    [SerializeField] private TextMeshProUGUI _resourceNameText;
    [SerializeField] private TextMeshProUGUI _resourceStorageText;
    [SerializeField] private TextMeshProUGUI _resourcePriceText;
    [SerializeField] private TextMeshProUGUI _resourceBuyPriceText;
    [SerializeField] private TextMeshProUGUI _resourceSellPriceText;

    private ResourceEntry _selectedResourceEntry;
    private string _selectedResourceId = string.Empty;
    public string GetSelectedResourceId() => _selectedResourceId;

    /// <summary>
    /// 패널을 초기화하고 날짜 변경 이벤트를 구독합니다.
    /// </summary>
    /// <param name="dataManager">데이터 관리자 참조</param>
    /// <param name="marketPanel">부모 마켓 패널 참조</param>
    public void Init(MarketCanvas marketPanel)
    {
        _dataManager = DataManager.Instance;
        _marketPanel = marketPanel;

        if (_selectedResourceEntry == null)
        {
            _selectedResourceEntry = _dataManager.Resource.GetResourceEntry("iron_ore");
            ChangeResource(_selectedResourceEntry);
        }
        else
        {
            RefreshUI();
        }
    }


    /// <summary>
    /// 리소스 목록에서 버튼 클릭 시 호출되어 상세 정보를 갱신합니다.
    /// </summary>
    /// <param name="entry">선택된 리소스 엔트리</param>
    public void ChangeResource(ResourceEntry entry)
    {
        if (entry == null) return;

        _selectedResourceEntry = entry;
        _selectedResourceId = entry.data.id;

        _resouceTradeInputField.text = entry.state.marketDeltaCount.ToString();

        RefreshUI();
    }

    /// <summary>
    /// 거래량 입력 필드의 값이 변경될 때 호출됩니다. (UI Event)
    /// </summary>
    public void ChangeResouceTradeInput()
    {
        if (_selectedResourceEntry == null) return;

        string inputText = _resouceTradeInputField.text;

        if (int.TryParse(inputText, out int tradeAmount))
        {
            _selectedResourceEntry.state.marketDeltaCount = tradeAmount;
            _marketPanel.RefreshButtons();
        }
    }

    public void ChangeResourceTradeInputFieldValue(int value)
    {
        _resouceTradeInputField.text = (int.Parse(_resouceTradeInputField.text) + value).ToString();
        ChangeResouceTradeInput();
    }

    /// <summary>
    /// 날짜가 변경되었을 때 그래프와 상세 정보를 새로고침합니다.
    /// </summary>
    public void HandleDayChanged()
    {
        RefreshUI();
    }

    private void RefreshUI()
    {
        if (_selectedResourceEntry == null) return;

        ResourceData data = _selectedResourceEntry.data;
        ResourceState state = _selectedResourceEntry.state;

        _resouceImage.sprite = data.icon;
        _resourceNameText.text = data.id.Localize(LocalizationUtils.TABLE_RESOURCE);
        _resourceStorageText.text = state.count.ToString("N0");

        string priceText = $"{state.currentValue:N0}";
        if (state.currentChangeValue != 0f)
        {
            string deltaSymbol = state.currentChangeValue > 0 ? "+" : "";
            priceText += $" ({deltaSymbol}{state.currentChangeValue:N0})";
        }
        _resourcePriceText.text = priceText;
        _resourcePriceText.color = VisualManager.Instance.GetDeltaColor(state.currentChangeValue);

        long purchasePrice = _dataManager.Resource.GetPurchasePrice(_selectedResourceId);
        long salePrice = _dataManager.Resource.GetSalePrice(_selectedResourceId);

        _resourceBuyPriceText.text = $"{purchasePrice:N0}";
        _resourceSellPriceText.text = $"{salePrice:N0}";

        RefreshLineChart(state._priceHistory);
        _resouceTradeInputField.text = state.marketDeltaCount.ToString();
    }

    /// <summary>
    /// 가격 히스토리를 LineChart에 반영합니다.
    /// </summary>
    private void RefreshLineChart(List<float> priceHistory)
    {
        if (_lineChart == null || priceHistory == null) return;

        int count = priceHistory.Count;
        int labelStep = count <= 12 ? 1 : Mathf.Max(1, count / 10);
        _lineChart.dataPoints.Clear();

        for (int i = 0; i < count; i++)
        {
            bool showLabel = (i % labelStep == 0) || (i == count - 1);
            string xLabel = showLabel ? (i + 1).ToString() : "";
            _lineChart.dataPoints.Add(new LineChart.DataPoint(xLabel, priceHistory[i]));
        }
        if (_lineChart.dataPoints.Count == 1)
        {
            float v = _lineChart.dataPoints[0].value;
            _lineChart.dataPoints.Add(new LineChart.DataPoint("2", v));
        }

        _lineChart.DrawChart();
    }
}