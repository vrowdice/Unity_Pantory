using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 자원 데이터와 상태를 포함하는 엔트리.
/// 이펙트는 EffectDataHandler에서 instanceId = data.id 로 관리합니다.
/// </summary>
[Serializable]
public class ResourceEntry
{
    public ResourceData data;
    public ResourceState state;

    public ResourceEntry(ResourceData data, int priceHistoryCapacity = 60)
    {
        this.data = data;

        state = new ResourceState();
        state.currentEventValue = data.baseValue;
        state.count = data.initialAmount;
    }

    /// <summary>
    /// 가격을 기록하고 히스토리에 추가합니다.
    /// </summary>
    public void RecordPrice(float price, int priceHistoryCapacity)
    {
        float clampedPrice = Mathf.Max(0.01f, price);

        state._priceHistory.Add(clampedPrice);

        if (state._priceHistory.Count > priceHistoryCapacity)
        {
            state._priceHistory.RemoveAt(0);
        }
    }

    /// <summary>
    /// 자원 수량을 수정
    /// </summary>
    /// <param name="amount">변경할 수량</param>
    /// <returns>실제로 변경된 수량</returns>
    public bool ModifyCount(int amount)
    {
        int newCount = state.count + amount;
        if (newCount < 0)
        {
            return false;
        }
        state.count = newCount;
        return true;
    }
}
