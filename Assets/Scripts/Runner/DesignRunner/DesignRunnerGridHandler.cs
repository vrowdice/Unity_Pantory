using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 그리드 타일의 생성, 건물 오브젝트의 배치 및 좌표 변환을 관리하는 핸들러입니다.
/// 모든 건물은 정사각형으로 처리되며, 스프라이트 회전 없이 내부 데이터(포트 위치)만 회전합니다.
/// </summary>
public class DesignRunnerGridHandler
{
    private readonly DesignRunner _manager;
    private readonly Transform _parentTransform;

    // Prefabs
    private readonly GameObject _tilePrefab;
    private readonly GameObject _buildingPrefab;
    private readonly GameObject _inputMarkerPrefab;
    private readonly GameObject _outputMarkerPrefab;

    // Grid Data
    private int _gridWidth;
    private int _gridHeight;
    private readonly Dictionary<Vector2Int, GameObject> _tileMap = new Dictionary<Vector2Int, GameObject>();
    private readonly Dictionary<Vector2Int, GameObject> _buildingMap = new Dictionary<Vector2Int, GameObject>();

    // Constants (Depth/Z-axis)
    private const float TileZDepth = 10f;
    private const float BuildingZDepth = 9f;

    public int Width => _gridWidth;
    public int Height => _gridHeight;

    public DesignRunnerGridHandler(
        DesignRunner manager,
        GameObject tilePrefab,
        GameObject buildingPrefab,
        GameObject inputMarker,
        GameObject outputMarker,
        int width,
        int height)
    {
        _manager = manager;
        _parentTransform = manager.transform;
        _tilePrefab = tilePrefab;
        _buildingPrefab = buildingPrefab;
        _inputMarkerPrefab = inputMarker;
        _outputMarkerPrefab = outputMarker;
        _gridWidth = width;
        _gridHeight = height;
    }

    #region Grid Initialization

    public void CreateGrid(int width, int height)
    {
        ClearGrid();
        _gridWidth = width;
        _gridHeight = height;

        for (int y = 0; y < _gridHeight; y++)
        {
            for (int x = 0; x < _gridWidth; x++)
            {
                Vector2Int gridPosition = new Vector2Int(x, y);
                // 유니티 좌표계 특성상 y축은 아래로 생성 (-y)
                GameObject tileObject = Object.Instantiate(_tilePrefab, new Vector3(x, -y, TileZDepth), Quaternion.identity, _parentTransform);
                tileObject.name = "Tile_" + x + "_" + y;

                BuildingTile buildingTileComponent;
                if (tileObject.TryGetComponent<BuildingTile>(out buildingTileComponent))
                {
                    buildingTileComponent.Initialize(gridPosition, null);
                }

                _tileMap[gridPosition] = tileObject;
            }
        }
    }

    public void ExpandGrid(int newWidth, int newHeight) => CreateGrid(newWidth, newHeight);

    public void ClearGrid()
    {
        ClearAllPlacedBuildings();
        foreach (GameObject tileObject in _tileMap.Values)
        {
            if (tileObject != null) Object.Destroy(tileObject);
        }
        _tileMap.Clear();
    }

    public void ClearAllPlacedBuildings()
    {
        foreach (GameObject buildingObject in _buildingMap.Values)
        {
            if (buildingObject != null) Object.Destroy(buildingObject);
        }
        _buildingMap.Clear();
        ClearAllOccupiedStatus();
    }

    public void ClearAllOccupiedStatus()
    {
        foreach (GameObject tileObject in _tileMap.Values)
        {
            BuildingTile buildingTileComponent;
            if (tileObject.TryGetComponent<BuildingTile>(out buildingTileComponent))
            {
                buildingTileComponent.SetOccupied(false);
            }
        }
    }

    #endregion

    #region Occupation Logic

    public bool CanPlaceBuilding(Vector2Int gridPosition, Vector2Int size)
    {
        // 1. 그리드 범위 밖인지 확인
        if (gridPosition.x < 0 || gridPosition.y < 0 ||
            gridPosition.x + size.x > _gridWidth || gridPosition.y + size.y > _gridHeight)
        {
            return false;
        }

        // 2. 해당 영역의 타일들이 비어있는지 확인
        for (int y = 0; y < size.y; y++)
        {
            for (int x = 0; x < size.x; x++)
            {
                if (!IsTileAvailable(new Vector2Int(gridPosition.x + x, gridPosition.y + y)))
                {
                    return false;
                }
            }
        }
        return true;
    }

    private bool IsTileAvailable(Vector2Int gridPosition)
    {
        GameObject tileObject;
        if (_tileMap.TryGetValue(gridPosition, out tileObject))
        {
            BuildingTile buildingTileComponent;
            if (tileObject.TryGetComponent<BuildingTile>(out buildingTileComponent))
            {
                return !buildingTileComponent.IsOccupied;
            }
        }
        return false;
    }

    public void MarkTilesAsOccupied(Vector2Int startPosition, Vector2Int size, bool occupied = true)
    {
        for (int y = 0; y < size.y; y++)
        {
            for (int x = 0; x < size.x; x++)
            {
                Vector2Int currentPosition = new Vector2Int(startPosition.x + x, startPosition.y + y);
                GameObject tileObject;
                if (_tileMap.TryGetValue(currentPosition, out tileObject))
                {
                    BuildingTile buildingTileComponent;
                    if (tileObject.TryGetComponent<BuildingTile>(out buildingTileComponent))
                    {
                        buildingTileComponent.SetOccupied(occupied);
                    }
                }
            }
        }
    }

    #endregion

    #region Building Management

    public GameObject CreateBuildingObject(Vector2Int gridPosition, BuildingData buildingData, BuildingState buildingState = null)
    {
        if (buildingData.buildingSprite == null) return null;

        // 1. 오브젝트 생성 및 이름 설정
        GameObject buildingObject;
        if (_buildingPrefab != null)
        {
            buildingObject = Object.Instantiate(_buildingPrefab, _parentTransform);
        }
        else
        {
            buildingObject = new GameObject("Building_" + buildingData.id);
        }

        buildingObject.name = "Building_" + buildingData.id + "_" + gridPosition;
        buildingObject.transform.SetParent(_parentTransform);

        // 2. 위치 및 크기 설정 (정사각형 건물이므로 회전 0도 고정)
        buildingObject.transform.position = GridToWorldPosition(gridPosition, buildingData.size);
        buildingObject.transform.rotation = Quaternion.identity;

        // 3. 비주얼 설정 (SpriteRenderer)
        SpriteRenderer spriteRenderer = buildingObject.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) spriteRenderer = buildingObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = buildingData.buildingSprite;

        // 4. 스케일 계산
        buildingObject.transform.localScale = GameObjectUtils.CalculateSpriteScale(buildingData.buildingSprite, buildingData.size);

        // 5. 컴포넌트 초기화
        BuildingObject buildingComponent = buildingObject.GetComponent<BuildingObject>();
        if (buildingComponent == null) buildingComponent = buildingObject.AddComponent<BuildingObject>();

        BoxCollider2D boxCollider = buildingObject.GetComponent<BoxCollider2D>();
        if (boxCollider == null) boxCollider = buildingObject.AddComponent<BoxCollider2D>();
        boxCollider.size = new Vector2(buildingData.size.x, buildingData.size.y);

        if (buildingState != null)
        {
            // 실제 회전 데이터는 BuildingObject 내부에서 마커 위치를 정할 때만 사용됨
            buildingComponent.Initialize(buildingData, buildingState, _inputMarkerPrefab, _outputMarkerPrefab, this);
            buildingComponent.SetupProductionIcons();
        }

        _buildingMap[gridPosition] = buildingObject;
        return buildingObject;
    }

    public void RemoveBuildingObject(Vector2Int gridPosition)
    {
        GameObject buildingObject;
        if (_buildingMap.TryGetValue(gridPosition, out buildingObject))
        {
            Object.Destroy(buildingObject);
            _buildingMap.Remove(gridPosition);
        }
    }

    /// <summary>
    /// 특정 좌표에 위치한 건물 오브젝트를 가져옵니다.
    /// </summary>
    public GameObject GetBuildingAtPosition(Vector2Int gridPosition)
    {
        foreach (KeyValuePair<Vector2Int, GameObject> entry in _buildingMap)
        {
            Vector2Int buildingOrigin = entry.Key;
            GameObject buildingObject = entry.Value;

            BuildingObject buildingComponent;
            if (buildingObject.TryGetComponent<BuildingObject>(out buildingComponent))
            {
                Vector2Int buildingSize = buildingComponent.BuildingData.size;
                if (gridPosition.x >= buildingOrigin.x && gridPosition.x < buildingOrigin.x + buildingSize.x &&
                    gridPosition.y >= buildingOrigin.y && gridPosition.y < buildingOrigin.y + buildingSize.y)
                {
                    return buildingObject;
                }
            }
        }
        return null;
    }

    #endregion

    #region Coordinate Conversion

    public Vector2Int WorldToGridPosition(Vector3 worldPosition)
    {
        Vector3 localPosition = worldPosition - _parentTransform.position;
        return new Vector2Int(
            Mathf.FloorToInt(localPosition.x + 0.5f),
            Mathf.FloorToInt(-localPosition.y + 0.5f)
        );
    }

    public Vector3 GridToWorldPosition(Vector2Int gridPosition, Vector2Int size)
    {
        float centerX = gridPosition.x + (size.x - 1) * 0.5f;
        float centerY = -gridPosition.y - (size.y - 1) * 0.5f;
        return _parentTransform.position + new Vector3(centerX, centerY, BuildingZDepth);
    }

    #endregion
}