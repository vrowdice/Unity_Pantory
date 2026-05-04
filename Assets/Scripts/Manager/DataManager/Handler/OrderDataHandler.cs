using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class OrderDataHandler : IDataHandlerEvents, ITimeChangeHandler
{
    private readonly DataManager _dataManager;
    private readonly InitialOrderData _initialOrderData;

    private readonly Dictionary<string, OrderData> _orderDataDict;
    private readonly List<OrderData> _orderDataList = new List<OrderData>();
    private readonly List<OrderState> _activeOrderList = new List<OrderState>();

    private int _daysSinceLastOrder = 0;
    private float _currentOrderChance = 0.0f;

    public event Action<OrderState> OnOrderChanged;

    public OrderDataHandler(DataManager dataManager, List<OrderData> orderDataList, InitialOrderData initialOrderData)
    {
        _dataManager = dataManager;
        _orderDataList = orderDataList ?? new List<OrderData>();
        _initialOrderData = initialOrderData;
        _orderDataDict = new Dictionary<string, OrderData>();

        foreach (OrderData data in _orderDataList)
        {
            if (data == null || string.IsNullOrEmpty(data.id)) continue;
            if (_orderDataDict.ContainsKey(data.id)) continue;
            _orderDataDict[data.id] = data;
        }

        ResetOrderChance();
    }

    private void TryGenerateOrder()
    {
        if (_activeOrderList.Count >= _initialOrderData.maxOrderItems)
        {
            ResetOrderChance();
            return;
        }

        _currentOrderChance += _initialOrderData.orderChanceIncrement;
        _daysSinceLastOrder += 1;

        float randomValue = UnityEngine.Random.Range(0f, 1f);

        if (randomValue <= _currentOrderChance || _daysSinceLastOrder >= _initialOrderData.guaranteedOrderDay)
        {
            GenerateOrder();
            ResetOrderChance();
        }
    }

    private void GenerateOrder()
    {
        long wealth = _dataManager.Finances.Wealth;

        List<MarketActorType> possibleTypes = EnumUtils.GetAllEnumValues<MarketActorType>();
        if (wealth < _initialOrderData.governmentOrderAvailableWealth)
            possibleTypes.Remove(MarketActorType.Government);
        if (wealth < _initialOrderData.companyOrderAvailableWealth)
            possibleTypes.Remove(MarketActorType.Company);

        Dictionary<MarketActorEntry, float> actorWeights = new Dictionary<MarketActorEntry, float>();
        float totalWeight = 0f;

        foreach (MarketActorEntry actorEntry in _dataManager.MarketActor.GetAllMarketActors().Values)
        {
            if (!possibleTypes.Contains(actorEntry.data.marketActorType)) continue;
            float weight = Mathf.Max(1f, (float)actorEntry.state.trust);
            actorWeights.Add(actorEntry, weight);
            totalWeight += weight;
        }

        if (actorWeights.Count == 0) return;

        MarketActorEntry selectedActor = GetWeightedRandomActor(actorWeights, totalWeight);
        List<OrderData> possibleTemplates = _orderDataDict.Values
            .Where(data => data.marketActorType == selectedActor.data.marketActorType)
            .ToList();

        if (possibleTemplates.Count > 0)
        {
            OrderData randomTemplate = possibleTemplates[UnityEngine.Random.Range(0, possibleTemplates.Count)];
            CreateOrderInstance(randomTemplate, selectedActor);
        }
    }

    /// <summary>
    /// 새 주문 생성.
    /// </summary>
    /// <param name="orderData">주문 데이터</param>
    /// <param name="marketActorEntry">거래처</param>
    private void CreateOrderInstance(OrderData orderData, MarketActorEntry marketActorEntry)
    {
        OrderState newState = new OrderState(orderData);

        newState.durationDays = _initialOrderData.orderAcceptanceDelayDays;

        newState.resourceRequestList = new List<OrderState.ResourceRequest>();
        long totalMarketValue = 0;
        long playerWealth = _dataManager.Finances.Wealth;

        foreach (ResourceData resourceData in orderData.potentialResources)
        {
            ResourceEntry resourceEntry = _dataManager.Resource.GetResourceEntry(resourceData.id);
            if (resourceEntry == null) continue;

            float resourceCount = orderData.potentialResources.Count;
            float allocatedWealth = (playerWealth * orderData.scaleFactor) / resourceCount;
            int count = Mathf.RoundToInt(allocatedWealth / resourceEntry.state.currentValue);
            count = Mathf.Max(1, count);

            OrderState.ResourceRequest request = new OrderState.ResourceRequest
            {
                resourceId = resourceData.id,
                requiredCount = count
            };

            newState.resourceRequestList.Add(request);
            totalMarketValue += (long)count * resourceEntry.state.currentValue;
        }
        float trustBonus = (marketActorEntry.state.trust - 50) * 0.001f;
        float finalMultiplier = orderData.priceMultiplier + trustBonus;

        newState.rewardCredit = (long)(totalMarketValue * finalMultiplier);

        _activeOrderList.Add(newState);
        OnOrderChanged?.Invoke(newState);
    }

    private MarketActorEntry GetWeightedRandomActor(Dictionary<MarketActorEntry, float> weights, float totalWeight)
    {
        float randomValue = UnityEngine.Random.Range(0f, totalWeight);
        float cumulative = 0f;

        foreach (KeyValuePair<MarketActorEntry, float> kvp in weights)
        {
            cumulative += kvp.Value;
            if (randomValue <= cumulative) return kvp.Key;
        }

        return weights.Keys.First();
    }

    private void ResetOrderChance()
    {
        _currentOrderChance = _initialOrderData.baseOrderChance;
        _daysSinceLastOrder = 0;
    }

    public void AcceptAndCompleteOrder(OrderState order)
    {
        if (order == null) return;

        if (order.isAccepted)
        {
            foreach (OrderState.ResourceRequest request in order.resourceRequestList)
            {
                if (_dataManager.Resource.ModifyResourceCount(request.resourceId, -request.requiredCount)) continue;
                UIManager.Instance.ShowWarningPopup(WarningMessage.NotEnoughResources);
                return;
            }

            _dataManager.Finances.ModifyCredit(order.rewardCredit);
            _dataManager.MarketActor.ModifyMarketActorTrust(order.senderActorId, order.rewardTrust);

            _activeOrderList.Remove(order);
        }
        else
        {
            order.isAccepted = true;
            order.durationDays = GetOrderData(order.id).durationDays;
        }

        OnOrderChanged?.Invoke(order);
    }

    public OrderData GetOrderData(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        return _orderDataDict.TryGetValue(id, out OrderData data) ? data : null;
    }

    public Dictionary<string, OrderData> GetAllOrderData()
    {
        return new Dictionary<string, OrderData>(_orderDataDict);
    }

    public List<OrderState> GetActiveOrderList()
    {
        return new List<OrderState>(_activeOrderList);
    }

    public void ClearAllSubscriptions()
    {
        OnOrderChanged = null;
    }

    public void CaptureTo(GameSaveData saveData)
    {
        if (saveData == null)
        {
            return;
        }

        foreach (OrderState state in _activeOrderList)
        {
            saveData.activeOrders.Add(CloneState(state));
        }
    }

    public void ApplyFromSave(GameSaveData saveData)
    {
        if (saveData == null)
        {
            return;
        }

        _activeOrderList.Clear();
        foreach (OrderState state in saveData.activeOrders)
        {
            _activeOrderList.Add(CloneState(state));
        }
    }

    public void HandleDayChanged()
    {
        for (int i = _activeOrderList.Count - 1; i >= 0; i--)
        {
            OrderState order = _activeOrderList[i];
            order.durationDays--;
            if (order.durationDays <= 0)
            {
                if(order.isAccepted)
                {
                    _dataManager.MarketActor.ModifyMarketActorTrust(order.senderActorId, -order.rewardTrust / 2);
                }

                _activeOrderList.RemoveAt(i);
            }
        }

        TryGenerateOrder();
    }

    private static OrderState CloneState(OrderState state)
    {
        string json = JsonUtility.ToJson(state);
        return JsonUtility.FromJson<OrderState>(json);
    }
}
