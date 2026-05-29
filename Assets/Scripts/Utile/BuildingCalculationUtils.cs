using System.Collections.Generic;
using UnityEngine;

public enum OutputIndicatorEdge
{
    Right,
    Down,
    Left,
    Up
}

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
    /// 인디케이터용 — 오른쪽 면에 여러 개 배치(건물·도로 기본 출력).
    /// </summary>
    public static List<Vector3> GetOutputLocalPositions(Vector2Int size)
    {
        List<Vector3> result = new List<Vector3>();
        for (int y = 0; y < size.y; y++)
            result.Add(GetOutputIndicatorLocalPosition(size, OutputIndicatorEdge.Right, y));
        return result;
    }

    /// <summary>
    /// OutputObj 루트 앵커. ViewObj 로컬 x=0.5 로 오른쪽 시각 오프셋 — 여기서는 Y(또는 X)만 조정하고 Z 회전으로 면을 맞춥니다.
    /// </summary>
    public static Vector3 GetOutputIndicatorLocalPosition(Vector2Int size, OutputIndicatorEdge edge, int spanIndex = 0)
    {
        spanIndex = Mathf.Clamp(spanIndex, 0, GetOutputIndicatorSpanCount(size, edge) - 1);
        float centerX = (size.x - 1) * 0.5f;
        float centerY = (size.y - 1) * 0.5f;

        switch (edge)
        {
            case OutputIndicatorEdge.Right:
            case OutputIndicatorEdge.Left:
                return new Vector3(0f, -(spanIndex + 0.5f) + centerY + 0.5f, 0f);
            case OutputIndicatorEdge.Down:
            case OutputIndicatorEdge.Up:
                return new Vector3((spanIndex + 0.5f) - centerX - 0.5f, 0f, 0f);
            default:
                return Vector3.zero;
        }
    }

    public static int GetOutputIndicatorSpanCount(Vector2Int size, OutputIndicatorEdge edge)
    {
        switch (edge)
        {
            case OutputIndicatorEdge.Right:
            case OutputIndicatorEdge.Left:
                return size.y;
            case OutputIndicatorEdge.Down:
            case OutputIndicatorEdge.Up:
                return size.x;
            default:
                return 1;
        }
    }

    /// <summary>인디케이터 스프라이트 기본(오른쪽) 기준, 면 바깥을 가리키는 Z 회전.</summary>
    public static float GetOutputIndicatorLocalRotationZ(OutputIndicatorEdge edge)
    {
        switch (edge)
        {
            case OutputIndicatorEdge.Right: return 0f;
            case OutputIndicatorEdge.Down: return -90f;
            case OutputIndicatorEdge.Left: return 180f;
            case OutputIndicatorEdge.Up: return 90f;
            default: return 0f;
        }
    }

    public static Vector3 GetLocalPositionForCell(Vector2Int cell, Vector2Int size)
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