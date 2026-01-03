using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ResourceState
{
    [Header("Inventory")]
    public int count;
    public int threadDeltaCount;
    public int marketDeltaCount;
    public int currnetChangeCount;

    public float value;
    public float currentChangeValue;

    public const int PriceHistoryCapacity = 60;
    [SerializeField] private List<float> _priceHistory = new List<float>(PriceHistoryCapacity);
    public IReadOnlyList<float> PriceHistory => _priceHistory;

    public ResourceState()
    {
        InitializeDefaults();
    }

    public void InitializeFromData(ResourceData data)
    {
        InitializeDefaults();

        if (data == null)
        {
            return;
        }

        count = data.initialAmount;
        RecordPrice(value);
    }

    private void InitializeDefaults()
    {
        value = 0f;
        count = 0;
        threadDeltaCount = 0;

        if (_priceHistory == null)
        {
            _priceHistory = new List<float>(PriceHistoryCapacity);
        }
        else
        {
            _priceHistory.Clear();
        }
    }

    public void RecordPrice(float price)
    {
        if (_priceHistory == null)
        {
            _priceHistory = new List<float>(PriceHistoryCapacity);
        }

        float clampedPrice = Mathf.Max(0.01f, price);
        _priceHistory.Add(clampedPrice);
        if (_priceHistory.Count > PriceHistoryCapacity)
        {
            _priceHistory.RemoveAt(0);
        }
    }
}
