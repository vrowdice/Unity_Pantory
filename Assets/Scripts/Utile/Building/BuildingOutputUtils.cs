using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public enum OutputIndicatorEdge
{
    Right,
    Down,
    Left,
    Up
}

/// <summary>
/// 건물·도로 출구 그리드 좌표와 OutputObj 인디케이터 배치.
/// <see cref="BuildingData.outputOffset"/> 은 에셋 OnValidate 로만 보정합니다.
/// </summary>
public static class BuildingOutputUtils
{
    private static readonly Vector2Int[] PassThroughNeighborDeltas =
    {
        new Vector2Int(0, -1),
        new Vector2Int(1, 0),
        new Vector2Int(0, 1),
        new Vector2Int(-1, 0)
    };

    // —— 그리드 출구 ——

    public static Vector2Int GetOutputGridPosition(
        Vector2Int origin,
        Vector2Int size,
        int rotation,
        BuildingData buildingData) =>
        GetOutputGridPosition(origin, size, rotation, buildingData.outputOffset);

    public static Vector2Int GetOutputGridPosition(
        Vector2Int origin,
        Vector2Int size,
        int rotation,
        Vector2Int localOutputOffset)
    {
        Vector2Int rotatedLocal = RotateCellAroundFootprintCenter(localOutputOffset, rotation, size);
        return origin + rotatedLocal;
    }

    public static void CollectRoadOutputCells(
        RoadBuildingData roadData,
        Vector2Int gridOrigin,
        int rotation,
        List<Vector2Int> results)
    {
        results.Clear();
        if (roadData == null)
            return;

        Vector2Int size = Vector2Int.one;

        if (roadData.passThroughNeighbors)
        {
            for (int i = 0; i < PassThroughNeighborDeltas.Length; i++)
                results.Add(gridOrigin + PassThroughNeighborDeltas[i]);
            return;
        }

        results.Add(GetOutputGridPosition(gridOrigin, size, rotation, roadData));

        if (roadData.hasSecondaryOutput)
            results.Add(GetOutputGridPosition(gridOrigin, size, rotation, roadData.secondaryOutputOffset));
    }

    // —— 인디케이터 ——

    public static GameObject SpawnIndicator(
        Transform parent,
        GameObject prefab,
        Vector2Int footprintSize,
        BuildingData buildingData)
    {
        if (prefab == null || parent == null || buildingData == null)
            return null;

        GameObject instance = Object.Instantiate(prefab, parent);
        ApplyIndicatorFromOutputOffset(instance.transform, footprintSize, buildingData.outputOffset);
        return instance;
    }

    public static void ApplyIndicatorFromOutputOffset(
        Transform indicator,
        Vector2Int footprintSize,
        Vector2Int localOutputOffset)
    {
        if (indicator == null)
            return;

        OutputIndicatorEdge edge = GetEdgeForLocalOutputOffset(footprintSize, localOutputOffset);
        int spanIndex = GetSpanIndex(footprintSize, localOutputOffset, edge);

        indicator.localPosition = GetLocalPositionOnFootprintEdge(footprintSize, edge, spanIndex);
        indicator.localRotation = Quaternion.Euler(0f, 0f, GetIndicatorRotationZ(edge));
    }

    public static OutputIndicatorEdge EdgeFromSplitterFlow(FlowDirection direction, FlowDirection downFlow) =>
        direction == downFlow ? OutputIndicatorEdge.Down : OutputIndicatorEdge.Right;

    public static float GetSplitterArrowRotationZ(OutputIndicatorEdge edge) =>
        edge == OutputIndicatorEdge.Down ? -90f : 0f;

    public static float GetIndicatorRotationZ(OutputIndicatorEdge edge)
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

    public static void TweenIndicatorRotationZ(
        Transform target,
        float rotationZ,
        bool immediate,
        float duration,
        ref Tween activeTween)
    {
        if (target == null)
            return;

        activeTween?.Kill();
        if (immediate)
        {
            target.localRotation = Quaternion.Euler(0f, 0f, rotationZ);
            return;
        }

        activeTween = target
            .DOLocalRotate(new Vector3(0f, 0f, rotationZ), duration)
            .SetEase(Ease.OutQuad);
    }

    private static Vector2Int RotateCellAroundFootprintCenter(Vector2Int cell, int rotation, Vector2Int size)
    {
        rotation = rotation % 4;
        if (rotation == 0)
            return cell;

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

    private static OutputIndicatorEdge GetEdgeForLocalOutputOffset(Vector2Int size, Vector2Int localOffset)
    {
        if (localOffset.x >= size.x)
            return OutputIndicatorEdge.Right;
        if (localOffset.x < 0)
            return OutputIndicatorEdge.Left;
        if (localOffset.y >= size.y)
            return OutputIndicatorEdge.Down;
        return OutputIndicatorEdge.Up;
    }

    private static int GetSpanIndex(Vector2Int size, Vector2Int localOutputOffset, OutputIndicatorEdge edge)
    {
        switch (edge)
        {
            case OutputIndicatorEdge.Down:
            case OutputIndicatorEdge.Up:
                return Mathf.Clamp(localOutputOffset.x, 0, Mathf.Max(0, size.x - 1));
            default:
                return Mathf.Clamp(localOutputOffset.y, 0, Mathf.Max(0, size.y - 1));
        }
    }

    private static Vector3 GetLocalPositionOnFootprintEdge(Vector2Int size, OutputIndicatorEdge edge, int spanIndex)
    {
        Vector3 position = GetLocalPositionOnTileEdge(size, edge, spanIndex);
        switch (edge)
        {
            case OutputIndicatorEdge.Left:
                position.x -= (size.x - 1) * 0.5f;
                break;
            case OutputIndicatorEdge.Down:
                position.y -= (size.y - 1) * 0.5f;
                break;
            case OutputIndicatorEdge.Up:
                position.y += (size.y - 1) * 0.5f;
                break;
            default:
                position.x += (size.x - 1) * 0.5f;
                break;
        }

        return position;
    }

    private static Vector3 GetLocalPositionOnTileEdge(Vector2Int size, OutputIndicatorEdge edge, int spanIndex)
    {
        int maxSpan = edge == OutputIndicatorEdge.Down || edge == OutputIndicatorEdge.Up ? size.x : size.y;
        spanIndex = Mathf.Clamp(spanIndex, 0, Mathf.Max(0, maxSpan - 1));

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
}
