using UnityEngine;

/// <summary>
/// Vector2Int를 직렬화하기 위한 Wrapper 클래스 (JSON 직렬화용)
/// </summary>
[System.Serializable]
public class SerializableVector2Int
{
    public int x;
    public int y;

    public SerializableVector2Int() { }

    public SerializableVector2Int(Vector2Int v)
    {
        x = v.x;
        y = v.y;
    }

    public Vector2Int ToVector2Int()
    {
        return new Vector2Int(x, y);
    }
}
