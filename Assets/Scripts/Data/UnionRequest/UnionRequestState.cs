using System;

[Serializable]
public class UnionRequestState
{
    public string id;
    public int remainingDays;
    public bool isFulfilled;

    public UnionRequestConditionType conditionType;
    public string targetId = string.Empty;
    public long targetValue;

    public UnionRequestState() { }

    public UnionRequestState(UnionRequestData template)
    {
        id = template.id;
        conditionType = template.conditionType;
    }
}
