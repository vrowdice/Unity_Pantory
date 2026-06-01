using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public static class TutorialPanelLayout
{
    public static void MovePanelToPosition(
        RectTransform panelRect,
        Vector2 desiredPosition,
        GameObject focusTarget,
        float screenEdgePadding,
        bool animate,
        float duration = 0.35f,
        Ease ease = Ease.OutCubic,
        float focusClearance = 12f)
    {
        if (panelRect == null)
            return;

        Vector2 startPos = panelRect.anchoredPosition;
        Vector2 targetPos = ResolvePanelPosition(
            panelRect,
            desiredPosition,
            focusTarget,
            screenEdgePadding,
            focusClearance);

        if (!animate || (targetPos - startPos).sqrMagnitude < 1f)
            return;

        panelRect.anchoredPosition = startPos;
        panelRect.DOKill();
        panelRect.DOAnchorPos(targetPos, duration).SetEase(ease);
    }

    private enum FocusSide
    {
        Below,
        Above,
        Left,
        Right
    }

    public static Vector2 ResolvePanelPosition(
        RectTransform panelRect,
        Vector2 desiredPosition,
        GameObject focusTarget,
        float screenEdgePadding,
        float focusClearance = 12f)
    {
        if (panelRect == null)
            return desiredPosition;

        RectTransform parentRect = panelRect.parent as RectTransform;
        if (parentRect == null)
            return desiredPosition;

        Canvas.ForceUpdateCanvases();

        MoveBounds bounds = GetMoveBounds(panelRect, parentRect, screenEdgePadding);
        Vector2 clampedDesired = ClampToBounds(desiredPosition, bounds);

        RectTransform focusRect = focusTarget != null
            ? focusTarget.GetComponent<RectTransform>()
            : null;

        if (focusRect == null)
            return SetPanelPosition(panelRect, clampedDesired);

        if (TrySetClearPosition(panelRect, clampedDesired, focusRect, focusClearance))
            return panelRect.anchoredPosition;

        Rect panelWorld = GetWorldRect(panelRect);
        Rect focusWorld = GetWorldRect(focusRect);

        List<Vector2> candidates = BuildCandidatePositions(
            panelRect,
            bounds,
            panelWorld,
            focusWorld,
            focusClearance,
            clampedDesired);

        for (int i = 0; i < candidates.Count; i++)
        {
            Vector2 candidate = ClampToBounds(candidates[i], bounds);
            if (TrySetClearPosition(panelRect, candidate, focusRect, focusClearance))
                return panelRect.anchoredPosition;
        }

        return SetPanelPosition(panelRect, new Vector2(bounds.MinX, bounds.MaxY));
    }

    private static List<Vector2> BuildCandidatePositions(
        RectTransform panelRect,
        MoveBounds bounds,
        Rect panelWorld,
        Rect focusWorld,
        float clearance,
        Vector2 desiredPosition)
    {
        List<Vector2> candidates = new List<Vector2>(12)
        {
            desiredPosition,
            GetPositionBesideFocus(panelRect, panelWorld, focusWorld, clearance, FocusSide.Below),
            GetPositionBesideFocus(panelRect, panelWorld, focusWorld, clearance, FocusSide.Above),
            GetPositionBesideFocus(panelRect, panelWorld, focusWorld, clearance, FocusSide.Right),
            GetPositionBesideFocus(panelRect, panelWorld, focusWorld, clearance, FocusSide.Left),
            new Vector2(bounds.MinX, bounds.MaxY),
            new Vector2(bounds.MaxX, bounds.MaxY),
            new Vector2(bounds.MinX, bounds.MinY),
            new Vector2(bounds.MaxX, bounds.MinY),
            new Vector2(0f, bounds.MaxY),
            new Vector2(0f, bounds.MinY),
            new Vector2(bounds.MinX, 0f),
            new Vector2(bounds.MaxX, 0f)
        };

        return candidates;
    }

    private static Vector2 GetPositionBesideFocus(
        RectTransform panelRect,
        Rect panelWorld,
        Rect focusWorld,
        float clearance,
        FocusSide side)
    {
        Rect targetWorld = panelWorld;

        switch (side)
        {
            case FocusSide.Below:
                targetWorld.yMin = focusWorld.yMin - clearance - panelWorld.height;
                targetWorld.yMax = targetWorld.yMin + panelWorld.height;
                targetWorld.xMin = focusWorld.center.x - panelWorld.width * 0.5f;
                targetWorld.xMax = targetWorld.xMin + panelWorld.width;
                break;
            case FocusSide.Above:
                targetWorld.yMax = focusWorld.yMax + clearance + panelWorld.height;
                targetWorld.yMin = targetWorld.yMax - panelWorld.height;
                targetWorld.xMin = focusWorld.center.x - panelWorld.width * 0.5f;
                targetWorld.xMax = targetWorld.xMin + panelWorld.width;
                break;
            case FocusSide.Left:
                targetWorld.xMin = focusWorld.xMin - clearance - panelWorld.width;
                targetWorld.xMax = targetWorld.xMin + panelWorld.width;
                targetWorld.yMin = focusWorld.center.y - panelWorld.height * 0.5f;
                targetWorld.yMax = targetWorld.yMin + panelWorld.height;
                break;
            case FocusSide.Right:
                targetWorld.xMax = focusWorld.xMax + clearance + panelWorld.width;
                targetWorld.xMin = targetWorld.xMax - panelWorld.width;
                targetWorld.yMin = focusWorld.center.y - panelWorld.height * 0.5f;
                targetWorld.yMax = targetWorld.yMin + panelWorld.height;
                break;
        }

        return GetAnchoredPositionForWorldRect(panelRect, targetWorld);
    }

    private static Vector2 GetAnchoredPositionForWorldRect(RectTransform panelRect, Rect worldRect)
    {
        RectTransform parentRect = panelRect.parent as RectTransform;
        Vector3 worldCenter = new Vector3(worldRect.center.x, worldRect.center.y, panelRect.position.z);
        Vector2 localCenter = parentRect.InverseTransformPoint(worldCenter);

        Vector2 anchorCenter = (panelRect.anchorMin + panelRect.anchorMax) * 0.5f;
        if (Mathf.Approximately(anchorCenter.x, 0.5f) && Mathf.Approximately(anchorCenter.y, 0.5f))
            return localCenter;

        Vector2 parentSize = parentRect.rect.size;
        Vector2 anchorReference = new Vector2(
            (anchorCenter.x - parentRect.pivot.x) * parentSize.x,
            (anchorCenter.y - parentRect.pivot.y) * parentSize.y);
        return localCenter - anchorReference;
    }

    private static bool TrySetClearPosition(
        RectTransform panelRect,
        Vector2 anchoredPosition,
        RectTransform focusRect,
        float clearance)
    {
        SetPanelPosition(panelRect, anchoredPosition);
        return !Overlaps(panelRect, focusRect, clearance);
    }

    private static Vector2 SetPanelPosition(RectTransform panelRect, Vector2 anchoredPosition)
    {
        panelRect.anchoredPosition = anchoredPosition;
        return panelRect.anchoredPosition;
    }

    private static bool Overlaps(RectTransform panelRect, RectTransform focusRect, float clearance)
    {
        Rect panelWorld = GetWorldRect(panelRect);
        Rect focusWorld = GetWorldRect(focusRect);

        panelWorld.xMin += clearance;
        panelWorld.yMin += clearance;
        panelWorld.xMax -= clearance;
        panelWorld.yMax -= clearance;

        focusWorld.xMin += clearance;
        focusWorld.yMin += clearance;
        focusWorld.xMax -= clearance;
        focusWorld.yMax -= clearance;

        return panelWorld.Overlaps(focusWorld);
    }

    private static Rect GetWorldRect(RectTransform rectTransform)
    {
        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);
        return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
    }

    private static MoveBounds GetMoveBounds(
        RectTransform panelRect,
        RectTransform parentRect,
        float screenEdgePadding)
    {
        float parentWidth = parentRect.rect.width;
        float parentHeight = parentRect.rect.height;
        float panelWidth = panelRect.rect.width;
        float panelHeight = panelRect.rect.height;
        Vector2 pivot = panelRect.pivot;

        return new MoveBounds
        {
            MinX = -parentWidth * 0.5f + panelWidth * pivot.x + screenEdgePadding,
            MaxX = parentWidth * 0.5f - panelWidth * (1f - pivot.x) - screenEdgePadding,
            MinY = -parentHeight * 0.5f + panelHeight * pivot.y + screenEdgePadding,
            MaxY = parentHeight * 0.5f - panelHeight * (1f - pivot.y) - screenEdgePadding
        };
    }

    private static Vector2 ClampToBounds(Vector2 position, MoveBounds bounds)
    {
        if (bounds.MinX > bounds.MaxX)
            position.x = 0f;
        else
            position.x = Mathf.Clamp(position.x, bounds.MinX, bounds.MaxX);

        if (bounds.MinY > bounds.MaxY)
            position.y = 0f;
        else
            position.y = Mathf.Clamp(position.y, bounds.MinY, bounds.MaxY);

        return position;
    }

    private struct MoveBounds
    {
        public float MinX;
        public float MaxX;
        public float MinY;
        public float MaxY;
    }
}
