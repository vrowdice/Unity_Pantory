using UnityEngine;

/// <summary>
/// 메인 씬에서 그리드에 설치된 건물 오브젝트의 최소 메타데이터.
/// 멀티 타일(1x1 이상) 건물 제거/조회 시 신뢰 가능한 원점/사이즈/회전을 제공하기 위함.
/// </summary>
public class BuildingObject : MonoBehaviour
{
    [SerializeField] private BuildingData _buildingData;
    [SerializeField] private Vector2Int _origin;
    [SerializeField] private Vector2Int _size;
    [SerializeField] private int _rotation;
    [SerializeField] private SpriteRenderer _viewObjRenderer;
    [SerializeField] private BoxCollider2D _collider;

    [Header("Output Indicators")]
    [SerializeField] private GameObject _outputIndicatorPrefab;
    public System.Collections.Generic.List<Vector2Int> OutputGridPositions { get; private set; }

    public BuildingData BuildingData => _buildingData;
    public Vector2Int Origin => _origin;
    public Vector2Int Size => _size;
    public int Rotation => _rotation;

    public void Init(BuildingData buildingData, Vector2Int origin, Vector2Int rotatedSize, int rotation)
    {
        _buildingData = buildingData;
        _origin = origin;
        _size = rotatedSize;
        _rotation = rotation;

        RebuildOutputGridPositions();

        if (_viewObjRenderer != null)
        {
            _viewObjRenderer.sprite = buildingData != null ? buildingData.buildingSprite : null;
            _viewObjRenderer.transform.localRotation = Quaternion.Euler(0, 0, -rotation * 90f);
            _viewObjRenderer.transform.localScale = new Vector3(rotatedSize.x, rotatedSize.y, 1);
        }
        if (_collider != null)
        {
            _collider.size = new Vector2(rotatedSize.x, rotatedSize.y);
            _collider.offset = Vector2.zero;
        }

        UpdateOutputIndicators();
    }

    /// <summary>
    /// _origin/_size/_rotation 기준으로 실제 출력 그리드 좌표 목록을 계산합니다.
    /// </summary>
    private void RebuildOutputGridPositions()
    {
        if (OutputGridPositions == null)
        {
            OutputGridPositions = new System.Collections.Generic.List<Vector2Int>();
        }
        else
        {
            OutputGridPositions.Clear();
        }
        
        int rightX = _size.x - 1;
        for (int y = 0; y < _size.y; y++)
        {
            Vector2Int localCell = new Vector2Int(rightX, y);
            Vector2Int rotatedLocal = RotateCellAroundCenter(localCell, _rotation, _size);
            Vector2Int worldGridPos = _origin + rotatedLocal;
            OutputGridPositions.Add(worldGridPos);
        }
    }

    private void UpdateOutputIndicators()
    {
        if (_buildingData == null)
        {
            return;
        }

        if (_outputIndicatorPrefab == null)
        {
            return;
        }

        int rightX = _size.x - 1;
        for (int y = 0; y < _size.y; y++)
        {
            Vector2Int localCell = new Vector2Int(rightX, y);
            Vector3 localPos = GetLocalPositionForCell(localCell, _size);
            localPos = RotateOffset(localPos, _rotation);

            GameObject indicator = Instantiate(_outputIndicatorPrefab, transform);
            indicator.transform.localPosition = localPos;
            indicator.transform.localRotation = Quaternion.Euler(0f, 0f, -_rotation * 90f);
        }
    }

    private Vector3 GetLocalPositionForCell(Vector2Int cell, Vector2Int size)
    {
        float centerX = (size.x - 1) * 0.5f;
        float centerY = (size.y - 1) * 0.5f;

        float x = cell.x + 0.5f - centerX;
        float y = -(cell.y + 0.5f) + centerY;

        return new Vector3(x, y, 0f);
    }

    private Vector3 RotateOffset(Vector3 offset, int rotation)
    {
        float angle = -rotation * 90f;
        Quaternion rot = Quaternion.Euler(0f, 0f, angle);
        return rot * offset;
    }

    /// <summary>
    /// BuildingState의 RotatePositionAroundCenter와 동일한 방식으로
    /// 로컬 그리드 셀을 건물 중심 기준으로 회전시킵니다.
    /// </summary>
    private Vector2Int RotateCellAroundCenter(Vector2Int cell, int rotation, Vector2Int buildingSize)
    {
        rotation = rotation % 4;
        if (rotation == 0)
        {
            return cell;
        }

        float centerX = (buildingSize.x - 1) / 2f;
        float centerY = (buildingSize.y - 1) / 2f;

        float relX = cell.x - centerX;
        float relY = cell.y - centerY;

        float rotatedX;
        float rotatedY;

        switch (rotation)
        {
            case 1:
                rotatedX = -relY;
                rotatedY = relX;
                break;
            case 2:
                rotatedX = -relX;
                rotatedY = -relY;
                break;
            case 3:
                rotatedX = relY;
                rotatedY = -relX;
                break;
            default:
                rotatedX = relX;
                rotatedY = relY;
                break;
        }

        return new Vector2Int(
            Mathf.RoundToInt(rotatedX + centerX),
            Mathf.RoundToInt(rotatedY + centerY)
        );
    }
}

