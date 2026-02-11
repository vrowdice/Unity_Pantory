using System;
using System.Collections.Generic;

[Serializable]
public class OrderState
{
    public string id;
    public OrderClientType clientType;
    public List<ResourceRequest> resourceRequestList;
    public long totalRewardCredit;
    public int expireDay;
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
        clientType = orderData.clientType;
        isAccepted = false;
        isCompleted = false;
    }
}
