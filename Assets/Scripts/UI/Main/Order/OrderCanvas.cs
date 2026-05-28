using UnityEngine;
using Evo.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 주문 관리 패널
/// </summary>
public class OrderCanvas : MainCanvasPanelBase
{
    [SerializeField] private Transform _orderActionBtnContentTransform;
    [SerializeField] private Transform _orderMarketActorPopupBtnScrollViewContentTransform;
    [SerializeField] private Transform _orderBtnScrollViewContentTransform;
    [SerializeField] private Switch _acceptedOrderSwitch;

    [SerializeField] private GameObject _orderMarketActorPopupBtnPrefab;
    [SerializeField] private GameObject _orderBtnPrefab;

    private MarketActorType? _currentMarketActorType = null;
    private bool _showAcceptedOrders = false;

    private List<ActionBtn> _filterButtonList = new();
    private List<MarketActorPopupBtn> _marketActorPopupBtnList = new();
    private Dictionary<OrderState, OrderBtn> _orderButtonMap = new();
    private Coroutine _filterButtonCoroutine;
    private Coroutine _marketActorCoroutine;
    private Coroutine _orderButtonCoroutine;

    public override void Init(MainCanvas argUIManager)
    {
        base.Init(argUIManager);

        _dataManager.Order.OnOrderChanged -= HandleOrderUpdated;
        _dataManager.Order.OnOrderChanged += HandleOrderUpdated;

        _dataManager.Time.OnDayChanged -= HandleDayChanged;
        _dataManager.Time.OnDayChanged += HandleDayChanged;

        _dataManager.Resource.OnResourceChanged -= HandleResourceChanged;
        _dataManager.Resource.OnResourceChanged += HandleResourceChanged;

        InitializeFilterButtons();
        RefreshMarketActorButtons();
        RefreshOrderButtons();

        _acceptedOrderSwitch.onValueChanged.RemoveListener(OnAcceptedOrderSwitchChanged);
        _acceptedOrderSwitch.onValueChanged.AddListener(OnAcceptedOrderSwitchChanged);
    }

    public void UpdataUI()
    {
        foreach (MarketActorPopupBtn btn in _marketActorPopupBtnList) btn.UpdateUI();
        foreach (OrderBtn btn in _orderButtonMap.Values) btn.UpdateUI();
    }

    private void InitializeFilterButtons()
    {
        int targetCount = EnumUtils.GetAllEnumValues<MarketActorType>().Count + 1;
        if (_orderActionBtnContentTransform.childCount == targetCount)
        {
            _filterButtonList.Clear();
            foreach (Transform child in _orderActionBtnContentTransform)
            {
                ActionBtn btn = child.GetComponent<ActionBtn>();
                if (btn != null)
                {
                    _filterButtonList.Add(btn);
                }
            }
            UpdateFilterHighlight();
            return;
        }

        _gameManager.PoolingManager.ClearChildrenToPool(_orderActionBtnContentTransform);
        _filterButtonList.Clear();

        List<(string label, System.Action onClick)> filterDefs = new List<(string label, System.Action onClick)>
        {
            (LocalizationUtils.Localize("All"), () =>
            {
                OnMarketActorTypeClick(null);
                UpdateFilterHighlight();
            })
        };

        foreach (MarketActorType actorType in EnumUtils.GetAllEnumValues<MarketActorType>())
        {
            MarketActorType capturedType = actorType;
            filterDefs.Add((actorType.Localize(LocalizationUtils.TABLE_MARKET_ACTOR), () =>
            {
                OnMarketActorTypeClick(capturedType);
                UpdateFilterHighlight();
            }));
        }

        StaggeredSpawnUtils.Restart(this, ref _filterButtonCoroutine, CreateFilterButtonsRoutine(filterDefs));
        UpdateFilterHighlight();
    }

    private IEnumerator CreateFilterButtonsRoutine(List<(string label, System.Action onClick)> filterDefs)
    {
        yield return StaggeredSpawnUtils.ForEachFrame(filterDefs.Count, i =>
        {
            (string label, System.Action onClick) def = filterDefs[i];
            GameObject btnObj = _gameManager.PoolingManager.GetPooledObject(_panelUIManager.ActionBtnPrefab);
            btnObj.transform.SetParent(_orderActionBtnContentTransform, false);
            ActionBtn btn = btnObj.GetComponent<ActionBtn>();
            btn.Init(def.label, def.onClick);
            _filterButtonList.Add(btn);
        });
    }

    private void UpdateFilterHighlight()
    {
        if (_filterButtonList.Count == 0) return;

        _filterButtonList[0].SetHighlight(_currentMarketActorType == null);

        List<MarketActorType> types = EnumUtils.GetAllEnumValues<MarketActorType>();
        for (int i = 0; i < types.Count && i + 1 < _filterButtonList.Count; i++)
        {
            _filterButtonList[i + 1].SetHighlight(_currentMarketActorType == types[i]);
        }
    }

    private void OnMarketActorTypeClick(MarketActorType? actorType)
    {
        _currentMarketActorType = actorType;
        RefreshMarketActorButtons();
        RefreshOrderButtons();
    }

    private void OnAcceptedOrderSwitchChanged(bool isOn)
    {
        _showAcceptedOrders = isOn;
        RefreshOrderButtons();
    }

    private void RefreshMarketActorButtons()
    {
        StaggeredSpawnUtils.Restart(this, ref _marketActorCoroutine, RefreshMarketActorButtonsRoutine());
    }

    private IEnumerator RefreshMarketActorButtonsRoutine()
    {
        _gameManager.PoolingManager.ClearChildrenToPool(_orderMarketActorPopupBtnScrollViewContentTransform);
        _marketActorPopupBtnList.Clear();

        List<MarketActorEntry> actorEntries = new List<MarketActorEntry>();
        foreach (MarketActorEntry actorEntry in _dataManager.MarketActor.GetAllMarketActors().Values)
        {
            if (actorEntry == null)
                continue;

            if (_currentMarketActorType != null && actorEntry.data.marketActorType != _currentMarketActorType.Value)
                continue;

            actorEntries.Add(actorEntry);
        }

        yield return StaggeredSpawnUtils.ForEachFrame(actorEntries.Count, i =>
        {
            MarketActorEntry actorEntry = actorEntries[i];
            GameObject btnObj = _gameManager.PoolingManager.GetPooledObject(_orderMarketActorPopupBtnPrefab);
            btnObj.transform.SetParent(_orderMarketActorPopupBtnScrollViewContentTransform, false);
            MarketActorPopupBtn btn = btnObj.GetComponent<MarketActorPopupBtn>();
            if (btn != null)
                btn.Init(actorEntry);

            _marketActorPopupBtnList.Add(btn);
        });
    }

    private void RefreshOrderButtons()
    {
        StaggeredSpawnUtils.Restart(this, ref _orderButtonCoroutine, RefreshOrderButtonsRoutine());
    }

    private IEnumerator RefreshOrderButtonsRoutine()
    {
        List<OrderState> activeOrders = _dataManager.Order.GetActiveOrderList();
        List<OrderState> filteredOrders = activeOrders.Where(order => {
            if (order == null) return false;
            if (_showAcceptedOrders && !order.isAccepted) return false;
            if (!_showAcceptedOrders && order.isAccepted) return false;

            OrderData orderData = _dataManager.Order.GetOrderData(order.id);
            if (orderData == null || orderData.senderActorData == null) return false;

            MarketActorEntry actorEntry = _dataManager.MarketActor.GetMarketActorEntry(orderData.senderActorData.id);
            if (actorEntry == null) return false;

            return _currentMarketActorType == null || actorEntry.data.marketActorType == _currentMarketActorType.Value;
        }).ToList();

        List<OrderState> toRemove = _orderButtonMap.Keys
            .Where(state => !filteredOrders.Contains(state))
            .ToList();

        foreach (OrderState state in toRemove)
        {
            if (_orderButtonMap.TryGetValue(state, out OrderBtn btn))
            {
                _gameManager.PoolingManager.ReturnToPool(btn.gameObject);
                _orderButtonMap.Remove(state);
            }
        }

        List<OrderState> ordersToCreate = new List<OrderState>();
        foreach (OrderState order in filteredOrders)
        {
            if (_orderButtonMap.ContainsKey(order))
            {
                _orderButtonMap[order].Init(order, _uiManager);
                continue;
            }

            ordersToCreate.Add(order);
        }

        yield return StaggeredSpawnUtils.ForEachFrame(ordersToCreate.Count, i =>
        {
            OrderState order = ordersToCreate[i];
            GameObject btnObj = _gameManager.PoolingManager.GetPooledObject(_orderBtnPrefab);
            btnObj.transform.SetParent(_orderBtnScrollViewContentTransform, false);

            OrderBtn btn = btnObj.GetComponent<OrderBtn>();
            if (btn != null)
            {
                btn.Init(order, _uiManager);
                _orderButtonMap.Add(order, btn);
            }
        });
    }

    private void HandleOrderUpdated(OrderState orderState)
    {
        RefreshOrderButtons();
    }

    private void HandleDayChanged()
    {
        RefreshOrderButtons();
        UpdataUI();
    }

    private void HandleResourceChanged()
    {
        if (!gameObject.activeInHierarchy)
            return;

        UpdataUI();
    }
    
    protected override void OnDisable()
    {
        StaggeredSpawnUtils.Stop(this, ref _filterButtonCoroutine);
        StaggeredSpawnUtils.Stop(this, ref _marketActorCoroutine);
        StaggeredSpawnUtils.Stop(this, ref _orderButtonCoroutine);
        base.OnDisable();

        if (_acceptedOrderSwitch != null)
            _acceptedOrderSwitch.onValueChanged.RemoveListener(OnAcceptedOrderSwitchChanged);

        if (_dataManager != null)
        {
            _dataManager.Order.OnOrderChanged -= HandleOrderUpdated;
            _dataManager.Time.OnDayChanged -= HandleDayChanged;
            _dataManager.Resource.OnResourceChanged -= HandleResourceChanged;
        }
    }
}

