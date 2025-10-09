using System;

[Serializable]
public class ResourceState
{
    public float currentValue;      // 현재 가격
    public float priceChangeRate;   // 가격 변동률
    public long quantity;           // 보유 수량
    
    public ResourceState()
    {
        currentValue = 0f;
        priceChangeRate = 0f;
        quantity = 0;
    }
}
