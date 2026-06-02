using DG.Tweening;
using UnityEngine;

/// <summary>
/// 건물 스프라이트 연출. 시뮬레이션과 분리되며 화면 밖에서는 재생하지 않습니다.
/// </summary>
public static class BuildingVisualFx
{
    private const float ProductionPunchScaleFactor = 0.16f;
    private const float ProductionPunchDuration = 0.28f;

    public static void TryPlayProductionPulse(SpriteRenderer viewRenderer, Vector3 worldPosition)
    {
        if (viewRenderer == null)
            return;

        if (!ResourceFlowFx.IsWorldPointVisible(worldPosition))
            return;

        Transform visualTransform = viewRenderer.transform;
        visualTransform.DOKill(false);

        Vector3 baseScale = visualTransform.localScale;
        visualTransform.localScale = baseScale;
        visualTransform
            .DOPunchScale(baseScale * ProductionPunchScaleFactor, ProductionPunchDuration, 10, 0.65f)
            .SetUpdate(true)
            .SetLink(viewRenderer.gameObject);
    }
}
