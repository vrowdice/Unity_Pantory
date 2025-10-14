using UnityEngine;

/// <summary>
/// 건물의 현재 상태를 나타내는 클래스
/// </summary>
public class BuildingState
{
    // 건물이 건설되었는지 여부
    public bool isConstructed;
    
    // 건물의 현재 레벨 (0 = 건설 안됨)
    public int level;
    
    // 작업 효율 (0.0 ~ 무한대, 기본값 1.0)
    public float workingEfficiency;

    public BuildingState()
    {
        isConstructed = false;
        level = 0;
        workingEfficiency = 0f;
    }
}
