using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 2D 환경에서 메인 카메라의 드래그 이동, 줌(Zoom-to-Cursor), 이동 경계 제한 기능을 관리합니다.
/// </summary>
public class MainCameraController : MonoBehaviour
{
    [Header("Drag Settings")]
    [SerializeField] private float _dragSpeed = 1f;

    [Header("Zoom Settings")]
    [SerializeField] private float _zoomSpeed = 1f;
    [SerializeField] private float _minZoom = 2f;
    [SerializeField] private float _maxZoom = 20f;

    [Header("Boundary Settings")]
    [SerializeField] private BoxCollider2D _boundaryCollider;

    public Camera Camera => _camera;

    private Camera _camera;
    private Vector3 _dragOrigin;
    private bool _isDragging;
    private bool _isDragEnabled = true;

    /// <summary>
    /// 외부 매니저 등을 통해 카메라 컴포넌트를 초기화합니다.
    /// </summary>
    public void OnInitialize()
    {
        _camera = GetComponent<Camera>();
    }

    /// <summary>
    /// 카메라의 드래그 기능을 활성화하거나 비활성화합니다.
    /// </summary>
    /// <param name="enabled">활성화 여부</param>
    public void SetDragEnabled(bool enabled)
    {
        _isDragEnabled = enabled;
        if (!enabled) _isDragging = false;
    }

    private void Update()
    {
        HandleDrag();
        HandleZoom();
    }

    /// <summary>
    /// 터치 또는 마우스 입력을 감지하여 카메라를 이동시킵니다.
    /// </summary>
    private void HandleDrag()
    {
        if (!_isDragEnabled) return;

        // 1. 입력 처리 (모바일/PC 공용 로직)
        if (Input.touchCount > 0)
        {
            ProcessTouchInput();
        }
        else
        {
            ProcessMouseInput();
        }
    }

    private void ProcessTouchInput()
    {
        Touch touch = Input.GetTouch(0);

        if (touch.phase == TouchPhase.Began)
        {
            if (IsPointerOverUI()) return;
            _dragOrigin = _camera.ScreenToWorldPoint(touch.position);
            _isDragging = true;
        }
        else if (touch.phase == TouchPhase.Moved && _isDragging)
        {
            MoveCamera(touch.position);
        }
        else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
        {
            _isDragging = false;
        }
    }

    private void ProcessMouseInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (IsPointerOverUI()) return;
            _dragOrigin = _camera.ScreenToWorldPoint(Input.mousePosition);
            _isDragging = true;
        }

        if (Input.GetMouseButton(0) && _isDragging)
        {
            MoveCamera(Input.mousePosition);
        }

        if (Input.GetMouseButtonUp(0))
        {
            _isDragging = false;
        }
    }

    /// <summary>
    /// 특정 입력 좌표를 바탕으로 카메라 위치를 갱신합니다.
    /// </summary>
    private void MoveCamera(Vector3 screenPosition)
    {
        Vector3 currentWorldPos = _camera.ScreenToWorldPoint(screenPosition);
        Vector3 difference = _dragOrigin - currentWorldPos;

        Vector3 newPosition = transform.position + (difference * _dragSpeed);
        newPosition.z = transform.position.z;

        if (_boundaryCollider != null)
        {
            newPosition = ClampToBounds(newPosition);
        }

        transform.position = newPosition;
        _dragOrigin = _camera.ScreenToWorldPoint(screenPosition);
    }

    /// <summary>
    /// 마우스 휠 입력을 감지하여 커서 방향으로 줌 인/아웃을 수행합니다.
    /// </summary>
    private void HandleZoom()
    {
        float scrollInput = Input.mouseScrollDelta.y;

        if (scrollInput == 0 || IsPointerOverUI() || !IsMouseInViewport()) return;

        Vector3 mouseWorldPosBefore = _camera.ScreenToWorldPoint(Input.mousePosition);

        // Orthographic 줌 적용
        _camera.orthographicSize = Mathf.Clamp(_camera.orthographicSize - (scrollInput * _zoomSpeed), _minZoom, _maxZoom);

        Vector3 mouseWorldPosAfter = _camera.ScreenToWorldPoint(Input.mousePosition);

        // Zoom-to-Cursor: 줌 이후 마우스 월드 좌표 차이만큼 카메라 보정
        Vector3 offset = mouseWorldPosBefore - mouseWorldPosAfter;
        Vector3 newPosition = transform.position + offset;
        newPosition.z = transform.position.z;

        if (_boundaryCollider != null)
        {
            newPosition = ClampToBounds(newPosition);
        }

        transform.position = newPosition;
    }

    /// <summary>
    /// 카메라의 중심 좌표가 설정된 BoxCollider2D 영역을 벗어나지 않도록 제한합니다.
    /// </summary>
    private Vector3 ClampToBounds(Vector3 position)
    {
        Bounds bounds = _boundaryCollider.bounds;

        position.x = Mathf.Clamp(position.x, bounds.min.x, bounds.max.x);
        position.y = Mathf.Clamp(position.y, bounds.min.y, bounds.max.y);

        return position;
    }

    /// <summary>
    /// 현재 포인터(마우스/터치)가 UI 요소 위에 있는지 확인합니다.
    /// </summary>
    private bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    /// <summary>
    /// 마우스 커서가 게임 화면(Viewport) 내부에 위치하는지 확인합니다.
    /// </summary>
    private bool IsMouseInViewport()
    {
        Vector3 mousePos = Input.mousePosition;
        return mousePos.x >= 0 && mousePos.x <= Screen.width &&
               mousePos.y >= 0 && mousePos.y <= Screen.height;
    }
}