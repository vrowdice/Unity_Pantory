#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom editor for MarketActorData.
/// All actors handle both supply and consumption.
/// </summary>
[CustomEditor(typeof(MarketActorData))]
public class MarketActorDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        MarketActorData data = (MarketActorData)target;
        
        // Draw default inspector
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "All market actors handle both supply (Provider) and consumption (Consumer). " +
            "Both profiles are always active.",
            MessageType.Info);
    }
}
#endif

