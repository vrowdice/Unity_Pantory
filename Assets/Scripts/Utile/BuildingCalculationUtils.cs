using System.Collections.Generic;
using UnityEngine;

public static class BuildingCalculationUtils
{
    /// <summary>
    /// 건물 목록 기준으로 경제/자원 수치를 계산합니다. 연구 완료 여부와 관계없이 모든 건물을 포함합니다.
    /// </summary>
    public static ThreadCalculationResult CalculateProductionStats(
        DataManager dataManager,
        List<BuildingState> buildingStates)
    {
        ThreadCalculationResult result = new ThreadCalculationResult();
        foreach (BuildingState buildingState in buildingStates)
        {
            BuildingData buildingData = dataManager.Building.GetBuildingData(buildingState.Id);

            result.TotalBuildCost += buildingData.buildCost;
            result.TotalMaintenanceCost += buildingData.maintenanceCost;
            result.TotalRequiredEmployees += buildingData.requiredEmployees;
            result.RequiredTechnicians += buildingData.isProfessional ? buildingData.requiredEmployees : 0;
            if (buildingData.IsProductionBuilding)
            {
                ProcessBuildingResources(dataManager, buildingState, result);
            }
        }

        return result;
    }

    private static void ProcessBuildingResources(
        DataManager dataManager,
        BuildingState state,
        ThreadCalculationResult result)
    {
        if (state.inputProductionIds != null)
        {
            foreach (string id in state.inputProductionIds)
            {
                if (string.IsNullOrEmpty(id)) continue;
                result.InputResourceCounts[id] = result.InputResourceCounts.GetValueOrDefault(id, 0) + 1;
            }
        }

        if (state.outputProductionIds != null)
        {
            foreach (string id in state.outputProductionIds)
            {
                if (string.IsNullOrEmpty(id)) continue;
                result.OutputResourceCounts[id] = result.OutputResourceCounts.GetValueOrDefault(id, 0) + 1;
            }
        }
    }

    /// <summary>
    /// 건물/도로의 원점, 사이즈, 회전 기준으로 출력 그리드 좌표를 계산합니다.
    /// BuildingObject / RoadObject 양쪽에서 공통 사용.
    /// </summary>
    public static List<Vector2Int> GetOutputGridPositions(Vector2Int origin, Vector2Int size, int rotation)
    {
        List<Vector2Int> result = new List<Vector2Int>();

        int rightX = size.x - 1;
        for (int y = 0; y < size.y; y++)
        {
            Vector2Int localCell = new Vector2Int(rightX, y);
            Vector2Int rotatedLocal = RotateCellAroundCenter(localCell, rotation, size);
            Vector2Int worldGridPos = origin + rotatedLocal;
            result.Add(worldGridPos);
        }

        return result;
    }

    /// <summary>
    /// 건물/도로의 사이즈, 회전 기준으로 출력 인디케이터의 로컬 위치들을 계산합니다.
    /// BuildingObject / RoadObject 양쪽에서 공통 사용.
    /// </summary>
    public static List<Vector3> GetOutputLocalPositions(Vector2Int size, int rotation)
    {
        List<Vector3> result = new List<Vector3>();

        int rightX = size.x - 1;
        for (int y = 0; y < size.y; y++)
        {
            Vector2Int localCell = new Vector2Int(rightX, y);
            Vector3 localPos = GetLocalPositionForCell(localCell, size);
            localPos = RotateOffset(localPos, rotation);
            result.Add(localPos);
        }

        return result;
    }

    private static Vector3 GetLocalPositionForCell(Vector2Int cell, Vector2Int size)
    {
        float centerX = (size.x - 1) * 0.5f;
        float centerY = (size.y - 1) * 0.5f;

        float x = cell.x + 0.5f - centerX;
        float y = -(cell.y + 0.5f) + centerY;

        return new Vector3(x, y, 0f);
    }

    private static Vector3 RotateOffset(Vector3 offset, int rotation)
    {
        float angle = -rotation * 90f;
        Quaternion rot = Quaternion.Euler(0f, 0f, angle);
        return rot * offset;
    }

    private static Vector2Int RotateCellAroundCenter(Vector2Int cell, int rotation, Vector2Int size)
    {
        rotation = rotation % 4;
        if (rotation == 0)
        {
            return cell;
        }

        float centerX = (size.x - 1) / 2f;
        float centerY = (size.y - 1) / 2f;

        float relX = cell.x - centerX;
        float relY = cell.y - centerY;

        float rotatedX;
        float rotatedY;

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