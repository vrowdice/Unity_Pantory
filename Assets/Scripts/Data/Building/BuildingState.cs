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
    public int positionX;
    public int positionY;
    
    // 건물 회전 (0=0도, 1=90도, 2=180도, 3=270도)
    public int rotation = 0;
    
    // 스레드(그리드) 기준 절대 좌표
    public Vector2Int inputPosition;
    public Vector2Int outputPosition;

    // 현재 이 건물을 지나가거나 처리 중인 자원 ID (null이면 없음)
    public string currentResourceId; 
    
    // 자원 충돌 발생 여부 (여러 자원이 겹침)
    public bool hasResourceConflict;
    
    public BuildingState(string buildingId, Vector2Int position, BuildingData buildingData, int rotation = 0)
    {
        this.buildingId = buildingId;
        this.positionX = position.x;
        this.positionY = position.y;
        this.rotation = rotation;
        this.currentResourceId = null;
        this.hasResourceConflict = false;

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
    /// 런타임 상태를 초기화합니다 (매 프레임 경로 계산 전 호출)
    /// </summary>
    public void ResetRuntimeStatus()
    {
        currentResourceId = null;
        hasResourceConflict = false;
    }
    
    /// <summary>
    /// 이 건물이 연구를 통해 언락되었는지 확인합니다.
    /// GameDataManager를 통해 연구 완료 상태를 확인합니다.
    /// </summary>
    /// <param name="gameDataManager">게임 데이터 매니저 (연구 상태 확인용)</param>
    /// <returns>언락되었으면 true, 연구가 필요 없거나 완료되었으면 true</returns>
    public bool IsUnlocked(GameDataManager gameDataManager)
    {
        if (gameDataManager == null || string.IsNullOrEmpty(buildingId))
            return false;
            
        // BuildingData 가져오기
        var buildingData = gameDataManager?.Building?.GetBuildingData(buildingId);
        if (buildingData == null)
            return false;
            
        // 연구가 필요 없으면 언락됨
        if (string.IsNullOrEmpty(buildingData.requiredResearchId))
            return true;
            
        // ResearchDataHandler를 통해 연구 완료 상태 확인
        if (gameDataManager.Research != null)
        {
            return gameDataManager.Research.IsResearchCompleted(buildingData.requiredResearchId);
        }
        
        // ResearchDataHandler가 없으면 연구가 필요한데 완료되지 않은 것으로 간주
        return false;
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
