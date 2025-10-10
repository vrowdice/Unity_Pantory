using System;

[Serializable]
public class ResourceState
{
    public int count;              // 개수
    public float currentValue;      // 현재 가격
    public float priceChangeRate;   // 가격 변동률
    public long quantity;           // 보유 수량
    public bool isEnchanted;        // 마법 주문 적용 여부

    public ResourceState()
    {
        currentValue = 0f;
        priceChangeRate = 0f;
        quantity = 0;
    }
}
