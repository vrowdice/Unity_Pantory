using System.Collections.Generic;
using UnityEngine;

public static class GameObjectUtils
{
    public static void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            GameObject.Destroy(parent.GetChild(i).gameObject);
        }
    }
}
