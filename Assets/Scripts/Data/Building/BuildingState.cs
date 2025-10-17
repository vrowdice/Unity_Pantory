using System;
using System.Collections.Generic;
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
    public List<string> inputProductionIds;
    public List<string> outputProductionIds;

    // 건물 배치 위치
    public Vector2Int position;
    
    // 스레드(그리드) 기준 절대 좌표
    public Vector2Int inputPosition;
    public Vector2Int outputPosition;

    public BuildingState(string buildingId, Vector2Int position, BuildingData buildingData)
    {
        this.buildingId = buildingId;
        this.position = position;
        
        // 건물의 배치 위치 + 상대 위치 = 스레드 기준 절대 좌표
        this.inputPosition = position + buildingData.inputPosition;
        this.outputPosition = position + buildingData.outputPosition;
    }
}
