using UnityEngine;

[CreateAssetMenu(fileName = "NewUnionRequestData", menuName = "Game Data/Union Request Data")]
public class UnionRequestData : ScriptableObject
{
    public string id;
    public string description;

    public UnionRequestConditionType conditionType;
    public string targetId = string.Empty;
    public long targetValue;
    public float scaleFactor = 1f;

    public int rewardCohesion;
}
