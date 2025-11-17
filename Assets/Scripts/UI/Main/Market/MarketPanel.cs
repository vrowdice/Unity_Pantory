using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MarketPanel : BasePanel
{
    [Header("Action Buttons")]
    [SerializeField] private GameObject _actionBtnPrefab = null;
    [SerializeField] private Transform _marketActionBtnContentTransform = null;

    [Header("Panels")]
    [SerializeField] private MarketResurcePanel _resourcePanel;
    [SerializeField] private MarketTraderPanel _traderPanel;
    
    [Header("Scroll & Prefabs (Injected to Panels)")]
    [SerializeField] private Transform _marketScrollViewContent;
    [SerializeField] private GameObject _marketResourceBtnPrefab;
    [SerializeField] private GameObject _marketTraderBtnPrefab;
    
    private bool _isResourceView = true;
    private bool _isSubscribedToDayChange;

    /// <summary>
    /// initialize market panel
    /// </summary>
    protected override void OnInitialize()
    {
        if (_dataManager == null)
        {
            return;
        }

        SetupActionButtons();
        SubscribeToDayChange();
        ShowResourceView(); // 기본 진입은 리소스 패널
    }

    private void SetupActionButtons()
    {
        if (_actionBtnPrefab == null || _marketActionBtnContentTransform == null)
        {
            return;
        }

        GameObjectUtils.ClearChildren(_marketActionBtnContentTransform);

        // Resource View Button
        var resBtnObj = Instantiate(_actionBtnPrefab, _marketActionBtnContentTransform);
        var resBtn = resBtnObj.GetComponent<ActionBtn>();
        resBtn?.OnInitialize("Resources", ShowResourceView);

        // Trader View Button
        var traderBtnObj = Instantiate(_actionBtnPrefab, _marketActionBtnContentTransform);
        var traderBtn = traderBtnObj.GetComponent<ActionBtn>();
        traderBtn?.OnInitialize("Traders", ShowTraderView);
    }

    public void HandleResourceButtonClicked(ResourceEntry entry)
    {
        _resourcePanel?.HandleResourceButtonClicked(entry);
    }

    private void ShowResourceView()
    {
        _isResourceView = true;
        if (_resourcePanel != null && _traderPanel != null)
        {
            _resourcePanel.gameObject.SetActive(true);
            _traderPanel.gameObject.SetActive(false);
        }
        _resourcePanel?.OnInitialize(_dataManager);
        RefreshResourceList();
    }

    private void ShowTraderView()
    {
        _isResourceView = false;
        if (_resourcePanel != null && _traderPanel != null)
        {
            _resourcePanel.gameObject.SetActive(false);
            _traderPanel.gameObject.SetActive(true);
        }
        _traderPanel?.OnInitialize(_dataManager);
        RefreshTraderList();
    }

    private void RefreshResourceList()
    {
        if (_dataManager == null || _marketScrollViewContent == null || _marketResourceBtnPrefab == null)
        {
            return;
        }

        // 현재 선택된 리소스 ID 저장
        string currentSelectedId = _resourcePanel?.GetSelectedResourceId() ?? string.Empty;

        GameObjectUtils.ClearChildren(_marketScrollViewContent);

        Dictionary<string, ResourceEntry> resources = _dataManager.GetAllResources();
        if (resources == null || resources.Count == 0)
        {
            return;
        }

        ResourceEntry firstEntry = null;
        ResourceEntry selectedEntry = null;

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

            // 현재 선택된 리소스 찾기
            if (!string.IsNullOrEmpty(currentSelectedId) && entry.resourceData?.id == currentSelectedId)
            {
                selectedEntry = entry;
            }

            GameObject btnObj = Instantiate(_marketResourceBtnPrefab, _marketScrollViewContent);
            MarketResourceBtn resourceBtn = btnObj.GetComponent<MarketResourceBtn>();
            if (resourceBtn != null)
            {
                resourceBtn.OnInitialize(this, entry);
            }
        }

        // 현재 선택된 리소스가 있으면 유지, 없으면 첫 번째 선택
        ResourceEntry entryToSelect = selectedEntry ?? firstEntry;
        if (entryToSelect != null)
        {
            HandleResourceButtonClicked(entryToSelect);
        }
    }

    private void RefreshTraderList()
    {
        if (_dataManager == null || _marketScrollViewContent == null || _marketTraderBtnPrefab == null)
        {
            return;
        }

        GameObjectUtils.ClearChildren(_marketScrollViewContent);

        // 플레이어 버튼 추가 (맨 위에 표시)
        AddPlayerButton();

        var market = _dataManager.Market;
        if (market == null)
        {
            return;
        }

        Dictionary<string, MarketActorEntry> actors = market.GetAllActors();
        if (actors == null || actors.Count == 0)
        {
            return;
        }

        foreach (var actor in actors.Values)
        {
            if (actor?.data == null)
            {
                continue;
            }

            GameObject btnObj = Instantiate(_marketTraderBtnPrefab, _marketScrollViewContent);
            var traderBtn = btnObj.GetComponent<MarketTraderBtn>();
            if (traderBtn != null)
            {
                traderBtn.OnInitialize(_traderPanel, actor);
            }
        }
    }

    private void AddPlayerButton()
    {
        if (_dataManager == null || _marketScrollViewContent == null || _marketTraderBtnPrefab == null)
        {
            return;
        }

        GameObject btnObj = Instantiate(_marketTraderBtnPrefab, _marketScrollViewContent);
        var traderBtn = btnObj.GetComponent<MarketTraderBtn>();
        if (traderBtn != null)
        {
            traderBtn.OnInitializePlayer(_traderPanel, _dataManager);
        }
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
        if (_isResourceView)
        {
            RefreshResourceList();
        }
        else
        {
            RefreshTraderList();
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
