using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewOrderData", menuName = "Game Data/Order Data")]
public class OrderData : ScriptableObject
{
    public string id;
    public string displayName;
    public string senderActorId;
    public MarketActorType marketActorType;
    public int durationDays;
    public float scaleFactor;
    public List<ResourceData> potentialResources;
    public float priceMultiplier;
}
