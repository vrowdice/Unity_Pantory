using System.Collections.Generic;
using UnityEngine;

public class OrderDataHandler : IDataHandlerEvents, ITimeChangeHandler
{
    private readonly DataManager _dataManager;
    private readonly InitialOrderData _initialOrderData;

    private readonly Dictionary<string, OrderData> _orderDataDict;
    private readonly List<OrderData> _orderDataList;

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

    public void ClearAllSubscriptions()
    {

    }

    public void HandleDayChanged()
    {

    }
}
