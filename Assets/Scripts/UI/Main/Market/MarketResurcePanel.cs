using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class MarketResurcePanel : MonoBehaviour
{
    private GameDataManager _dataManager;

    [SerializeField] private WindowGraph _windowGraph;

    [Header("Info Panel")]
    [SerializeField] private TextMeshProUGUI _resourceNameText;
    [SerializeField] private TextMeshProUGUI _resourcePriceText;
    [SerializeField] private TextMeshProUGUI _resourceStatusText;
    [SerializeField] private TextMeshProUGUI _resourceNextStageText;

    private readonly List<MarketResourceBtn> _resourceBtns = new List<MarketResourceBtn>();
    private bool _isSubscribedToDayChange;
    private ResourceEntry _selectedResourceEntry;
    private string _selectedResourceId = string.Empty;

    public void OnInitialize(GameDataManager dataManager)
    {
        _dataManager = dataManager;

        SubscribeToDayChange();
        _windowGraph?.OnInitialize();
        UpdateGraph();
        UpdateSelectionDetails();
    }

    public void HandleResourceButtonClicked(ResourceEntry entry)
    {
        if (entry == null)
        {
            return;
        }

        _selectedResourceEntry = entry;
        _selectedResourceId = entry.resourceData?.id ?? string.Empty;
        UpdateGraph();
        UpdateSelectionDetails();
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
