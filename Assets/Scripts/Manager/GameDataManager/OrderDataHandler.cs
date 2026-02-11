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
        _orderDataList = orderDataList;
        _initialOrderData = initialOrderData;
        _orderDataDict = new Dictionary<string, OrderData>();

        if (_orderDataList != null)
        {
            foreach (OrderData data in _orderDataList)
            {
                if (data == null || string.IsNullOrEmpty(data.id)) continue;
                if (_orderDataDict.ContainsKey(data.id)) continue;
                _orderDataDict[data.id] = data;
            }
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

    }

    private void ResetOrderChance()
    {
        _currentOrderChance = _initialOrderData.baseOrderChance;
        _daysSinceLastOrder = 0;
    }

    public OrderData GetOrderData(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (_orderDataDict.TryGetValue(id, out var data))
        {
            return data;
        }
        return null;
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

    public void HandleDayChanged()
    {
        _daysSinceLastOrder++;

        TryGenerateOrder();
    }
}
