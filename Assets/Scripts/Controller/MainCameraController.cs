using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

[RequireComponent(typeof(Camera))]
public class MainCameraController : MonoBehaviour
{
    [Header("Drag Settings")]
    [SerializeField] private float _dragSpeed = 1f;
    [SerializeField] private bool _isDragEnabled = true;

    [Header("Zoom Settings")]
    [SerializeField] private float _zoomSpeed = 5f;
    [SerializeField] private float _minZoom = 2f;
    [SerializeField] private float _maxZoom = 20f;

    [Header("Boundary Settings")]
    [SerializeField] private BoxCollider2D _boundaryCollider;

    public Camera Camera => _camera;

    private Camera _camera;
    private Vector3 _dragOrigin;
    private bool _isDragging;

    private void Awake() => Init();

    public void Init() => _camera = GetComponent<Camera>();

    public void SetDragEnabled(bool isEnabled)
    {
        _isDragEnabled = isEnabled;
        if (!isEnabled) _isDragging = false;
    }

    /// <summary>
    /// 카메라 경계를 설정합니다. 그리드 영역에 맞게 카메라 이동을 제한합니다.
    /// </summary>
    /// <param name="center">경계의 중심 위치</param>
    /// <param name="size">경계의 크기 (width, height)</param>
    public void SetBoundary(Vector2 center, Vector2 size)
    {
        if (_boundaryCollider == null)
        {
            GameObject boundaryObj = new GameObject("CameraBoundary");
            boundaryObj.transform.SetParent(transform.parent);
            _boundaryCollider = boundaryObj.AddComponent<BoxCollider2D>();
            _boundaryCollider.isTrigger = true;
        }

        _boundaryCollider.transform.position = new Vector3(center.x, center.y, 0);
        _boundaryCollider.size = size;
    }

    public void ShakeCamera()
    {
        transform.DOShakePosition(0.1f, 0.1f, 10, 90, false, true);
    }

    private void LateUpdate()
    {
        HandleInput();
        HandleZoom();
    }

    private void HandleInput()
    {
        if (!_isDragEnabled) return;

        // PC/모바일 통합 입력 감지
        bool inputBegin = Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began);
        bool inputMove = Input.GetMouseButton(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Moved);
        bool inputEnd = Input.GetMouseButtonUp(0) || (Input.touchCount > 0 && (Input.GetTouch(0).phase == TouchPhase.Ended || Input.GetTouch(0).phase == TouchPhase.Canceled));

        Vector3 currentInputPos = Input.touchCount > 0 ? (Vector3)Input.GetTouch(0).position : Input.mousePosition;

        if (inputBegin)
        {
            if (IsPointerOverUI()) return;
            _dragOrigin = _camera.ScreenToWorldPoint(currentInputPos);
            _isDragging = true;
        }

        if (_isDragging && inputMove)
        {
            Vector3 currentWorldPos = _camera.ScreenToWorldPoint(currentInputPos);
            Vector3 direction = _dragOrigin - currentWorldPos;

            transform.position = ClampToBoundary(transform.position + direction * _dragSpeed);
            _dragOrigin = _camera.ScreenToWorldPoint(currentInputPos); // 드래그 원점 실시간 갱신으로 부드럽게 이동
        }

        if (inputEnd) _isDragging = false;
    }

    private void HandleZoom()
    {
        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) < 0.01f || IsPointerOverUI() || !IsMouseInViewport()) return;

        Vector3 mouseBefore = _camera.ScreenToWorldPoint(Input.mousePosition);

        _camera.orthographicSize = Mathf.Clamp(_camera.orthographicSize - (scroll * _zoomSpeed), _minZoom, _maxZoom);

        Vector3 mouseAfter = _camera.ScreenToWorldPoint(Input.mousePosition);
        Vector3 offset = mouseBefore - mouseAfter;

        transform.position = ClampToBoundary(transform.position + offset);
    }

    private Vector3 ClampToBoundary(Vector3 targetPos)
    {
        if (_boundaryCollider == null) return targetPos;

        Bounds bounds = _boundaryCollider.bounds;

        // 유저 의도: 카메라 중심점만 Collider 영역 내로 제한
        targetPos.x = Mathf.Clamp(targetPos.x, bounds.min.x, bounds.max.x);
        targetPos.y = Mathf.Clamp(targetPos.y, bounds.min.y, bounds.max.y);
        targetPos.z = transform.position.z;

        return targetPos;
    }

    private bool IsPointerOverUI() => EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

    private bool IsMouseInViewport()
    {
        Vector3 viewPos = _camera.ScreenToViewportPoint(Input.mousePosition);
        return viewPos.x >= 0 && viewPos.x <= 1 && viewPos.y >= 0 && viewPos.y <= 1;
    }
}