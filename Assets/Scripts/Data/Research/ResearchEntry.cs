using UnityEngine;

public class ResearchEntry
{
    [Tooltip("연구 ScriptableObject")]
    public ResearchData data;
    [Tooltip("해금·완료 등 런타임 상태")]
    public ResearchState state;
}
