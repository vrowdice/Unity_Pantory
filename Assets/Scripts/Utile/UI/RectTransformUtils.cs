using UnityEngine;
using UnityEngine.UI;

public static class RectTransformUtils
{
    /// <summary>
    /// focus의 위치·크기를 target의 화면상 bounds와 맞춥니다. (stretch 레이아웃·다른 캔버스 대응)
    /// </summary>
    public static void SyncUiFocusToTarget(RectTransform focus, RectTransform target)
    {
        if (focus == null || target == null)
            return;

        LayoutRebuilder.ForceRebuildLayoutImmediate(target);
        Canvas.ForceUpdateCanvases();

        Rect worldRect = GetWorldRect(target);
        float worldWidth = Mathf.Max(worldRect.width, 32f);
        float worldHeight = Mathf.Max(worldRect.height, 32f);

        float focusScaleX = Mathf.Approximately(focus.lossyScale.x, 0f) ? 1f : Mathf.Abs(focus.lossyScale.x);
        float focusScaleY = Mathf.Approximately(focus.lossyScale.y, 0f) ? 1f : Mathf.Abs(focus.lossyScale.y);

        focus.sizeDelta = new Vector2(
            worldWidth / focusScaleX,
            worldHeight / focusScaleY);

        Vector3 center = worldRect.center;
        center.z = focus.position.z;
        focus.position = center;
    }

    /// <summary>
    /// focus의 크기를 target과 동기화합니다. 서로 다른 캔버스여도 화면상 크기가 맞도록 스케일을 보정합니다.
    /// </summary>
    public static void SyncSizeToTarget(RectTransform focus, RectTransform target)
    {
        SyncUiFocusToTarget(focus, target);
    }

    public static Rect GetWorldRect(RectTransform rectTransform)
    {
        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);
        float minX = corners[0].x;
        float minY = corners[0].y;
        float maxX = corners[0].x;
        float maxY = corners[0].y;

        for (int i = 1; i < corners.Length; i++)
        {
            minX = Mathf.Min(minX, corners[i].x);
            minY = Mathf.Min(minY, corners[i].y);
            maxX = Mathf.Max(maxX, corners[i].x);
            maxY = Mathf.Max(maxY, corners[i].y);
        }

        return Rect.MinMaxRect(minX, minY, maxX, maxY);
    }
}
