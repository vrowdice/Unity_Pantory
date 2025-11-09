using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 메인 카메라의 드래그, 줌, 경계 제한 기능을 관리하는 클래스
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
    private bool _isDragging = false;
    private bool _isDragEnabled = true; // 드래그 활성화 여부

    private void Awake()
    {
        _camera = GetComponent<Camera>();
    }

    /// <summary>
    /// 카메라 드래그 활성화/비활성화
    /// </summary>
    public void SetDragEnabled(bool enabled)
    {
        _isDragEnabled = enabled;
        if (!enabled)
        {
            _isDragging = false; // 비활성화 시 현재 드래그도 중단
        }
    }

    void Update()
    {
        HandleDrag();
        HandleZoom();
    }

    // 드래그로 카메라 이동
    private void HandleDrag()
    {
        // 드래그가 비활성화된 경우 처리하지 않음
        if (!_isDragEnabled)
        {
            _isDragging = false;
            return;
        }

        // 마우스 오른쪽 클릭 (1) 또는 휠 클릭 (2)으로 드래그 시작
        if (Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
        {
            // UI 위에 포인터가 있을 경우 드래그 시작을 막음 (단, 휠 클릭은 예외)
            // NOTE: 원본 코드에는 Input.GetMouseButtonDown(0) 조건이 있었으나, 
            // 오른쪽/휠 클릭만 확인하므로, UI 체크는 왼쪽 클릭이 아닌 경우에만 의미가 있습니다.
            if (Input.GetMouseButtonDown(1) && IsPointerOverUI())
            {
                _isDragging = false;
                return;
            }

            _dragOrigin = _camera.ScreenToWorldPoint(Input.mousePosition);
            _isDragging = true;
        }

        // 드래그 중일 때 카메라 이동
        if ((Input.GetMouseButton(1) || Input.GetMouseButton(2)) && _isDragging)
        {
            Vector3 currentMousePos = _camera.ScreenToWorldPoint(Input.mousePosition);
            Vector3 difference = _dragOrigin - currentMousePos;

            Vector3 newPosition = transform.position + difference * _dragSpeed;
            newPosition.z = transform.position.z; // Z 위치 고정

            // 경계 제한 적용
            if (_boundaryCollider != null)
            {
                newPosition = ClampToBounds(newPosition);
            }

            transform.position = newPosition;

            // dragOrigin 업데이트: 뷰포트 내에서는 마우스 위치를 계속 추적
            _dragOrigin = _camera.ScreenToWorldPoint(Input.mousePosition);
        }

        // 마우스 오른쪽 클릭 또는 휠 클릭 해제 시 드래그 종료
        if (Input.GetMouseButtonUp(1) || Input.GetMouseButtonUp(2))
        {
            _isDragging = false;
        }
    }

    // 마우스 휠로 줌인/줌아웃
    private void HandleZoom()
    {
        float scrollInput = Input.mouseScrollDelta.y;

        // 1. 스크롤 입력이 없거나,
        // 2. 마우스가 UI 위에 있거나,
        // 3. 마우스가 게임 뷰포트 밖에 있다면 줌 무시
        if (scrollInput == 0 || IsPointerOverUI() || !IsMouseInViewport())
        {
            return;
        }

        // 줌 전 마우스의 월드 좌표 저장
        Vector3 mouseWorldPosBefore = _camera.ScreenToWorldPoint(Input.mousePosition);

        // Orthographic Size 조절
        _camera.orthographicSize -= scrollInput * _zoomSpeed;
        _camera.orthographicSize = Mathf.Clamp(_camera.orthographicSize, _minZoom, _maxZoom);

        // 줌 후 마우스의 월드 좌표
        Vector3 mouseWorldPosAfter = _camera.ScreenToWorldPoint(Input.mousePosition);

        // 마우스 위치가 변하지 않도록 카메라 위치 보정 (Zoom-to-Cursor)
        Vector3 offset = mouseWorldPosBefore - mouseWorldPosAfter;
        Vector3 newPosition = transform.position + offset;
        newPosition.z = transform.position.z; // Z 위치 고정

        // 경계 제한 적용
        if (_boundaryCollider != null)
        {
            newPosition = ClampToBounds(newPosition);
        }

        transform.position = newPosition;
    }

    // 카메라 위치를 경계 내로 제한 (카메라 중심점 기준)
    private Vector3 ClampToBounds(Vector3 position)
    {
        // 뷰포트 크기를 고려하여 경계를 계산해야 하지만, 
        // 현재 로직은 카메라 중심(position)만 경계 내로 클램프합니다. 
        // (이것이 의도된 동작이라면 유지)
        Bounds bounds = _boundaryCollider.bounds;

        position.x = Mathf.Clamp(position.x, bounds.min.x, bounds.max.x);
        position.y = Mathf.Clamp(position.y, bounds.min.y, bounds.max.y);

        return position;
    }

    // UI 위에 마우스가 있는지 확인
    private bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    /// <summary>
    /// 마우스 커서가 현재 게임 뷰포트(화면) 내에 있는지 확인합니다.
    /// </summary>
    private bool IsMouseInViewport()
    {
        Vector3 mousePos = Input.mousePosition;

        // 마우스 좌표는 스크린의 왼쪽 아래가 (0, 0), 오른쪽 위가 (Screen.width, Screen.height)입니다.
        if (mousePos.x < 0 || mousePos.x > Screen.width ||
            mousePos.y < 0 || mousePos.y > Screen.height)
        {
            return false;
        }
        return true;
    }
}