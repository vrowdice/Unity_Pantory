using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class TMPFontChangerUtileObj : MonoBehaviour
{
    [SerializeField] public TMP_FontAsset FontAsset;
}

#if UNITY_EDITOR
[CustomEditor(typeof(TMPFontChangerUtileObj))]
public class TMP_FontChangerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (GUILayout.Button("Change Font"))
        {
            TMP_FontAsset fontAsset = ((TMPFontChangerUtileObj)target).FontAsset;

            foreach(TextMeshPro textMeshPro3D in FindObjectsByType<TextMeshPro>(FindObjectsInactive.Include, FindObjectsSortMode.None)) 
            { 
                textMeshPro3D.font = fontAsset;
            }
            foreach(TextMeshProUGUI textMeshProUi in FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None)) 
            { 
                textMeshProUi.font = fontAsset;
            }
        }
    }
}
#endif