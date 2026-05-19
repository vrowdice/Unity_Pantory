using System;
using UnityEngine;

[Serializable]
public class ResearchState
{
    [Tooltip("연구 트리에서 해금되어 연구 가능한지")]
    public bool isUnlocked = false;
    [Tooltip("연구 완료 여부")]
    public bool isCompleted = false;
}
