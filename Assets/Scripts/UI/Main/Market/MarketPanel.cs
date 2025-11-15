using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MarketPanel : BasePanel
{
    [Header("Resouce ScrollView")]
    [SerializeField] private GameObject _marketResourceBtn;
    [SerializeField] private Transform _resouceScrollViewContent;
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

    /// <summary>
    /// initialize market panel
    /// </summary>
    protected override void OnInitialize()
    {
        if (_dataManager == null)
        {
            return;
        }

        SubscribeToDayChange();
        RefreshResourceScrollView();

        _windowGraph?.OnInitialize();
        UpdateGraph();
        UpdateSelectionDetails();
    }

    private void RefreshResourceScrollView()
    {
        if (_dataManager == null || _resouceScrollViewContent == null)
        {
            return;
        }

        GameObjectUtils.ClearChildren(_resouceScrollViewContent);
        _resourceBtns.Clear();

        if (_marketResourceBtn == null)
        {
            Debug.LogWarning("[MainUiManager] MainScrollViewResouceBtn prefab is not assigned.");
            return;
        }

        Dictionary<string, ResourceEntry> resources = _dataManager.GetAllResources();
        if (resources == null || resources.Count == 0)
        {
            return;
        }

        ResourceEntry firstEntry = null;
        bool selectionFound = false;

        foreach (var entry in resources.Values)
        {
            if (entry == null)
            {
                continue;
            }

            if (firstEntry == null)
            {
                firstEntry = entry;
            }

            if (!string.IsNullOrEmpty(_selectedResourceId) &&
                entry.resourceData != null &&
                entry.resourceData.id == _selectedResourceId)
            {
                _selectedResourceEntry = entry;
                selectionFound = true;
            }

            GameObject btnObj = Instantiate(_marketResourceBtn, _resouceScrollViewContent);
            MarketResourceBtn resourceBtn = btnObj.GetComponent<MarketResourceBtn>();
            if (resourceBtn != null)
            {
                resourceBtn.OnInitialize(this, entry);
                _resourceBtns.Add(resourceBtn);
            }
        }

        if (!selectionFound)
        {
            _selectedResourceEntry = firstEntry;
            _selectedResourceId = firstEntry?.resourceData?.id ?? string.Empty;
        }

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
        RefreshResourceScrollView();
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
