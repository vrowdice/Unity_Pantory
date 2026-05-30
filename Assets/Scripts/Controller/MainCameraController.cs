using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(Camera))]
public class MainCameraController : MonoBehaviour
{
    [Header("Drag Settings")]
    [SerializeField] private float _dragSpeed = 1f;
    [SerializeField] private bool _isDragEnabled = true;

    [Header("Keyboard Settings (PC)")]
    [SerializeField] private bool _isKeyboardMoveEnabled = true;
    [SerializeField] private float _keyboardMoveSpeed = 8f;
    [SerializeField] private float _keyboardZoomReference = 10f;

    [Header("Zoom Settings")]
    [SerializeField] private float _zoomSpeed = 5f;
    [SerializeField] private float _pinchZoomSpeed = 0.01f;
    [SerializeField] private float _minZoom = 2f;
    [SerializeField] private float _maxZoom = 20f;

    [Header("Boundary Settings")]
    [SerializeField] private BoxCollider2D _boundaryCollider;

    [Header("Construction feedback (DOTween)")]
    [SerializeField] private float _constructionShakeDuration = 0.22f;
    [SerializeField] private Vector3 _constructionShakeStrength = new Vector3(0.12f, 0.12f, 0f);
    [SerializeField] private int _constructionShakeVibrato = 14;
    [SerializeField] private float _constructionShakeRandomness = 90f;

    [Header("Removal feedback (DOTween)")]
    [SerializeField] private float _removalShakeDuration = 0.18f;
    [SerializeField] private Vector3 _removalShakeStrength = new Vector3(0.1f, 0.1f, 0f);
    [SerializeField] private int _removalShakeVibrato = 12;
    [SerializeField] private float _removalShakeRandomness = 90f;

    public Camera Camera => _camera;

    private Camera _camera;
    private Vector3 _dragOrigin;
    private bool _isDragging;
    private Vector3 _middleDragOrigin;
    private bool _isMiddleDragging;
    private BuildingSceneRunnerBase _sceneRunner;

    private void Awake()
    {
        Init();
    }

    public void Init()
    {
        _camera = GetComponent<Camera>();
    }

    public void SetDragEnabled(bool isEnabled)
    {
        _isDragEnabled = isEnabled;
        if (!isEnabled)
        {
            _isDragging = false;
            _isMiddleDragging = false;
        }
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

    /// <summary>
    /// 건물 제거 시 짧은 카메라 흔들림.
    /// </summary>
    public void ShakeForRemoval()
    {
        transform.DOKill(false);
        transform.DOShakePosition(
                _removalShakeDuration,
                _removalShakeStrength,
                _removalShakeVibrato,
                _removalShakeRandomness,
                false,
                true,
                ShakeRandomnessMode.Full)
            .SetUpdate(UpdateType.Normal, isIndependentUpdate: true)
            .SetLink(gameObject);
    }

    private void LateUpdate()
    {
        HandlePinchZoom();
        HandleKeyboardMove();
        HandleInput();
        HandleMiddleMouseDrag();
        HandleScrollZoom();
    }

    private void HandleKeyboardMove()
    {
        if (Application.isMobilePlatform)
            return;

        if (!_isKeyboardMoveEnabled)
            return;

        if (UIManager.Instance != null && UIManager.Instance.IsTypingInTextInput())
            return;

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        if (Mathf.Approximately(horizontal, 0f) && Mathf.Approximately(vertical, 0f))
            return;

        Vector3 move = new Vector3(horizontal, vertical, 0f);
        if (move.sqrMagnitude > 1f)
            move.Normalize();

        transform.position = ClampToBoundary(
            transform.position + move * (GetKeyboardMoveSpeed() * Time.deltaTime));
    }

    private float GetKeyboardMoveSpeed()
    {
        float zoomReference = Mathf.Max(0.001f, _keyboardZoomReference);
        return _keyboardMoveSpeed * (_camera.orthographicSize / zoomReference);
    }

    private void HandleInput()
    {
        if (!_isDragEnabled) return;
        if (PointerInput.IsMultiTouch)
        {
            _isDragging = false;
            return;
        }

        if (IsContinuousPlacementInProgress())
        {
            _isDragging = false;
            return;
        }

        Vector3 currentInputPos = PointerInput.PrimaryScreenPosition;

        if (PointerInput.GetPrimaryPointerDown())
        {
            if (PointerInput.IsPointerOverUi()) return;
            _dragOrigin = _camera.ScreenToWorldPoint(currentInputPos);
            _isDragging = true;
        }

        if (_isDragging && PointerInput.GetPrimaryPointerHeld())
        {
            Vector3 currentWorldPos = _camera.ScreenToWorldPoint(currentInputPos);
            Vector3 direction = _dragOrigin - currentWorldPos;

            transform.position = ClampToBoundary(transform.position + direction * _dragSpeed);
            _dragOrigin = _camera.ScreenToWorldPoint(currentInputPos);
        }

        if (PointerInput.GetPrimaryPointerUp())
            _isDragging = false;
    }

    private void HandleMiddleMouseDrag()
    {
        if (!_isDragEnabled || Application.isMobilePlatform)
            return;

        Vector3 currentInputPos = Input.mousePosition;

        if (Input.GetMouseButtonDown(2))
        {
            if (PointerInput.IsPointerOverUi())
                return;

            _middleDragOrigin = _camera.ScreenToWorldPoint(currentInputPos);
            _isMiddleDragging = true;
        }

        if (_isMiddleDragging && Input.GetMouseButton(2))
        {
            Vector3 currentWorldPos = _camera.ScreenToWorldPoint(currentInputPos);
            Vector3 direction = _middleDragOrigin - currentWorldPos;

            transform.position = ClampToBoundary(transform.position + direction * _dragSpeed);
            _middleDragOrigin = _camera.ScreenToWorldPoint(currentInputPos);
        }

        if (Input.GetMouseButtonUp(2))
            _isMiddleDragging = false;
    }

    private bool IsContinuousPlacementInProgress()
    {
        if (_sceneRunner == null)
        {
            _sceneRunner = FindAnyObjectByType<MainRunner>();
            if (_sceneRunner == null)
            {
                BuildingSceneRunnerBase[] runners =
                    FindObjectsByType<BuildingSceneRunnerBase>(FindObjectsSortMode.None);
                if (runners.Length > 0)
                    _sceneRunner = runners[0];
            }
        }

        if (_sceneRunner == null)
            return false;
        if (_sceneRunner.BlueprintHandler != null && _sceneRunner.BlueprintHandler.IsBlockingCameraDrag)
            return true;
        if (_sceneRunner.PlacementHandler == null)
            return false;

        if (_sceneRunner.PlacementHandler.IsRemovalMode &&
            _sceneRunner.PlacementHandler.IsPointerRemovalActive)
            return true;

        if (!_sceneRunner.PlacementHandler.IsPlacementMode)
            return false;
        return _sceneRunner.PlacementHandler.IsPointerPlacementActive;
    }

    private void HandleScrollZoom()
    {
        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) < 0.01f || PointerInput.IsPointerOverUi()) return;
        if (!PointerInput.IsScreenPositionInViewport(_camera, PointerInput.PrimaryScreenPosition)) return;

        ApplyZoomAtScreenPoint(scroll * _zoomSpeed, PointerInput.PrimaryScreenPosition);
    }

    private void HandlePinchZoom()
    {
        if (Input.touchCount < 2)
            return;

        Touch touch0 = Input.GetTouch(0);
        Touch touch1 = Input.GetTouch(1);

        Vector2 prevPos0 = touch0.position - touch0.deltaPosition;
        Vector2 prevPos1 = touch1.position - touch1.deltaPosition;
        float previousDistance = (prevPos0 - prevPos1).magnitude;
        float currentDistance = (touch0.position - touch1.position).magnitude;
        float pinchDelta = currentDistance - previousDistance;

        if (Mathf.Abs(pinchDelta) < 0.01f)
            return;

        Vector2 pinchCenter = (touch0.position + touch1.position) * 0.5f;
        ApplyZoomAtScreenPoint(-pinchDelta * _pinchZoomSpeed, pinchCenter);
    }

    private void ApplyZoomAtScreenPoint(float orthoSizeDelta, Vector2 screenPoint)
    {
        Vector3 worldBefore = _camera.ScreenToWorldPoint(screenPoint);

        _camera.orthographicSize = Mathf.Clamp(_camera.orthographicSize - orthoSizeDelta, _minZoom, _maxZoom);

        Vector3 worldAfter = _camera.ScreenToWorldPoint(screenPoint);
        Vector3 offset = worldBefore - worldAfter;

        transform.position = ClampToBoundary(transform.position + offset);
    }

    private Vector3 ClampToBoundary(Vector3 targetPos)
    {
        if (_boundaryCollider == null) return targetPos;

        Bounds bounds = _boundaryCollider.bounds;

        targetPos.x = Mathf.Clamp(targetPos.x, bounds.min.x, bounds.max.x);
        targetPos.y = Mathf.Clamp(targetPos.y, bounds.min.y, bounds.max.y);
        targetPos.z = transform.position.z;

        return targetPos;
    }

}