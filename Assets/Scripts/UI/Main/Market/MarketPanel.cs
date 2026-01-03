using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MarketPanel : BasePanel
{
    [Header("Action Buttons")]
    [SerializeField] private Transform _marketActionBtnContentTransform = null;

    [Header("Panels")]
    [SerializeField] private MarketResurcePanel _resourcePanel;
    [SerializeField] private MarketTraderPanel _traderPanel;
    
    [Header("Scroll & Prefabs (Injected to Panels)")]
    [SerializeField] private Transform _marketScrollViewContent;
    [SerializeField] private GameObject _marketResourceBtnPrefab;
    [SerializeField] private GameObject _marketTraderBtnPrefab;
    
    private bool _isResourceView = true;

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
        _dataManager.Time.OnDayChanged += HandleDayChanged;
        _dataManager.Market.OnMarketUpdated += HandleMarketUpdated;

        if (!_isResourceView)
        {
            RefreshTraderList();
        }
        else
        {
            ShowResourceView();
        }
    }

    private void SetupActionButtons()
    {
        if (_gameManager.ActionBtnPrefab == null || _marketActionBtnContentTransform == null)
        {
            return;
        }

        GameObjectUtils.ClearChildren(_marketActionBtnContentTransform);

        GameObject resBtnObj = Instantiate(_gameManager.ActionBtnPrefab, _marketActionBtnContentTransform);
        ActionBtn resBtn = resBtnObj.GetComponent<ActionBtn>();
        resBtn?.OnInitialize("Resources", ShowResourceView);

        GameObject traderBtnObj = Instantiate(_gameManager.ActionBtnPrefab, _marketActionBtnContentTransform);
        ActionBtn traderBtn = traderBtnObj.GetComponent<ActionBtn>();
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
        _resourcePanel?.OnInitialize(_dataManager, this);
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
        _traderPanel?.OnInitialize(_gameManager ,_dataManager);
        RefreshTraderList();
    }

    private void RefreshResourceList()
    {
        if (_dataManager == null || _marketScrollViewContent == null || _marketResourceBtnPrefab == null)
        {
            return;
        }

        string currentSelectedId = _resourcePanel?.GetSelectedResourceId() ?? string.Empty;

        GameObjectUtils.ClearChildren(_marketScrollViewContent);

        Dictionary<string, ResourceEntry> resources = _dataManager.Resource.GetAllResources();
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

            if (!string.IsNullOrEmpty(currentSelectedId) && entry.data?.id == currentSelectedId)
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

        ResourceEntry entryToSelect = selectedEntry ?? firstEntry;
        if (entryToSelect != null)
        {
            HandleResourceButtonClicked(entryToSelect);
        }
    }

    private void RefreshTraderList()
    {

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

    private void HandleMarketUpdated()
    {
        if (!_isResourceView)
        {
            RefreshTraderButtons();
        }
    }

    /// <summary>
    /// 기존 트레이더 버튼들의 정보를 업데이트합니다.
    /// </summary>
    private void RefreshTraderButtons()
    {
        if (_marketScrollViewContent == null)
        {
            return;
        }

        int updatedCount = 0;
        // 모든 자식 버튼의 정보 업데이트
        for (int i = 0; i < _marketScrollViewContent.childCount; i++)
        {
            Transform child = _marketScrollViewContent.GetChild(i);
            MarketTraderBtn traderBtn = child.GetComponent<MarketTraderBtn>();
            if (traderBtn != null)
            {
                traderBtn.RefreshIndicator();
                updatedCount++;
            }
        }
    }

    /// <summary>
    /// 리소스 버튼들의 거래 값(매수/매도)을 업데이트합니다.
    /// </summary>
    public void RefreshResourceButtons()
    {
        if (_marketScrollViewContent == null || !_isResourceView)
        {
            return;
        }

        int updatedCount = 0;
        for (int i = 0; i < _marketScrollViewContent.childCount; i++)
        {
            Transform child = _marketScrollViewContent.GetChild(i);
            MarketResourceBtn resourceBtn = child.GetComponent<MarketResourceBtn>();
            if (resourceBtn != null)
            {
                resourceBtn.RefreshTradeValue();
                updatedCount++;
            }
        }
    }
}
