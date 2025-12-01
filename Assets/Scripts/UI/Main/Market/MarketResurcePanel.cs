using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class MarketResurcePanel : MonoBehaviour
{
    private GameDataManager _dataManager;
    private MarketPanel _marketPanel;

    [SerializeField] private WindowGraph _windowGraph;

    [SerializeField] private TMP_InputField _resouceTradeInputField;

    [Header("Info Panel")]
    [SerializeField] private Image _resouceImage;
    [SerializeField] private TextMeshProUGUI _resourceNameText;
    [SerializeField] private TextMeshProUGUI _resourcePriceText;
    [SerializeField] private Slider _resourceInventorySlider;
    [SerializeField] private TextMeshProUGUI _resourceInventoryText;

    private readonly List<MarketResourceBtn> _resourceBtns = new List<MarketResourceBtn>();
    private bool _isSubscribedToDayChange;
    private ResourceEntry _selectedResourceEntry;
    private string _selectedResourceId = string.Empty;
    
    // 시장 재고량 슬라이더 기준값 (초기 재고량 또는 평균 재고량)
    private const long DEFAULT_INVENTORY_MAX = 10000; // 기본 최대값

    /// <summary>
    /// 현재 선택된 리소스 ID를 반환합니다.
    /// </summary>
    public string GetSelectedResourceId()
    {
        return _selectedResourceId;
    }

    public void OnInitialize(GameDataManager dataManager, MarketPanel marketPanel = null)
    {
        _dataManager = dataManager;
        _marketPanel = marketPanel;

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
            
            // 리소스 버튼들의 거래 값 업데이트
            _marketPanel?.RefreshResourceButtons();
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
            UpdateResourceImage(null);
            UpdateInventorySlider(0, 0);
            return;
        }

        var data = _selectedResourceEntry.resourceData;
        var state = _selectedResourceEntry.resourceState;

        // 자원 이미지 업데이트
        UpdateResourceImage(data.icon);

        // 시장 재고량 업데이트
        long marketInventory = state != null ? state.count : 0;
        UpdateInventorySlider(marketInventory, data.initialAmount);

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
    }

    /// <summary>
    /// 자원 이미지를 업데이트합니다.
    /// </summary>
    private void UpdateResourceImage(Sprite icon)
    {
        if (_resouceImage != null)
        {
            _resouceImage.sprite = icon;
            _resouceImage.enabled = icon != null;
        }
    }

    /// <summary>
    /// 시장 재고량 슬라이더를 업데이트합니다.
    /// </summary>
    /// <param name="currentInventory">현재 시장 재고량</param>
    /// <param name="initialAmount">초기 재고량 (기준값으로 사용)</param>
    private void UpdateInventorySlider(long currentInventory, long initialAmount)
    {
        if (_resourceInventorySlider == null)
        {
            return;
        }

        // 슬라이더 최대값 설정 (초기 재고량의 2배 또는 기본값 중 큰 값)
        long maxValue = (initialAmount * 2 > DEFAULT_INVENTORY_MAX) ? (initialAmount * 2) : DEFAULT_INVENTORY_MAX;
        _resourceInventorySlider.maxValue = maxValue;
        _resourceInventorySlider.value = Mathf.Clamp((float)currentInventory, 0f, (float)maxValue);

        // 재고량 텍스트 업데이트
        if (_resourceInventoryText != null)
        {
            _resourceInventoryText.text = $"{currentInventory:N0} / {maxValue:N0}";
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