using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// PC 마우스와 모바일 터치를 통합한 포인터 입력 유틸.
/// </summary>
public static class PointerInput
{
    private static readonly List<RaycastResult> UiRaycastResults = new List<RaycastResult>();

    public static bool IsMultiTouch => Input.touchCount >= 2;

    public static Vector2 PrimaryScreenPosition
    {
        get
        {
            if (Input.touchCount > 0)
                return Input.GetTouch(0).position;
            return Input.mousePosition;
        }
    }

    public static int PrimaryPointerId
    {
        get
        {
            if (Input.touchCount > 0)
                return Input.GetTouch(0).fingerId;
            return -1;
        }
    }

    public static bool GetPrimaryPointerDown()
    {
        if (IsMultiTouch)
            return false;

        if (Input.touchCount > 0)
            return Input.GetTouch(0).phase == TouchPhase.Began;

        return Input.GetMouseButtonDown(0);
    }

    public static bool GetPrimaryPointerHeld()
    {
        if (IsMultiTouch)
            return false;

        if (Input.touchCount > 0)
        {
            TouchPhase phase = Input.GetTouch(0).phase;
            return phase == TouchPhase.Began || phase == TouchPhase.Moved || phase == TouchPhase.Stationary;
        }

        return Input.GetMouseButton(0);
    }

    public static bool GetPrimaryPointerUp()
    {
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended)
            return true;

        if (Input.touchCount == 0 && Input.GetMouseButtonUp(0))
            return true;

        return false;
    }

    public static bool GetSecondaryPointerDown()
    {
        return Input.GetMouseButtonDown(1);
    }

    public static bool WasCancelPressed()
    {
        return GetSecondaryPointerDown();
    }

    public static bool IsPointerOverUi()
    {
        if (EventSystem.current == null)
            return false;

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                return true;

            return RaycastUiAtScreenPosition(touch.position);
        }

        return EventSystem.current.IsPointerOverGameObject();
    }

    private static bool RaycastUiAtScreenPosition(Vector2 screenPosition)
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = screenPosition;
        UiRaycastResults.Clear();
        EventSystem.current.RaycastAll(eventData, UiRaycastResults);
        return UiRaycastResults.Count > 0;
    }

    public static bool IsScreenPositionInViewport(Camera camera, Vector2 screenPosition)
    {
        if (camera == null)
            return false;

        Vector3 viewPos = camera.ScreenToViewportPoint(screenPosition);
        return viewPos.x >= 0f && viewPos.x <= 1f && viewPos.y >= 0f && viewPos.y <= 1f;
    }

    public static Vector3 ScreenToWorldOnPlane(Camera camera, Vector2 screenPosition, float planeZ = 0f)
    {
        Vector3 screenPoint = new Vector3(screenPosition.x, screenPosition.y, 0f);
        Vector3 world = camera.ScreenToWorldPoint(screenPoint);
        world.z = planeZ;
        return world;
    }
}
