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

    [Header("Construction feedback (DOTween)")]
    [SerializeField] private float _constructionShakeDuration = 0.22f;
    [SerializeField] private Vector3 _constructionShakeStrength = new Vector3(0.12f, 0.12f, 0f);
    [SerializeField] private int _constructionShakeVibrato = 14;
    [SerializeField] private float _constructionShakeRandomness = 90f;

    public Camera Camera => _camera;

    private Camera _camera;
    private Vector3 _dragOrigin;
    private bool _isDragging;
    private MainRunner _mainRunner;

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

    /// <summary>
    /// 건물 건설 등 짧은 피드백용. XY만 흔들고 Z는 유지(orthographic 2D 기준).
    /// </summary>
    public void ShakeForConstruction()
    {
        transform.DOKill(false);
        transform.DOShakePosition(
                _constructionShakeDuration,
                _constructionShakeStrength,
                _constructionShakeVibrato,
                _constructionShakeRandomness,
                false,
                true,
                ShakeRandomnessMode.Full)
            .SetUpdate(UpdateType.Normal, isIndependentUpdate: true)
            .SetLink(gameObject);
    }

    public void ShakeCamera() => ShakeForConstruction();

    private void LateUpdate()
    {
        HandleInput();
        HandleZoom();
    }

    private void HandleInput()
    {
        if (!_isDragEnabled) return;
        if (IsContinuousPlacementInProgress())
        {
            _isDragging = false;
            return;
        }

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

    private bool IsContinuousPlacementInProgress()
    {
        if (_mainRunner == null)
            _mainRunner = FindAnyObjectByType<MainRunner>();
        if (_mainRunner == null)
            return false;
        if (_mainRunner.BlueprintHandler != null && _mainRunner.BlueprintHandler.IsBlockingCameraDrag)
            return true;
        if (_mainRunner.PlacementHandler == null)
            return false;
        if (!_mainRunner.PlacementHandler.IsPlacementMode)
            return false;
        return _mainRunner.PlacementHandler.IsPointerPlacementActive;
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