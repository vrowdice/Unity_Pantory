using UnityEngine;

/// <summary>
/// 그리드 회전 및 좌표 계산을 위한 순수 수학 유틸리티입니다.
/// </summary>
public static class GridMathUtils
{
    /// <summary>
    /// 회전 인덱스를 기반으로 건물 크기를 계산합니다.
    /// </summary>
    /// <param name="size">원본 크기</param>
    /// <param name="rotationIndex">회전 인덱스 (0: 0도, 1: 90도, 2: 180도, 3: 270도)</param>
    /// <returns>회전된 크기</returns>
    public static Vector2Int GetRotatedSize(Vector2Int size, int rotationIndex)
    {
        int rot = rotationIndex % 4;
        // 90도(1) 혹은 270도(3) 회전 시 가로세로 반전
        return (rot == 1 || rot == 3) ? new Vector2Int(size.y, size.x) : size;
    }

    /// <summary>
    /// 회전 인덱스를 기반으로 오프셋을 회전시킵니다.
    /// </summary>
    /// <param name="offset">원본 오프셋</param>
    /// <param name="rotationIndex">회전 인덱스</param>
    /// <returns>회전된 오프셋</returns>
    public static Vector2Int GetRotatedOffset(Vector2Int offset, int rotationIndex)
    {
        int rot = rotationIndex % 4;
        switch (rot)
        {
            case 1: return new Vector2Int(offset.y, -offset.x);
            case 2: return new Vector2Int(-offset.x, -offset.y);
            case 3: return new Vector2Int(-offset.y, offset.x);
            default: return offset;
        }
    }

    /// <summary>
    /// 월드 좌표를 그리드 좌표로 변환합니다.
    /// </summary>
    /// <param name="parent">부모 Transform</param>
    /// <param name="worldPos">월드 좌표</param>
    /// <returns>그리드 좌표</returns>
    public static Vector2Int GetWorldToGridPos(Transform parent, Vector3 worldPos)
    {
        Vector3 localPos = worldPos - parent.position;
        return new Vector2Int(
            Mathf.FloorToInt(localPos.x + 0.5f),
            Mathf.FloorToInt(-localPos.y + 0.5f)
        );
    }

    /// <summary>
    /// 그리드 좌표를 월드 좌표로 변환합니다.
    /// </summary>
    /// <param name="parent">부모 Transform</param>
    /// <param name="gridPos">그리드 좌표</param>
    /// <param name="size">건물 크기</param>
    /// <param name="zDepth">Z축 깊이</param>
    /// <returns>월드 좌표</returns>
    public static Vector3 GetGridToWorldPos(Transform parent, Vector2Int gridPos, Vector2Int size, float zDepth)
    {
        float centerX = gridPos.x + (size.x - 1) * 0.5f;
        float centerY = -gridPos.y - (size.y - 1) * 0.5f;
        return parent.position + new Vector3(centerX, centerY, zDepth);
    }
}
