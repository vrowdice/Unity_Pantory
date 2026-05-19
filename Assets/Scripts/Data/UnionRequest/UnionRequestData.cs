using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewUnionRequestData", menuName = "Game Data/Union Request Data")]
public class UnionRequestData : ScriptableObject
{
    public string id;
    public string description;

    public long requireCredit;
    public float creditScaleFactor = 1f;
    public List<ResourceRequirement> requireResourceRequirementList;
    public float resourceScaleFactor = 0.05f;
    public List<PolicyData> requirePolicyList;

    public int rewardCohesion;
}