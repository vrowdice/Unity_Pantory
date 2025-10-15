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

    private Camera _camera;
    private Vector3 _dragOrigin;
    private bool _isDragging = false;
    
    void Start()
    {
        _camera = GetComponent<Camera>();
    }

    void Update()
    {
        HandleDrag();
        HandleZoom();
    }
    
    // 드래그로 카메라 이동
    private void HandleDrag()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // 드래그 시작 시에만 UI 체크
            if (IsPointerOverUI())
            {
                _isDragging = false;
                return;
            }
            
            _dragOrigin = _camera.ScreenToWorldPoint(Input.mousePosition);
            _isDragging = true;
        }
        
        if (Input.GetMouseButton(0) && _isDragging)
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
        
        if (Input.GetMouseButtonUp(0))
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
            // Orthographic Size 조절
            _camera.orthographicSize -= scrollInput * _zoomSpeed;
            _camera.orthographicSize = Mathf.Clamp(_camera.orthographicSize, _minZoom, _maxZoom);
            
            // 줌 후 경계 체크
            if (_boundaryCollider != null)
            {
                Vector3 clampedPosition = ClampToBounds(transform.position);
                transform.position = clampedPosition;
            }
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
