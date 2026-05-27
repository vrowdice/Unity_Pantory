#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class BtnPrefabWireUtility
{
    private const string MenuPath = "Tools/Pantory/UI/Wire Btn Prefab References";

    [MenuItem(MenuPath)]
    public static void WireAllBtnPrefabs()
    {
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" });
        int wiredCount = 0;

        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefabRoot == null)
            {
                continue;
            }

            if (WirePrefab(path, prefabRoot))
            {
                wiredCount++;
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[BtnPrefabWireUtility] Wired {wiredCount} prefab(s).");
    }

    private static bool WirePrefab(string path, GameObject prefabRoot)
    {
        bool changed = false;
        BtnBase[] btnComponents = prefabRoot.GetComponentsInChildren<BtnBase>(true);

        foreach (BtnBase btn in btnComponents)
        {
            if (TryAssignButtonReference(btn))
            {
                changed = true;
                EditorUtility.SetDirty(btn);
            }
        }

        if (changed)
        {
            PrefabUtility.SavePrefabAsset(prefabRoot);
            Debug.Log($"[BtnPrefabWireUtility] Updated: {path}");
        }

        return changed;
    }

    private static bool TryAssignButtonReference(BtnBase btn)
    {
        FieldInfo field = typeof(BtnBase).GetField("_button", BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null)
        {
            return false;
        }

        Evo.UI.Button current = field.GetValue(btn) as Evo.UI.Button;
        if (current != null)
        {
            return false;
        }

        Evo.UI.Button resolved = btn.GetComponent<Evo.UI.Button>();
        if (resolved == null)
        {
            resolved = btn.GetComponentInParent<Evo.UI.Button>();
        }
        if (resolved == null)
        {
            resolved = btn.GetComponentInChildren<Evo.UI.Button>(true);
        }

        if (resolved != null)
        {
            field.SetValue(btn, resolved);
            return true;
        }

        Debug.LogWarning($"[BtnPrefabWireUtility] No Evo.UI.Button found for {btn.GetType().Name} on {btn.gameObject.name}", btn);
        return false;
    }
}
#endif
