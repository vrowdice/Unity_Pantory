using UnityEngine;

/// <summary>
/// 월드(3D) 오브젝트를 UI 포커스용 스크린 좌표/크기로 투영합니다.
/// </summary>
public static class TutorialFocusProjection
{
    public static bool TryProjectToScreen(
        Transform worldTarget,
        Camera worldCamera,
        RectTransform focusPanel,
        float defaultWorldSize,
        float defaultScreenSize,
        out Vector3 screenCenter,
        out Vector2 screenSize)
    {
        screenCenter = Vector3.zero;
        screenSize = Vector2.zero;

        if (worldTarget == null || focusPanel == null)
            return false;

        if (worldCamera == null)
            worldCamera = Camera.main;

        if (worldCamera == null)
            return false;

        Bounds bounds = GetWorldBounds(worldTarget, defaultScreenSize);
        if (!TryGetScreenBounds(worldCamera, bounds, out Rect screenBounds))
            return false;

        screenCenter = new Vector3(screenBounds.center.x, screenBounds.center.y, 0f);
        screenSize = screenBounds.size;

        if (screenSize.x < 8f)
            screenSize.x = defaultScreenSize;
        if (screenSize.y < 8f)
            screenSize.y = defaultScreenSize;

        return true;
    }

    public static void ApplyScreenRectToFocusPanel(
        RectTransform focusPanel,
        Vector3 screenCenter,
        Vector2 screenSize)
    {
        focusPanel.position = screenCenter;

        float scaleX = Mathf.Approximately(focusPanel.lossyScale.x, 0f) ? 1f : focusPanel.lossyScale.x;
        float scaleY = Mathf.Approximately(focusPanel.lossyScale.y, 0f) ? 1f : focusPanel.lossyScale.y;
        focusPanel.sizeDelta = new Vector2(screenSize.x / scaleX, screenSize.y / scaleY);
    }

    private static Bounds GetWorldBounds(Transform target, float defaultWorldSize)
    {
        Bounds? merged = null;

        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || !renderer.enabled)
                continue;

            if (merged == null)
                merged = renderer.bounds;
            else
                merged.Value.Encapsulate(renderer.bounds);
        }

        if (merged.HasValue)
            return merged.Value;

        Collider[] colliders = target.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider collider = colliders[i];
            if (collider == null || !collider.enabled)
                continue;

            if (merged == null)
                merged = collider.bounds;
            else
                merged.Value.Encapsulate(collider.bounds);
        }

        if (merged.HasValue)
            return merged.Value;

        float half = defaultWorldSize * 0.5f;
        return new Bounds(target.position, new Vector3(defaultWorldSize, defaultWorldSize, defaultWorldSize));
    }

    private static bool TryGetScreenBounds(Camera camera, Bounds worldBounds, out Rect screenBounds)
    {
        screenBounds = default;

        Vector3 center = worldBounds.center;
        Vector3 extents = worldBounds.extents;
        Vector3[] corners =
        {
            center + new Vector3(extents.x, extents.y, extents.z),
            center + new Vector3(extents.x, extents.y, -extents.z),
            center + new Vector3(extents.x, -extents.y, extents.z),
            center + new Vector3(extents.x, -extents.y, -extents.z),
            center + new Vector3(-extents.x, extents.y, extents.z),
            center + new Vector3(-extents.x, extents.y, -extents.z),
            center + new Vector3(-extents.x, -extents.y, extents.z),
            center + new Vector3(-extents.x, -extents.y, -extents.z)
        };

        bool anyInFront = false;
        float minX = float.MaxValue;
        float minY = float.MaxValue;
        float maxX = float.MinValue;
        float maxY = float.MinValue;

        for (int i = 0; i < corners.Length; i++)
        {
            Vector3 screen = camera.WorldToScreenPoint(corners[i]);
            if (screen.z <= 0f)
                continue;

            anyInFront = true;
            minX = Mathf.Min(minX, screen.x);
            minY = Mathf.Min(minY, screen.y);
            maxX = Mathf.Max(maxX, screen.x);
            maxY = Mathf.Max(maxY, screen.y);
        }

        if (!anyInFront)
            return false;

        screenBounds = Rect.MinMaxRect(minX, minY, maxX, maxY);
        return true;
    }
}
