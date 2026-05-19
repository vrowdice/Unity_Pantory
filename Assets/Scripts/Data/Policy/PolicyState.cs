using System;
using UnityEngine;

[Serializable]
public class PolicyState
{
    [Tooltip("정책 활성화 여부")]
    public bool isActive;
    [Tooltip("ON/OFF 변경 잠금 남은 개월 수")]
    public int remainingMonths;
}
