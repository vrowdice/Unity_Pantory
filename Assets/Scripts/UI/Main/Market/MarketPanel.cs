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
    private bool _isSubscribedToDayChange;
    private bool _isSubscribedToMarketUpdate;

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
        SubscribeToMarketUpdate();
        
        // 기본 진입은 리소스 패널
        if (!_isResourceView)
        {
            // 트레이더 뷰가 이미 설정되어 있으면 그대로 유지
            RefreshTraderList();
        }
        else
        {
            // 리소스 뷰로 시작
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

        // Resource View Button
        var resBtnObj = Instantiate(_gameManager.ActionBtnPrefab, _marketActionBtnContentTransform);
        var resBtn = resBtnObj.GetComponent<ActionBtn>();
        resBtn?.OnInitialize("Resources", ShowResourceView);

        // Trader View Button
        var traderBtnObj = Instantiate(_gameManager.ActionBtnPrefab, _marketActionBtnContentTransform);
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

        // 현재 선택된 리소스 ID 저장
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
            Debug.LogWarning("[MarketPanel] Cannot refresh trader list: missing required components.");
            return;
        }

        GameObjectUtils.ClearChildren(_marketScrollViewContent);

        var market = _dataManager.Market;
        if (market == null)
        {
            Debug.LogWarning("[MarketPanel] Market handler is null.");
            return;
        }

        // 경제력 기준으로 정렬된 트레이더 목록 가져오기
        List<MarketActorEntry> sortedActors = market.GetActorsSortedByWealth(false);
        if (sortedActors == null)
        {
            sortedActors = new List<MarketActorEntry>();
        }

        // 플레이어 자산 계산 (FinancesDataHandler에서 가져오기)
        long playerWealth = _dataManager.Finances.GetCredit();

        // 플레이어를 포함한 정렬된 리스트 생성
        // 각 항목은 (isPlayer, wealth, entry, rank) 튜플로 저장
        var sortedItems = new List<(bool isPlayer, float wealth, MarketActorEntry entry, int rank)>();

        // NPC 액터 추가 (MarketActorState.rank 사용 - 이미 자산 기준으로 계산됨)
        foreach (var actor in sortedActors)
        {
            if (actor?.data == null)
            {
                continue;
            }

            float wealth = actor.state?.GetWealth() ?? 0f;
            int rank = actor.state?.GetRank() ?? 0; // MarketDataHandler에서 계산된 자산 기준 순위 사용
            sortedItems.Add((false, wealth, actor, rank));
        }

        // 플레이어 추가 (플레이어 자산을 기준으로 순위 계산)
        // 플레이어보다 자산이 많은 NPC 수를 세어서 순위 결정
        int playerRank = 1;
        foreach (var actor in sortedActors)
        {
            if (actor?.state != null && actor.state.GetWealth() > playerWealth)
            {
                playerRank++;
            }
        }
        sortedItems.Add((true, playerWealth, null, playerRank));

        // 자산 기준으로 내림차순 정렬 (표시 순서용, 순위는 이미 계산됨)
        sortedItems.Sort((a, b) => b.wealth.CompareTo(a.wealth));

        // 정렬된 순서대로 버튼 생성
        int createdCount = 0;
        foreach (var item in sortedItems)
        {
            GameObject btnObj = Instantiate(_marketTraderBtnPrefab, _marketScrollViewContent);
            if (btnObj == null)
            {
                Debug.LogError("[MarketPanel] Failed to instantiate trader button prefab.");
                continue;
            }

            var traderBtn = btnObj.GetComponent<MarketTraderBtn>();
            if (traderBtn == null)
            {
                Debug.LogError("[MarketPanel] MarketTraderBtn component not found on instantiated button.");
                continue;
            }

            if (item.isPlayer)
            {
                // 플레이어 버튼 초기화 (계산된 순위 전달)
                traderBtn.OnInitializePlayer(_traderPanel, _dataManager, item.rank);
            }
            else
            {
                // NPC 액터 버튼 초기화 (MarketActorState.rank 사용)
                traderBtn.OnInitialize(_traderPanel, item.entry);
            }
            createdCount++;
        }

        if (createdCount > 0)
        {
            Debug.Log($"[MarketPanel] Refreshed trader list: {createdCount} traders created (including player).");
        }
        else
        {
            Debug.LogWarning("[MarketPanel] No trader buttons were created.");
        }
    }

    private void SubscribeToDayChange()
    {
        // 중복 구독 방지
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

    private void SubscribeToMarketUpdate()
    {
        // 중복 구독 방지
        if (_isSubscribedToMarketUpdate)
        {
            return;
        }

        var market = _dataManager?.Market;
        if (market == null)
        {
            return;
        }

        market.OnMarketUpdated += HandleMarketUpdated;
        _isSubscribedToMarketUpdate = true;
    }

    private void UnsubscribeFromMarketUpdate()
    {
        if (!_isSubscribedToMarketUpdate)
        {
            return;
        }

        var market = _dataManager?.Market;
        if (market == null)
        {
            _isSubscribedToMarketUpdate = false;
            return;
        }

        market.OnMarketUpdated -= HandleMarketUpdated;
        _isSubscribedToMarketUpdate = false;
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
        // 시장이 업데이트되면 트레이더 뷰일 경우 목록 새로고침
        if (!_isResourceView)
        {
            // 기존 버튼들의 정보만 업데이트 (목록 재생성하지 않음)
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

        if (updatedCount > 0)
        {
            Debug.Log($"[MarketPanel] Updated {updatedCount} trader buttons.");
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
        // 모든 자식 버튼의 거래 값 업데이트
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

    private void OnDisable()
    {
        if (!gameObject.activeInHierarchy)
        {
            UnsubscribeFromDayChange();
            UnsubscribeFromMarketUpdate();
        }
    }

    private void OnDestroy()
    {
        UnsubscribeFromDayChange();
        UnsubscribeFromMarketUpdate();
    }
}
