using System;

[Serializable]
public class ResourceState
{
    public long count;           // 보유 수량
    public float currentValue;      // 현재 가격
    public float priceChangeRate;   // 가격 변동률
    public bool isEnchanted;        // 마법 주문 적용 여부
    public long deltaCount;         // 최근 변화 수량

    public ResourceState()
    {
        currentValue = 0f;
        priceChangeRate = 0f;
        count = 0;
        deltaCount = 0;
    }
}
