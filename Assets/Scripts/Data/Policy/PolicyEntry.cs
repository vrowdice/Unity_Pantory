using UnityEngine;

public class PolicyEntry
{
    [Tooltip("정책 ScriptableObject")]
    public PolicyData data;
    [Tooltip("활성화·잠금 등 런타임 상태")]
    public PolicyState state;
}
