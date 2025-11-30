using UnityEditor;
using Evo.EditorTools;

namespace Evo.UI
{
    [CustomEditor(typeof(RectDragger))]
    public class RectDraggerEditor : Editor
    {
        // Properties
        SerializedProperty isDraggable;
        SerializedProperty boundaryType;
        SerializedProperty boundaryRect;
        SerializedProperty allowOutOfBounds;
        SerializedProperty returnDuration;
        SerializedProperty returnCurve;

        // Foldout states
        bool settingsFoldout = true;

        void OnEnable()
        {
            isDraggable = serializedObject.FindProperty("isDraggable");
            boundaryType = serializedObject.FindProperty("boundaryType");
            boundaryRect = serializedObject.FindProperty("boundaryRect");
            allowOutOfBounds = serializedObject.FindProperty("allowOutOfBounds");
            returnDuration = serializedObject.FindProperty("returnDuration");
            returnCurve = serializedObject.FindProperty("returnCurve");

            EvoEditorGUI.RegisterEditor(this);
        }

        void OnDisable()
        {
            EvoEditorGUI.UnregisterEditor(this);
        }

        public override void OnInspectorGUI()
        {
            if (!EvoEditorSettings.IsCustomEditorEnabled(Constants.CUSTOM_EDITOR_ID)) { DrawDefaultInspector(); }
            else
            {
                DrawCustomGUI();
                EvoEditorGUI.HandleInspectorGUI();
            }
        }

        void DrawCustomGUI()
        {
            serializedObject.Update();
            EvoEditorGUI.BeginCenteredInspector();

            DrawSettings();

            EvoEditorGUI.EndCenteredInspector();
            serializedObject.ApplyModifiedProperties();
        }

        void DrawSettings()
        {
            EvoEditorGUI.BeginVerticalBackground();
            if (EvoEditorGUI.DrawFoldout(ref settingsFoldout, "Settings", EvoEditorGUI.GetIcon("UI_Settings")))
            {
                EvoEditorGUI.BeginContainer();
                {
                    EvoEditorGUI.DrawToggle(isDraggable, "Is Draggable", null, true, true, true);
                    EvoEditorGUI.BeginVerticalBackground(true);
                    EvoEditorGUI.DrawToggle(allowOutOfBounds, "Allow Going Out Of Bounds", addSpace: false, customBackground: true, revertColor: true, bypassNormalBackground: true);
                    if (allowOutOfBounds.boolValue)
                    {
                        EvoEditorGUI.BeginContainer(3);
                        EvoEditorGUI.DrawProperty(returnDuration, "Return Duration", null, true, true);
                        EvoEditorGUI.DrawProperty(returnCurve, "Return Curve", null, false, true);
                        EvoEditorGUI.EndContainer();
                    }
                    EvoEditorGUI.EndVerticalBackground(true);
                    EvoEditorGUI.BeginVerticalBackground(true);
                    {
                        EvoEditorGUI.DrawProperty(boundaryType, "Boundary Type", null, false, false);
                        if (boundaryType.enumValueIndex == 2) 
                        {
                            EvoEditorGUI.BeginContainer(3);
                            EvoEditorGUI.DrawProperty(boundaryRect, "Boundary Rect", null, false, true);
                            EvoEditorGUI.EndContainer();
                        }
                    }
                    EvoEditorGUI.EndVerticalBackground();
                }
                EvoEditorGUI.EndContainer();
            }
            EvoEditorGUI.EndVerticalBackground();
        }
    }
}