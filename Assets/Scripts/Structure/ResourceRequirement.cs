using UnityEngine;

[System.Serializable]
public class ResourceRequirement
{
    [Tooltip("요구·산출할 자원 데이터")]
    public ResourceData resource;
    [Tooltip("필요·산출 개수")]
    public int count;
}
