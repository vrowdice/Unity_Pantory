using System.Collections.Generic;
using UnityEngine;

public static class BuildingCalculationUtils
{
    /// <summary>
    /// 발자국 내부 면이 아니라 도로/건물이 올 수 있는 이웃 칸
    /// </summary>
    public static List<Vector2Int> GetOutputGridPositions(Vector2Int origin, Vector2Int size, int rotation)
    {
        List<Vector2Int> result = new List<Vector2Int>();

        int rightX = size.x - 1;
        for (int y = 0; y < size.y; y++)
        {
            Vector2Int localOutside = new Vector2Int(rightX + 1, y);
            Vector2Int rotatedLocal = RotateCellAroundCenter(localOutside, rotation, size);
            result.Add(origin + rotatedLocal);
        }

        return result;
    }

    /// <summary>
    /// 인디케이터용
    /// </summary>
    public static List<Vector3> GetOutputLocalPositions(Vector2Int size)
    {
        List<Vector3> result = new List<Vector3>();

        int rightX = size.x - 1;
        for (int y = 0; y < size.y; y++)
        {
            Vector2Int localOutside = new Vector2Int(rightX, y);
            result.Add(GetLocalPositionForCell(localOutside, size));
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