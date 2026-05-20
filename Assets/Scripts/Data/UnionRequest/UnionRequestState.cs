using System;

[Serializable]
public class UnionRequestState
{
    public string id;
    public int remainingDays;
    public bool isFulfilled;

    public long requireCredit;
    public string requireResourceId = string.Empty;
    public int requireResourceCount;
    public string requiredPolicyId = string.Empty;

    public UnionRequestState() { }

    public UnionRequestState(UnionRequestData template)
    {
        id = template.id;
    }
}
