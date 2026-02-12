using System;
using System.Collections.Generic;

[Serializable]
public class OrderState
{
    public string id;
    public string senderActorId;
    public List<ResourceRequest> resourceRequestList;
    public long totalRewardCredit;
    public int durationDays;
    public bool isAccepted;
    public bool isCompleted;

    [Serializable]
    public class ResourceRequest
    {
        public string resourceId;
        public int requiredCount;
    }

    public OrderState(OrderData orderData)
    {
        id = orderData.id;
        senderActorId = orderData.senderActorId;
        isAccepted = false;
        isCompleted = false;
    }
}
