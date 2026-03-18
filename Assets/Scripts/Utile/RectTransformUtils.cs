using UnityEngine;

public static class RectTransformUtils
{
    /// <summary>
    /// focus의 크기를 target과 동기화합니다. 서로 다른 캔버스여도 화면상 크기가 맞도록 스케일을 보정합니다.
    /// </summary>
    public static void SyncSizeToTarget(RectTransform focus, RectTransform target)
    {
        float focusScaleX = Mathf.Approximately(focus.lossyScale.x, 0f) ? 1f : focus.lossyScale.x;
        float focusScaleY = Mathf.Approximately(focus.lossyScale.y, 0f) ? 1f : focus.lossyScale.y;
        focus.sizeDelta = new Vector2(
            target.rect.width * target.lossyScale.x / focusScaleX,
            target.rect.height * target.lossyScale.y / focusScaleY);
    }
}
