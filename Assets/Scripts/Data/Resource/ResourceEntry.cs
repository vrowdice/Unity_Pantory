using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ResourceEntry
{
    public ResourceData data;   // 자원의 정적 데이터 (ScriptableObject)
    public ResourceState state; // 자원의 동적 상태
    
    public ResourceEntry(ResourceData data)
    {
        this.data = data;

        state = new ResourceState();
        state.currentEventValue = data.baseValue;
        state.count = data.initialAmount;
    }

    /// <summary>
    /// 가격을 기록하고 히스토리에 추가합니다.
    /// </summary>
    public void RecordPrice(float price)
    {
        if (state._priceHistory == null)
        {
            state._priceHistory = new List<float>(ResourceState.PriceHistoryCapacity);
        }

        float clampedPrice = Mathf.Max(0.01f, price);

        state._priceHistory.Add(clampedPrice);

        if (state._priceHistory.Count > ResourceState.PriceHistoryCapacity)
        {
            state._priceHistory.RemoveAt(0);
        }
    }

    /// <summary>
    /// 자원 수량을 안전하게 수정합니다. (0 이하로 내려가지 않음)
    /// </summary>
    /// <param name="amount">변경할 수량 (음수 가능)</param>
    /// <returns>실제로 변경된 수량</returns>
    public int ModifyCount(int amount)
    {
        int oldCount = state.count;
        state.count = Mathf.Max(0, state.count + amount);
        return state.count - oldCount;
    }

    /// <summary>
    /// Thread Delta를 안전하게 수정합니다.
    /// </summary>
    /// <param name="amount">변경할 수량</param>
    public void ModifyThreadDelta(int amount)
    {
        state.threadDeltaCount += amount;
    }

    /// <summary>
    /// Market Delta를 안전하게 수정합니다.
    /// </summary>
    /// <param name="amount">변경할 수량</param>
    public void ModifyMarketDelta(int amount)
    {
        state.marketDeltaCount += amount;
    }

    /// <summary>
    /// 자원 수량을 직접 설정합니다. (0 이하로 내려가지 않음)
    /// </summary>
    /// <param name="newCount">설정할 수량</param>
    public void SetCount(int newCount)
    {
        state.count = Mathf.Max(0, newCount);
    }
}
