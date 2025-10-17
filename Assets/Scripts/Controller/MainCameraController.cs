using UnityEngine;
using UnityEngine.EventSystems;

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
        
        // 왼쪽 클릭 또는 휠 클릭으로 드래그 시작
        if (Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
        {
            // 왼쪽 클릭인 경우에만 UI 체크 (휠 클릭은 항상 드래그 가능)
            if (Input.GetMouseButtonDown(0) && IsPointerOverUI())
            {
                _isDragging = false;
                return;
            }
            
            _dragOrigin = _camera.ScreenToWorldPoint(Input.mousePosition);
            _isDragging = true;
        }
        
        // 왼쪽 클릭 또는 휠 클릭 유지 중일 때 드래그
        if ((Input.GetMouseButton(1) || Input.GetMouseButton(2)) && _isDragging)
        {
            Vector3 currentMousePos = _camera.ScreenToWorldPoint(Input.mousePosition);
            Vector3 difference = _dragOrigin - currentMousePos;
            
            Vector3 newPosition = transform.position + difference * _dragSpeed;
            newPosition.z = transform.position.z; // Z 위치 고정
            
            // 경계 제한
            if (_boundaryCollider != null)
            {
                newPosition = ClampToBounds(newPosition);
            }
            
            transform.position = newPosition;
            
            // dragOrigin 업데이트
            _dragOrigin = _camera.ScreenToWorldPoint(Input.mousePosition);
        }
        
        // 왼쪽 클릭 또는 휠 클릭 해제 시 드래그 종료
        if (Input.GetMouseButtonUp(1) || Input.GetMouseButtonUp(2))
        {
            _isDragging = false;
        }
    }
    
    // 마우스 휠로 줌인/줌아웃
    private void HandleZoom()
    {
        // UI 위에 마우스가 있으면 줌 무시
        if (IsPointerOverUI())
        {
            return;
        }
        
        float scrollInput = Input.mouseScrollDelta.y;
        
        if (scrollInput != 0)
        {
            // 줌 전 마우스의 월드 좌표 저장
            Vector3 mouseWorldPosBefore = _camera.ScreenToWorldPoint(Input.mousePosition);
            
            // 기존 Orthographic Size 저장
            float oldSize = _camera.orthographicSize;
            
            // Orthographic Size 조절
            _camera.orthographicSize -= scrollInput * _zoomSpeed;
            _camera.orthographicSize = Mathf.Clamp(_camera.orthographicSize, _minZoom, _maxZoom);
            
            // 줌 후 마우스의 월드 좌표
            Vector3 mouseWorldPosAfter = _camera.ScreenToWorldPoint(Input.mousePosition);
            
            // 마우스 위치가 변하지 않도록 카메라 위치 보정
            Vector3 offset = mouseWorldPosBefore - mouseWorldPosAfter;
            Vector3 newPosition = transform.position + offset;
            newPosition.z = transform.position.z; // Z 위치 고정
            
            // 경계 제한
            if (_boundaryCollider != null)
            {
                newPosition = ClampToBounds(newPosition);
            }
            
            transform.position = newPosition;
        }
    }
    
    // 카메라 위치를 경계 내로 제한 (카메라 중심점 기준)
    private Vector3 ClampToBounds(Vector3 position)
    {
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
}
