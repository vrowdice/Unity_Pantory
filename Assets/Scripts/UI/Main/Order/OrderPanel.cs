using UnityEngine;
using Evo.UI;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 주문 관리 패널
/// </summary>
public class OrderPanel : BasePanel
{
    [SerializeField] private Transform _orderActionBtnContentTransform;
    [SerializeField] private Transform _orderMarketActorPopupBtnScrollViewContentTransform;
    [SerializeField] private Transform _orderBtnScrollViewContentTransform;
    [SerializeField] private Switch _acceptedOrderSwitch;

    [SerializeField] private GameObject _orderMarketActorPopupBtnPrefab;
    [SerializeField] private GameObject _orderBtnPrefab;

    private MarketActorType? _currentMarketActorType = null;
    private bool _showAcceptedOrders = false;

    private List<ActionBtn> _filterButtons = new List<ActionBtn>();
    private Dictionary<OrderState, OrderBtn> _orderButtonMap = new Dictionary<OrderState, OrderBtn>();

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

        _acceptedOrderSwitch.onValueChanged.AddListener(OnAcceptedOrderSwitchChanged);
    }

    private void InitializeFilterButtons()
    {
        int targetCount = EnumUtils.GetAllEnumValues<MarketActorType>().Count + 1;
        if (_orderActionBtnContentTransform.childCount == targetCount)
        {
            _filterButtons.Clear();
            foreach (Transform child in _orderActionBtnContentTransform)
            {
                ActionBtn btn = child.GetComponent<ActionBtn>();
                if (btn != null)
                {
                    _filterButtons.Add(btn);
                }
            }
            UpdateFilterHighlight();
            return;
        }

        _gameManager.PoolingManager.ClearChildrenToPool(_orderActionBtnContentTransform);
        _filterButtons.Clear();

        GameObject allBtnObj = Instantiate(_gameManager.ActionBtnPrefab, _orderActionBtnContentTransform);
        ActionBtn allBtn = allBtnObj.GetComponent<ActionBtn>();
        allBtn.Init(LocalizationUtils.Localize("All"), () => {
            OnMarketActorTypeClick(null);
            UpdateFilterHighlight();
        });
        _filterButtons.Add(allBtn);

        foreach (MarketActorType actorType in EnumUtils.GetAllEnumValues<MarketActorType>())
        {
            GameObject btnObj = Instantiate(_gameManager.ActionBtnPrefab, _orderActionBtnContentTransform);
            ActionBtn btn = btnObj.GetComponent<ActionBtn>();
            MarketActorType capturedType = actorType;
            btn.Init(actorType.Localize(), () => {
                OnMarketActorTypeClick(capturedType);
                UpdateFilterHighlight();
            });
            _filterButtons.Add(btn);
        }

        UpdateFilterHighlight();
    }

    private void UpdateFilterHighlight()
    {
        if (_filterButtons.Count == 0) return;

        _filterButtons[0].SetHighlight(_currentMarketActorType == null);

        List<MarketActorType> types = EnumUtils.GetAllEnumValues<MarketActorType>();
        for (int i = 0; i < types.Count && i + 1 < _filterButtons.Count; i++)
        {
            _filterButtons[i + 1].SetHighlight(_currentMarketActorType == types[i]);
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
        _gameManager.PoolingManager.ClearChildrenToPool(_orderMarketActorPopupBtnScrollViewContentTransform);
        foreach (MarketActorEntry actorEntry in _dataManager.MarketActor.GetAllMarketActors().Values)
        {
            if (actorEntry == null) continue;

            if (_currentMarketActorType != null && actorEntry.data.marketActorType != _currentMarketActorType.Value)
                continue;

            GameObject btnObj = Instantiate(_orderMarketActorPopupBtnPrefab, _orderMarketActorPopupBtnScrollViewContentTransform);
            MarketActorPopupBtn btn = btnObj.GetComponent<MarketActorPopupBtn>();
            if (btn != null)
            {
                btn.Init(actorEntry, _uiManager);
            }
        }
    }

    private List<OrderState> GetFilteredOrders()
    {
        List<OrderState> activeOrders = _dataManager.Order.GetActiveOrderList();
        return activeOrders.Where(order => {
            if (order == null) return false;
            if (_showAcceptedOrders && !order.isAccepted) return false;
            if (!_showAcceptedOrders && order.isAccepted) return false;

            OrderData orderData = _dataManager.Order.GetOrderData(order.id);
            if (orderData == null || orderData.senderActorData == null) return false;

            MarketActorEntry actorEntry = _dataManager.MarketActor.GetMarketActorEntry(orderData.senderActorData.id);
            if (actorEntry == null) return false;

            return _currentMarketActorType == null || actorEntry.data.marketActorType == _currentMarketActorType.Value;
        }).ToList();
    }

    private void RefreshOrderButtons()
    {
        List<OrderState> filteredOrders = GetFilteredOrders();

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

        foreach (OrderState order in filteredOrders)
        {
            if (order == null) continue;
            if (_orderButtonMap.ContainsKey(order)) continue;

            GameObject btnObj = _gameManager.PoolingManager.GetPooledObject(_orderBtnPrefab);
            btnObj.transform.SetParent(_orderBtnScrollViewContentTransform, false);

            OrderBtn btn = btnObj.GetComponent<OrderBtn>();
            if (btn != null)
            {
                btn.Init(order, _uiManager);
                _orderButtonMap.Add(order, btn);
            }
        }
    }

    private void HandleOrderUpdated(OrderState orderState)
    {
        RefreshOrderButtons();
    }

    private void HandleDayChanged()
    {
        foreach (OrderBtn btn in _orderButtonMap.Values)
        {
            btn.UpdateUI();
        }
    }

    private void HandleResourceChanged()
    {
        foreach (OrderBtn btn in _orderButtonMap.Values)
        {
            btn.UpdateUI();
        }
    }
    
    private void OnDisable()
    {
        _dataManager.Order.OnOrderChanged -= HandleOrderUpdated;
        _dataManager.Time.OnDayChanged -= HandleDayChanged;
        _dataManager.Resource.OnResourceChanged -= HandleResourceChanged;
    }
}

