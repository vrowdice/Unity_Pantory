using UnityEngine;
using System.Collections.Generic;

public class BuildingTileManager : MonoBehaviour
{
    [SerializeField] private GameObject _buildingTilePrefab;

    [SerializeField] private int _gridWidth = 10;
    [SerializeField] private int _gridHeight = 10;

    // 타일을 좌표로 관리하는 Dictionary
    private Dictionary<Vector2Int, GameObject> _buildingTiles = new Dictionary<Vector2Int, GameObject>();

    private BoxCollider2D _cameraCollider;



    void Start()
    {
        CreateGrid(_gridWidth, _gridHeight);
        SetPositionCenter();
        SetCameraCollider();
    }

    public void SetPositionCenter()
    {
        transform.position = new Vector3(-_gridWidth / 2, _gridHeight / 2, 11);
    }

    public void SetCameraCollider()
    {
        _cameraCollider = GetComponent<BoxCollider2D>();
        _cameraCollider.offset = new Vector2(_gridWidth / 2, -_gridHeight / 2);
        _cameraCollider.size = new Vector2(_gridWidth, _gridHeight);
    }

    // 그리드 생성
    public void CreateGrid(int width, int height)
    {
        ClearGrid();

        for (int y = 0; y < width; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2Int position = new Vector2Int(x, y);
                GameObject tile = Instantiate(_buildingTilePrefab, new Vector3(x, -y, 0), Quaternion.identity, transform);
                _buildingTiles[position] = tile;
            }
        }
    }

    // 특정 좌표의 타일 가져오기
    public GameObject GetThreadTile(Vector2Int position)
    {
        return _buildingTiles.ContainsKey(position) ? _buildingTiles[position] : null;
    }

    // 타일이 존재하는지 확인
    public bool HasThreadTile(Vector2Int position)
    {
        return _buildingTiles.ContainsKey(position);
    }

    // 그리드 확장 (런타임에 크기 변경 가능)
    public void ExpandGrid(int newWidth, int newHeight)
    {
        CreateGrid(newWidth, newHeight);
    }

    // 그리드 초기화
    private void ClearGrid()
    {
        foreach (var tile in _buildingTiles.Values)
        {
            Destroy(tile);
        }
        _buildingTiles.Clear();
    }
}
