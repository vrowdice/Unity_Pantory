using System;
using UnityEngine;

/// <summary>
/// ThreadState에 건설된 건물을 나타내는 클래스
/// 건물의 성능은 연구 시스템에서 관리됩니다.
/// </summary>
[Serializable]
public class BuildingState
{
    // 건물 ID (BuildingData 참조용)
    public string buildingId;
    
    // 건물 배치 위치
    public Vector2Int position;

    public BuildingState(string buildingId, Vector2Int position)
    {
        this.buildingId = buildingId;
        this.position = position;
    }
}
