using System.Collections.Generic;
using System.Diagnostics;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MarketResurcePanel : MonoBehaviour
{
    private DataManager _dataManager;
    private MarketPanel _marketPanel;

    [SerializeField] private WindowGraph _windowGraph;

    [SerializeField] private TMP_InputField _resouceTradeInputField;

    [Header("Info Panel")]
    [SerializeField] private Image _resouceImage;
    [SerializeField] private TextMeshProUGUI _resourceNameText;
    [SerializeField] private TextMeshProUGUI _resourceStorageText;
    [SerializeField] private TextMeshProUGUI _resourcePriceText;
    [SerializeField] private Slider _resourceInventorySlider;
    [SerializeField] private TextMeshProUGUI _resourceInventoryText;

    private ResourceEntry _selectedResourceEntry;
    private string _selectedResourceId = string.Empty;

    private const long DEFAULT_INVENTORY_MAX = 10000;

    /// <summary>
    /// 현재 선택된 리소스 ID를 반환합니다.
    /// </summary>
    public string GetSelectedResourceId()
    {
        return _selectedResourceId;
    }

    public void OnInitialize(DataManager dataManager, MarketPanel marketPanel)
    {
        _dataManager = dataManager;
        _marketPanel = marketPanel;

        _windowGraph.ShowGraph(_selectedResourceEntry.resourceState.PriceHistory);
        UpdateSelectionDetails();

        _dataManager.Time.OnDayChanged += HandleDayChanged;

        _windowGraph.OnInitialize();
    }

    private void OnDestroy()
    {
        if(_dataManager != null)
        {
            _dataManager.Time.OnDayChanged -= HandleDayChanged;
        }
    }

    public void HandleResourceButtonClicked(ResourceEntry entry)
    {
        if (entry == null)
        {
            return;
        }

        _selectedResourceEntry = entry;
        _selectedResourceId = entry.resourceData.id;
        _resouceTradeInputField.text = entry.resourceState.playerTransactionDelta.ToString();

        _windowGraph.ShowGraph(_selectedResourceEntry.resourceState.PriceHistory);
        UpdateSelectionDetails();
    }

    public void ResouceTradeInputFieldChanged()
    {
        if (_selectedResourceEntry == null || string.IsNullOrEmpty(_selectedResourceId))
        {
            return;
        }

        if (string.IsNullOrEmpty(_resouceTradeInputField.text))
        {
            _resouceTradeInputField.text = "0";
        }

        if (int.TryParse(_resouceTradeInputField.text, out int tradeAmount))
        {
            _selectedResourceEntry.resourceState.playerTransactionDelta = tradeAmount;
            _marketPanel.RefreshResourceButtons();
        }
    }

    private void HandleDayChanged()
    {
        _windowGraph.ShowGraph(_selectedResourceEntry.resourceState.PriceHistory);
        UpdateSelectionDetails();
    }

    private void UpdateSelectionDetails()
    {
        ResourceData data = _selectedResourceEntry.resourceData;
        ResourceState state = _selectedResourceEntry.resourceState;

        _resouceImage.sprite = data.icon;
        UpdateInventorySlider(state.count, data.initialAmount);

        string priceText = $"{state.currentValue:N0}";
        if (state != null && state.priceChangeRate != 0f)
        {
            priceText += $" ({state.priceChangeRate:+0.##;-0.##;0})";
        }

        string statusText = GetFriendlyStatus(state, out var statusColor);
        string nextStageText = data.nextStage.displayName;

        _resourceNameText.text = name;
        _resourcePriceText.text = priceText;
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
        _resourceInventorySlider.value = Mathf.Clamp(currentInventory, 0f, maxValue);

        // 재고량 텍스트 업데이트
        if (_resourceInventoryText != null)
        {
            _resourceInventoryText.text = $"{currentInventory:N0} / {maxValue:N0}";
        }
    }
}