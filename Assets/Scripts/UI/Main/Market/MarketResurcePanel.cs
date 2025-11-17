using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class MarketResurcePanel : MonoBehaviour
{
    private GameDataManager _dataManager;

    [SerializeField] private WindowGraph _windowGraph;

    [SerializeField] private TMP_InputField _resouceTradeInputField;

    [Header("Info Panel")]
    [SerializeField] private TextMeshProUGUI _resourceNameText;
    [SerializeField] private TextMeshProUGUI _resourcePriceText;
    [SerializeField] private TextMeshProUGUI _resourceStatusText;
    [SerializeField] private TextMeshProUGUI _resourceNextStageText;

    private readonly List<MarketResourceBtn> _resourceBtns = new List<MarketResourceBtn>();
    private bool _isSubscribedToDayChange;
    private ResourceEntry _selectedResourceEntry;
    private string _selectedResourceId = string.Empty;

    /// <summary>
    /// 현재 선택된 리소스 ID를 반환합니다.
    /// </summary>
    public string GetSelectedResourceId()
    {
        return _selectedResourceId;
    }

    public void OnInitialize(GameDataManager dataManager)
    {
        _dataManager = dataManager;

        SubscribeToDayChange();
        UpdateGraph();
        UpdateSelectionDetails();

        _windowGraph?.OnInitialize();
    }

    public void HandleResourceButtonClicked(ResourceEntry entry)
    {
        if (entry == null)
        {
            return;
        }

        _selectedResourceEntry = entry;
        _selectedResourceId = entry.resourceData?.id ?? string.Empty;
        
        // InputField에 현재 playerTransactionDelta 값 표시
        if (_resouceTradeInputField != null && entry.resourceState != null)
        {
            _resouceTradeInputField.text = entry.resourceState.playerTransactionDelta.ToString();
        }
        
        UpdateGraph();
        UpdateSelectionDetails();
    }

    public void ResouceTradeInputFieldChanged()
    {
        if (_selectedResourceEntry == null || string.IsNullOrEmpty(_selectedResourceId))
        {
            return;
        }

        // InputField 값을 playerTransactionDelta에 설정 (양수: 매수, 음수: 매도, 0: 거래 안함)
        string inputText = _resouceTradeInputField.text;
        if (string.IsNullOrEmpty(inputText))
        {
            inputText = "0";
        }

        if (int.TryParse(inputText, out int tradeAmount))
        {
            _selectedResourceEntry.resourceState.playerTransactionDelta = tradeAmount;
        }
    }

    public void ChangeResouceTradeInputFieldValue(int argValue)
    {
        _resouceTradeInputField.text = (int.Parse(_resouceTradeInputField.text) + argValue).ToString();
        ResouceTradeInputFieldChanged();
    }

    private void SubscribeToDayChange()
    {
        if (_isSubscribedToDayChange)
        {
            return;
        }

        if (_dataManager?.Time == null)
        {
            return;
        }

        _dataManager.Time.OnDayChanged += HandleDayChanged;
        _isSubscribedToDayChange = true;
    }

    private void UnsubscribeFromDayChange()
    {
        if (!_isSubscribedToDayChange)
        {
            return;
        }

        if (_dataManager?.Time == null)
        {
            _isSubscribedToDayChange = false;
            return;
        }

        _dataManager.Time.OnDayChanged -= HandleDayChanged;
        _isSubscribedToDayChange = false;
    }

    private void HandleDayChanged()
    {
        // 리스트는 MarketPanel에서 관리. 여기서는 상세 UI만 갱신.
        UpdateGraph();
        UpdateSelectionDetails();
    }

    private void UpdateGraph()
    {
        if (_windowGraph == null)
        {
            return;
        }

        _windowGraph.ShowGraph(_selectedResourceEntry?.resourceState?.PriceHistory);
    }

    private void UpdateSelectionDetails()
    {
        if (_selectedResourceEntry == null || _selectedResourceEntry.resourceData == null)
        {
            SetInfoTexts("-", "-", "-", "-", Color.white);
            return;
        }

        var data = _selectedResourceEntry.resourceData;
        var state = _selectedResourceEntry.resourceState;

        string priceText = state != null
            ? $"{state.currentValue:N0}"
            : "N/A";

        if (state != null && state.priceChangeRate != 0f)
        {
            priceText += $" ({state.priceChangeRate:+0.##;-0.##;0})";
        }

        string statusText = GetFriendlyStatus(state, out var statusColor);
        string nextStageText = data.nextStage != null
            ? data.nextStage.displayName
            : "Final Product";

        SetInfoTexts(
            data.displayName,
            priceText,
            statusText,
            nextStageText,
            statusColor);
    }

    private string GetFriendlyStatus(ResourceState state, out Color color)
    {
        color = Color.white;

        if (state == null)
        {
            return "No Data";
        }

        string status = "Stable";
        float delta = state.priceChangeRate;
        if (delta > 0.02f)
        {
            status = "Uptrend";
            color = Color.green;
        }
        else if (delta < -0.02f)
        {
            status = "Downtrend";
            color = Color.red;
        }

        float supply = state.lastSupply;
        float demand = state.lastDemand;
        if (supply > 0f || demand > 0f)
        {
            float ratio = demand - supply;
            if (ratio > 0.1f * Mathf.Max(1f, supply))
            {
                status = "Demand Crunch";
                color = new Color(1f, 0.5f, 0f); // orange
            }
            else if (-ratio > 0.1f * Mathf.Max(1f, demand))
            {
                status = "Oversupply";
                color = new Color(0.4f, 0.6f, 1f); // light blue
            }
        }

        return status;
    }

    private void SetInfoTexts(string name, string price, string status, string nextStage, Color statusColor)
    {
        if (_resourceNameText != null)
        {
            _resourceNameText.text = name;
        }

        if (_resourcePriceText != null)
        {
            _resourcePriceText.text = price;
        }

        if (_resourceStatusText != null)
        {
            _resourceStatusText.text = status;
            _resourceStatusText.color = statusColor;
        }

        if (_resourceNextStageText != null)
        {
            _resourceNextStageText.text = nextStage;
        }
    }

    private void OnDisable()
    {
        if (!gameObject.activeInHierarchy)
        {
            UnsubscribeFromDayChange();
        }
    }

    private void OnDestroy()
    {
        UnsubscribeFromDayChange();
    }
}
