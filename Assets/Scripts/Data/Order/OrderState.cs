using System;
using System.Collections.Generic;

[Serializable]
public class OrderState
{
    public string id;
    public string senderActorId;
    public List<ResourceRequest> resourceRequestList;
    public long rewardCredit;
    public int rewardTrust;
    public int durationDays;
    public bool isAccepted;

    [Serializable]
    public class ResourceRequest
    {
        public string resourceId;
        public int requiredCount;
    }

    public OrderState(OrderData orderData)
    {
        id = orderData.id;
        senderActorId = orderData.senderActorData != null ? orderData.senderActorData.id : string.Empty;
        isAccepted = false;

        rewardTrust = orderData.rewardTrust;
    }
}
