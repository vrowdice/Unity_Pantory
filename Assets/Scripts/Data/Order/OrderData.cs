using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewOrderData", menuName = "Game Data/Order Data")]
public class OrderData : ScriptableObject
{
    public string id;
    public string displayName;
    [TextArea(2, 6)]
    public string description;
    public OrderClientType clientType;
    public Sprite clientIcon;

    // 이 의뢰가 플레이어의 Wealth 대비 어느 정도의 비중을 차지할지 (예: 0.1 = 자산의 10% 규모)
    public float scaleFactor;
    // 이 의뢰에서 요구할 수 있는 자원들
    public List<ResourceData> potentialResources;
    // 시장가 대비 보상 배율
    public float priceMultiplier;
}
