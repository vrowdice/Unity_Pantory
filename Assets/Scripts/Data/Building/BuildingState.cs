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
    
    // 건물 회전 (0=0도, 1=90도, 2=180도, 3=270도)
    public int rotation = 0;
    
    // 스레드(그리드) 기준 절대 좌표
    public Vector2Int inputPosition;
    public Vector2Int outputPosition;

    public BuildingState(string buildingId, Vector2Int position, BuildingData buildingData, int rotation = 0)
    {
        this.buildingId = buildingId;
        this.position = position;
        this.rotation = rotation;
        
        // 건물의 배치 위치 + 회전된 상대 위치 = 스레드 기준 절대 좌표
        // InputPosition이 zero가 아니면 계산, zero면 건물 중심으로 설정
        if (buildingData.InputPosition != Vector2Int.zero)
        {
            this.inputPosition = position + RotatePositionAroundCenter(buildingData.InputPosition, rotation, buildingData.size);
        }
        else
        {
            // InputPosition이 zero면 건물 중심으로 설정 (LoadStation 등)
            this.inputPosition = position + new Vector2Int(buildingData.size.x / 2, buildingData.size.y / 2);
        }
        
        // OutputPosition이 zero가 아니면 계산, zero면 건물 중심으로 설정
        if (buildingData.OutputPosition != Vector2Int.zero)
        {
            this.outputPosition = position + RotatePositionAroundCenter(buildingData.OutputPosition, rotation, buildingData.size);
        }
        else
        {
            // OutputPosition이 zero면 건물 중심으로 설정 (UnloadStation 등)
            this.outputPosition = position + new Vector2Int(buildingData.size.x / 2, buildingData.size.y / 2);
        }

        inputProductionIds = new List<string>();
        outputProductionIds = new List<string>();
    }
    
    /// <summary>
    /// 건물 크기를 고려하여 중심을 기준으로 위치를 회전합니다.
    /// </summary>
    private Vector2Int RotatePositionAroundCenter(Vector2Int pos, int rotation, Vector2Int buildingSize)
    {
        rotation = rotation % 4;
        if (rotation == 0)
            return pos;
        
        // 건물 중심 계산
        float centerX = (buildingSize.x - 1) / 2f;
        float centerY = (buildingSize.y - 1) / 2f;
        
        // 중심 기준으로 변환
        float relX = pos.x - centerX;
        float relY = pos.y - centerY;
        
        // 회전 적용
        float rotatedX, rotatedY;
        switch (rotation)
        {
            case 1: // 90도 시계방향: (x, y) -> (-y, x)
                rotatedX = -relY;
                rotatedY = relX;
                break;
            case 2: // 180도: (x, y) -> (-x, -y)
                rotatedX = -relX;
                rotatedY = -relY;
                break;
            case 3: // 270도 시계방향: (x, y) -> (y, -x)
                rotatedX = relY;
                rotatedY = -relX;
                break;
            default:
                rotatedX = relX;
                rotatedY = relY;
                break;
        }
        
        // 다시 절대 좌표로 변환
        return new Vector2Int(
            Mathf.RoundToInt(rotatedX + centerX),
            Mathf.RoundToInt(rotatedY + centerY)
        );
    }
}
