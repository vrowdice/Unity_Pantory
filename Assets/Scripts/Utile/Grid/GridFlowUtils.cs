using UnityEngine;

public static class GridFlowUtils
{
    public static FlowDirection DirectionFromDelta(Vector2Int delta)
    {
        if (delta == new Vector2Int(0, -1)) return FlowDirection.Up;
        if (delta == new Vector2Int(1, 0)) return FlowDirection.Right;
        if (delta == new Vector2Int(0, 1)) return FlowDirection.Down;
        if (delta == new Vector2Int(-1, 0)) return FlowDirection.Left;
        return FlowDirection.None;
    }

    public static bool IsVertical(FlowDirection direction) =>
        direction == FlowDirection.Up || direction == FlowDirection.Down;

    public static bool IsHorizontal(FlowDirection direction) =>
        direction == FlowDirection.Left || direction == FlowDirection.Right;

    public static Vector2Int RotateCellClockwise(Vector2Int direction, int rotation)
    {
        int steps = ((rotation % 4) + 4) % 4;
        Vector2Int current = direction;
        for (int i = 0; i < steps; i++)
            current = new Vector2Int(-current.y, current.x);
        return current;
    }
}
