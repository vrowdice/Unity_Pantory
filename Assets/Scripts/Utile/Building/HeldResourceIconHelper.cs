using System.Collections.Generic;
using UnityEngine;

public static class HeldResourceIconHelper
{
    public static void Clear(ref GameObject container)
    {
        if (container == null) return;
        PoolingManager.Instance?.ClearChildrenToPool(container.transform);
        Object.Destroy(container);
        container = null;
    }

    public static void Refresh(
        ref GameObject container,
        Transform anchor,
        float iconScale,
        string containerNamePrefix,
        Dictionary<string, int> counts)
    {
        Clear(ref container);

        if (counts == null || counts.Count == 0) return;
        if (!ResourceFlowFx.IsWorldPointVisible(anchor.position)) return;

        Transform worldCanvas = GameManager.Instance?.GetWorldCanvas();
        if (worldCanvas == null) return;

        container = UIManager.Instance.CreateResourceImageContainer(
            worldCanvas,
            $"{containerNamePrefix}_{anchor.gameObject.GetInstanceID()}",
            anchor.position + ResourceFlowFx.IconWorldOffset,
            iconScale,
            counts);
    }
}
