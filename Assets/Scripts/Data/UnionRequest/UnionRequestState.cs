using System;
using System.Collections.Generic;

[Serializable]
public class UnionRequestState
{
    public string id;
    public int remainingDays;
    public bool isFulfilled;

    public long requireCredit;
    public List<ResourceRequirementState> resourceRequirements = new List<ResourceRequirementState>();
    public List<string> requiredPolicyIds = new List<string>();

    [Serializable]
    public class ResourceRequirementState
    {
        public string resourceId;
        public int count;
    }

    public UnionRequestState() { }

    public UnionRequestState(UnionRequestData template)
    {
        id = template.id;
    }
}
