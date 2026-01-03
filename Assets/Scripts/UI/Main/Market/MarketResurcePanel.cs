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
    [SerializeField] private TextMeshProUGUI _resourceInventoryText;

    private ResourceEntry _selectedResourceEntry;
    private string _selectedResourceId = string.Empty;

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

        _dataManager.Time.OnDayChanged -= HandleDayChanged;
        _dataManager.Time.OnDayChanged += HandleDayChanged;

        if(_selectedResourceEntry == null)
        {
            _selectedResourceEntry = _dataManager.Resource.GetResourceEntry("iron_ore");
        }

        UpdateSelectionDetails();
        _windowGraph.OnInitialize();
        _windowGraph.ShowGraph(_selectedResourceEntry.state.PriceHistory);
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
        _selectedResourceId = entry.data.id;
        _resouceTradeInputField.text = entry.state.threadDeltaCount.ToString();

        _windowGraph.ShowGraph(_selectedResourceEntry.state.PriceHistory);
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
            _selectedResourceEntry.state.threadDeltaCount = tradeAmount;
            _marketPanel.RefreshResourceButtons();
        }
    }

    private void HandleDayChanged()
    {
        _windowGraph.ShowGraph(_selectedResourceEntry.state.PriceHistory);
        UpdateSelectionDetails();
    }

    private void UpdateSelectionDetails()
    {
        ResourceData data = _selectedResourceEntry.data;
        ResourceState state = _selectedResourceEntry.state;

        _resouceImage.sprite = data.icon;

        string priceText = $"{state.value:N0}";
        if (state != null && state.currentChangeValue != 0f)
        {
            priceText += $" ({state.currentChangeValue:+0.##;-0.##;0})";
        }

        string statusText = GetFriendlyStatus(state, out var statusColor);

        _resourceNameText.text = data.displayName;
        _resourceStorageText.text = state.count.ToString();
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
        float delta = state.currentChangeValue;
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

        return status;
    }
}