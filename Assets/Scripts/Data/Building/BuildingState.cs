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
    public string buildingId;
    public List<string> inputProductionIds;
    public List<string> outputProductionIds;
    public int positionX;
    public int positionY;
    public int rotation = 0;
    public Vector2Int inputPosition;
    public Vector2Int outputPosition;
    public string currentResourceId; 
    
    public BuildingState(string buildingId, Vector2Int position, BuildingData buildingData, int rotation = 0)
    {
        this.buildingId = buildingId;
        this.positionX = position.x;
        this.positionY = position.y;
        this.rotation = rotation;
        this.currentResourceId = null;

        if (buildingData.InputPosition != Vector2Int.zero)
        {
            this.inputPosition = position + RotatePositionAroundCenter(buildingData.InputPosition, rotation, buildingData.size);
        }
        else
        {
            this.inputPosition = position + new Vector2Int(buildingData.size.x / 2, buildingData.size.y / 2);
        }

        if (buildingData.OutputPosition != Vector2Int.zero)
        {
            this.outputPosition = position + RotatePositionAroundCenter(buildingData.OutputPosition, rotation, buildingData.size);
        }
        else
        {
            this.outputPosition = position + new Vector2Int(buildingData.size.x / 2, buildingData.size.y / 2);
        }

        inputProductionIds = new List<string>();
        outputProductionIds = new List<string>();
    }
    
    /// <summary>
    /// 이 건물이 연구를 통해 언락되었는지 확인합니다.
    /// GameDataManager를 통해 연구 완료 상태를 확인합니다.
    /// </summary>
    /// <param name="dataManager">게임 데이터 매니저 (연구 상태 확인용)</param>
    /// <returns>언락되었으면 true, 연구가 필요 없거나 완료되었으면 true</returns>
    public bool IsUnlocked(DataManager dataManager)
    {
        BuildingData buildingData = dataManager.Building.GetBuildingData(buildingId);
        if (buildingData == null)
            return false;

        if(buildingData.requiredResearch == null)
        {
            return true;
        }

        return dataManager.Research.IsResearchCompleted(buildingData.requiredResearch.id);
    }
    
    /// <summary>
    /// 건물 크기를 고려하여 중심을 기준으로 위치를 회전합니다.
    /// </summary>
    private Vector2Int RotatePositionAroundCenter(Vector2Int pos, int rotation, Vector2Int buildingSize)
    {
        rotation = rotation % 4;
        if (rotation == 0)
            return pos;

        float centerX = (buildingSize.x - 1) / 2f;
        float centerY = (buildingSize.y - 1) / 2f;

        float relX = pos.x - centerX;
        float relY = pos.y - centerY;

        float rotatedX, rotatedY;
        switch (rotation)
        {
            case 1:
                rotatedX = -relY;
                rotatedY = relX;
                break;
            case 2:
                rotatedX = -relX;
                rotatedY = -relY;
                break;
            case 3:
                rotatedX = relY;
                rotatedY = -relX;
                break;
            default:
                rotatedX = relX;
                rotatedY = relY;
                break;
        }

        return new Vector2Int(
            Mathf.RoundToInt(rotatedX + centerX),
            Mathf.RoundToInt(rotatedY + centerY)
        );
    }
}
