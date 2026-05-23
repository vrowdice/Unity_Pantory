using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 자원 흐름 연출(FX). 시뮬레이션과 분리되며, 화면 밖에서는 생성하지 않습니다.
/// </summary>
public static class ResourceFlowFx
{
    public static readonly Vector3 IconWorldOffset = new Vector3(0f, 0f, -1f);

    private const float VisibilityMargin = 0.75f;
    private const float IconSize = 48f;
    private const float WorldIconScale = 0.02f;
    private const float ToWarehouseDuration = 0.5f;
    private const float FromWarehouseDuration = 0.5f;
    private const float MinTransitDuration = 0.28f;
    private const float MaxTransitDuration = 0.55f;
    private const float TransitDurationPerUnit = 0.18f;
    private const float BuildingBaseYOffset = 0.4f;
    private const float WarehouseFxHeight = 0.85f;

    public static bool IsWorldPointVisible(Vector3 worldPos, float margin = VisibilityMargin)
    {
        Camera camera = GetMainCamera();
        if (camera == null)
            return true;

        if (!camera.orthographic)
        {
            Vector3 viewport = camera.WorldToViewportPoint(worldPos);
            return viewport.z > 0f
                   && viewport.x >= 0f && viewport.x <= 1f
                   && viewport.y >= 0f && viewport.y <= 1f;
        }

        float halfHeight = camera.orthographicSize + margin;
        float halfWidth = halfHeight * camera.aspect;
        Vector3 center = camera.transform.position;

        return worldPos.x >= center.x - halfWidth && worldPos.x <= center.x + halfWidth
               && worldPos.y >= center.y - halfHeight && worldPos.y <= center.y + halfHeight;
    }

    public static bool IsTransitVisible(Vector3 fromWorld, Vector3 toWorld, float margin = VisibilityMargin)
    {
        return IsWorldPointVisible(fromWorld, margin)
               || IsWorldPointVisible(toWorld, margin)
               || IsWorldPointVisible((fromWorld + toWorld) * 0.5f, margin);
    }

    public static bool TryGetNodeWorldPosition(IResourceNode node, out Vector3 worldPos)
    {
        worldPos = default;
        if (node is MonoBehaviour behaviour)
        {
            worldPos = behaviour.transform.position;
            return true;
        }

        return false;
    }

    public static void TryPlayToWarehouse(string resourceId, Vector3 buildingWorldPos)
    {
        if (!IsWorldPointVisible(buildingWorldPos))
            return;

        Sprite icon = GetResourceIcon(resourceId);
        if (icon == null)
            return;

        Vector3 startWorld = buildingWorldPos + new Vector3(0f, BuildingBaseYOffset, IconWorldOffset.z);
        Vector3 endWorld = startWorld + new Vector3(0f, WarehouseFxHeight, 0f);
        PlayWarehouseMoveFx(icon, startWorld, endWorld, fadeOut: true, fadeIn: false, ToWarehouseDuration);
    }

    public static void TryPlayToWarehouse(ResourceData resource, Vector3 buildingWorldPos)
    {
        if (resource == null || resource.icon == null)
            return;

        if (!IsWorldPointVisible(buildingWorldPos))
            return;

        Vector3 startWorld = buildingWorldPos + new Vector3(0f, BuildingBaseYOffset, IconWorldOffset.z);
        Vector3 endWorld = startWorld + new Vector3(0f, WarehouseFxHeight, 0f);
        PlayWarehouseMoveFx(resource.icon, startWorld, endWorld, fadeOut: true, fadeIn: false, ToWarehouseDuration);
    }

    public static void TryPlayFromWarehouse(ResourceData resource, Vector3 buildingWorldPos)
    {
        if (resource == null || resource.icon == null)
            return;

        if (!IsWorldPointVisible(buildingWorldPos))
            return;

        Vector3 endWorld = buildingWorldPos + new Vector3(0f, BuildingBaseYOffset, IconWorldOffset.z);
        Vector3 startWorld = endWorld + new Vector3(0f, WarehouseFxHeight, 0f);
        PlayWarehouseMoveFx(resource.icon, startWorld, endWorld, fadeOut: true, fadeIn: true, FromWarehouseDuration);
    }

    public static void TryPlayFromWarehouse(string resourceId, Vector3 buildingWorldPos)
    {
        if (!IsWorldPointVisible(buildingWorldPos))
            return;

        Sprite icon = GetResourceIcon(resourceId);
        if (icon == null)
            return;

        Vector3 endWorld = buildingWorldPos + new Vector3(0f, BuildingBaseYOffset, IconWorldOffset.z);
        Vector3 startWorld = endWorld + new Vector3(0f, WarehouseFxHeight, 0f);
        PlayWarehouseMoveFx(icon, startWorld, endWorld, fadeOut: true, fadeIn: true, FromWarehouseDuration);
    }

    public static void TryPlayTransit(string resourceId, Vector3 fromWorld, Vector3 toWorld)
    {
        if (!IsTransitVisible(fromWorld, toWorld))
            return;

        Sprite icon = GetResourceIcon(resourceId);
        if (icon == null)
            return;

        float distance = Vector2.Distance(
            new Vector2(fromWorld.x, fromWorld.y),
            new Vector2(toWorld.x, toWorld.y));
        float duration = GetTransitDuration(fromWorld, toWorld);
        PlayTransitFx(icon, fromWorld, toWorld, duration);
    }

    public static void TryPlayNodeTransit(string resourceId, MonoBehaviour fromNode, IResourceNode toNode)
    {
        if (fromNode == null || string.IsNullOrEmpty(resourceId))
            return;

        Vector3 fromWorld = fromNode.transform.position + IconWorldOffset;
        if (!TryGetNodeWorldPosition(toNode, out Vector3 toPos))
            return;

        Vector3 toWorld = toPos + IconWorldOffset;
        TryPlayTransit(resourceId, fromWorld, toWorld);
    }

    /// <summary>
    /// 이미 표시 중인 ProductionInfoImage 등 UI를 목적지까지 이동시킵니다. 화면 밖이면 false.
    /// </summary>
    public static bool TryAnimateExistingIconTransit(RectTransform iconRect, Vector3 toWorld, System.Action onComplete, bool fadeOut = false)
    {
        if (iconRect == null)
            return false;

        Vector3 fromWorld = iconRect.position;
        if (!IsTransitVisible(fromWorld, toWorld))
            return false;

        float duration = GetTransitDuration(fromWorld, toWorld);
        Sequence sequence = DOTween.Sequence();
        sequence.SetLink(iconRect.gameObject);
        sequence.Append(iconRect.DOMove(toWorld, duration).SetEase(Ease.Linear));

        if (fadeOut)
        {
            CanvasGroup group = iconRect.GetComponent<CanvasGroup>();
            if (group == null)
                group = iconRect.gameObject.AddComponent<CanvasGroup>();

            group.alpha = 1f;
            group.interactable = false;
            group.blocksRaycasts = false;

            float fadeStart = duration * 0.45f;
            sequence.Insert(fadeStart, group.DOFade(0f, duration - fadeStart));
        }

        sequence.OnComplete(() => onComplete?.Invoke());
        return true;
    }

    public static float GetTransitDuration(Vector3 fromWorld, Vector3 toWorld)
    {
        float distance = Vector2.Distance(
            new Vector2(fromWorld.x, fromWorld.y),
            new Vector2(toWorld.x, toWorld.y));
        return Mathf.Clamp(distance * TransitDurationPerUnit, MinTransitDuration, MaxTransitDuration);
    }

    /// <summary>
    /// 도로 보유 아이콘 수신 연출. 상태는 즉시 반영하고, 짧은 펄스만 재생합니다(배속과 무관).
    /// </summary>
    public static void TryPulseHeldIconContainer(GameObject container, Vector3 worldAnchor)
    {
        if (container == null || !IsWorldPointVisible(worldAnchor))
            return;

        Transform iconTransform = container.transform;
        iconTransform.DOKill(false);
        Vector3 baseScale = iconTransform.localScale;
        iconTransform
            .DOPunchScale(baseScale * 0.18f, 0.1f, 4, 0.35f)
            .SetUpdate(true)
            .SetLink(container);
    }

    private static Camera GetMainCamera()
    {
        GameManager gameManager = GameManager.Instance;
        if (gameManager != null && gameManager.MainCameraController != null)
            return gameManager.MainCameraController.Camera;

        return Camera.main;
    }

    private static Sprite GetResourceIcon(string resourceId)
    {
        if (string.IsNullOrEmpty(resourceId))
            return null;

        DataManager dataManager = DataManager.Instance;
        if (dataManager == null)
            return null;

        ResourceEntry entry = dataManager.Resource.GetResourceEntry(resourceId);
        return entry?.data?.icon;
    }

    private static RectTransform GetWorldCanvasRoot()
    {
        GameManager gameManager = GameManager.Instance;
        if (gameManager == null)
            return null;

        return gameManager.GetWorldCanvas();
    }

    private static void PlayWarehouseMoveFx(
        Sprite icon,
        Vector3 startWorld,
        Vector3 endWorld,
        bool fadeOut,
        bool fadeIn,
        float duration)
    {
        RectTransform root = GetWorldCanvasRoot();
        if (root == null)
            return;

        GameObject fxObject = new GameObject("ResourceWarehouseFx");
        RectTransform rectTransform = fxObject.AddComponent<RectTransform>();
        rectTransform.SetParent(root, false);
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = new Vector2(IconSize, IconSize);
        rectTransform.localScale = Vector3.one * WorldIconScale;
        rectTransform.position = startWorld;
        rectTransform.rotation = Quaternion.identity;

        Image image = fxObject.AddComponent<Image>();
        image.sprite = icon;
        image.raycastTarget = false;
        image.preserveAspect = true;

        CanvasGroup group = fxObject.AddComponent<CanvasGroup>();
        group.alpha = fadeIn ? 0f : 1f;
        group.interactable = false;
        group.blocksRaycasts = false;

        Sequence sequence = DOTween.Sequence();
        sequence.SetLink(fxObject);
        sequence.Append(rectTransform.DOMove(endWorld, duration).SetEase(fadeIn ? Ease.InQuad : Ease.OutQuad));

        if (fadeIn)
            sequence.Join(group.DOFade(1f, duration * 0.35f));

        if (fadeOut)
        {
            float fadeStart = fadeIn ? duration * 0.65f : duration * 0.45f;
            float fadeLength = duration - fadeStart;
            sequence.Insert(fadeStart, group.DOFade(0f, fadeLength));
        }

        sequence.OnComplete(() =>
        {
            if (fxObject != null)
                Object.Destroy(fxObject);
        });
    }

    private static void PlayTransitFx(Sprite icon, Vector3 fromWorld, Vector3 toWorld, float duration)
    {
        RectTransform root = GetWorldCanvasRoot();
        if (root == null)
            return;

        GameObject fxObject = new GameObject("ResourceTransitFx");
        RectTransform rectTransform = fxObject.AddComponent<RectTransform>();
        rectTransform.SetParent(root, false);
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = new Vector2(IconSize, IconSize);
        rectTransform.localScale = Vector3.one * WorldIconScale;
        rectTransform.position = fromWorld;
        rectTransform.rotation = Quaternion.identity;

        Image image = fxObject.AddComponent<Image>();
        image.sprite = icon;
        image.raycastTarget = false;
        image.preserveAspect = true;

        CanvasGroup group = fxObject.AddComponent<CanvasGroup>();
        group.alpha = 1f;
        group.interactable = false;
        group.blocksRaycasts = false;

        Tween moveTween = rectTransform
            .DOMove(toWorld, duration)
            .SetEase(Ease.Linear)
            .SetLink(fxObject);

        moveTween.OnComplete(() =>
        {
            if (fxObject != null)
                Object.Destroy(fxObject);
        });
    }
}
