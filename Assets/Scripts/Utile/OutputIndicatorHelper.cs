using DG.Tweening;
using UnityEngine;

/// <summary>
/// OutputObj 프리팹 배치·회전 공통 처리. 루트 Z 회전 + ViewObj(x=0.5) 오프셋.
/// </summary>
public static class OutputIndicatorHelper
{
    public static GameObject Spawn(Transform parent, GameObject prefab, Vector2Int footprintSize, OutputIndicatorEdge edge, int spanIndex = 0)
    {
        if (prefab == null || parent == null) return null;

        GameObject instance = Object.Instantiate(prefab, parent);
        ApplyLocalPlacement(instance.transform, footprintSize, edge, spanIndex);
        return instance;
    }

    public static void SpawnOnRightEdge(Transform parent, GameObject prefab, Vector2Int footprintSize)
    {
        if (prefab == null || parent == null) return;

        int count = BuildingCalculationUtils.GetOutputIndicatorSpanCount(footprintSize, OutputIndicatorEdge.Right);
        for (int i = 0; i < count; i++)
            Spawn(parent, prefab, footprintSize, OutputIndicatorEdge.Right, i);
    }

    public static void SpawnOnRightEdgeForBuilding(Transform parent, GameObject prefab, Vector2Int footprintSize)
    {
        if (prefab == null || parent == null) return;

        int count = BuildingCalculationUtils.GetOutputIndicatorSpanCount(footprintSize, OutputIndicatorEdge.Right);
        for (int i = 0; i < count; i++)
        {
            GameObject instance = Object.Instantiate(prefab, parent);
            ApplyBuildingLocalPlacement(instance.transform, footprintSize, i);
        }
    }

    public static void ApplyBuildingLocalPlacement(Transform indicator, Vector2Int footprintSize, int spanIndex = 0)
    {
        if (indicator == null) return;

        indicator.localPosition = BuildingCalculationUtils.GetBuildingOutputIndicatorLocalPosition(footprintSize, spanIndex);
        float rotationZ = BuildingCalculationUtils.GetOutputIndicatorLocalRotationZ(OutputIndicatorEdge.Right);
        indicator.localRotation = Quaternion.Euler(0f, 0f, rotationZ);
    }

    public static void ApplyLocalPlacement(Transform indicator, Vector2Int footprintSize, OutputIndicatorEdge edge, int spanIndex = 0)
    {
        if (indicator == null) return;

        indicator.localPosition = BuildingCalculationUtils.GetOutputIndicatorLocalPosition(footprintSize, edge, spanIndex);
        float rotationZ = BuildingCalculationUtils.GetOutputIndicatorLocalRotationZ(edge);
        indicator.localRotation = Quaternion.Euler(0f, 0f, rotationZ);
    }

    public static void TweenLocalPlacement(
        Transform indicator,
        Vector2Int footprintSize,
        OutputIndicatorEdge edge,
        float duration,
        int spanIndex = 0)
    {
        if (indicator == null) return;

        Vector3 targetPosition = BuildingCalculationUtils.GetOutputIndicatorLocalPosition(footprintSize, edge, spanIndex);
        float targetRotationZ = BuildingCalculationUtils.GetOutputIndicatorLocalRotationZ(edge);

        indicator.DOKill();
        indicator.DOLocalMove(targetPosition, duration).SetEase(Ease.OutQuad);
        indicator.DOLocalRotate(new Vector3(0f, 0f, targetRotationZ), duration).SetEase(Ease.OutQuad);
    }

    public static OutputIndicatorEdge GetEdgeForSplitterOutputIndex(int outputIndex) =>
        outputIndex == 1 ? OutputIndicatorEdge.Down : OutputIndicatorEdge.Right;

    public static OutputIndicatorEdge EdgeFromSplitterFlow(FlowDirection direction, FlowDirection downFlow) =>
        direction == downFlow ? OutputIndicatorEdge.Down : OutputIndicatorEdge.Right;

    public static float GetSplitterArrowRotationZ(OutputIndicatorEdge edge) =>
        edge == OutputIndicatorEdge.Down ? -90f : 0f;

    public static void SetLocalRotationZ(Transform target, float rotationZ, bool immediate, float duration, ref Tween activeTween)
    {
        if (target == null) return;

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
}
