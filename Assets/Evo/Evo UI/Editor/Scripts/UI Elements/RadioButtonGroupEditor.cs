using UnityEditor;
using Evo.EditorTools;

namespace Evo.UI
{
    [CustomEditor(typeof(RadioButtonGroup))]
    public class RadioButtonGroupEditor : Editor
    {
        // Target
        RadioButtonGroup rbgTarget;

        // Properties
        SerializedProperty selectedIndex;
        SerializedProperty allowDeselection;
        SerializedProperty onSelectionChanged;

        // Foldout states
        bool settingsFoldout = true;
        bool eventsFoldout = false;
        void OnEnable()
        {
            rbgTarget = (RadioButtonGroup)target;

            selectedIndex = serializedObject.FindProperty("selectedIndex");
            allowDeselection = serializedObject.FindProperty("allowDeselection");
            onSelectionChanged = serializedObject.FindProperty("onSelectionChanged");

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
            DrawEvents();

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
                    EvoEditorGUI.DrawToggle(allowDeselection, "Allow Deselection", "Allow deselection of buttons.", true, true, true);
                    EvoEditorGUI.BeginVerticalBackground(true);
                    {
                        if (rbgTarget.selectedIndex > rbgTarget.transform.childCount - 1) { rbgTarget.selectedIndex = -1; }
                        if (rbgTarget.selectedIndex >= 0 && rbgTarget.transform.childCount > 0) { EvoEditorGUI.DrawLabel($" Selected Button: {rbgTarget.transform.GetChild(rbgTarget.selectedIndex).name}"); }
                        else { EvoEditorGUI.DrawLabel(" Selected Button: None"); }
                        EditorGUI.BeginChangeCheck();
                        EvoEditorGUI.DrawSlider(selectedIndex, -1, rbgTarget.transform.childCount > 0 ? rbgTarget.transform.childCount - 1 : 0, "Index:", false, false, labelWidth: 40);
                        if (EditorGUI.EndChangeCheck()) { serializedObject.ApplyModifiedProperties(); }
                    }
                    EvoEditorGUI.EndVerticalBackground();
                }
                EvoEditorGUI.EndContainer();
            }
            EvoEditorGUI.EndVerticalBackground();
            EvoEditorGUI.AddFoldoutSpace();
        }

        void DrawEvents()
        {
            EvoEditorGUI.BeginVerticalBackground();
            if (EvoEditorGUI.DrawFoldout(ref eventsFoldout, "Events", EvoEditorGUI.GetIcon("UI_Event")))
            {
                EvoEditorGUI.BeginContainer();
                {
                    EvoEditorGUI.DrawProperty(onSelectionChanged, "On Selection Changed", null, true, false);
                }
                EvoEditorGUI.EndContainer();
            }
            EvoEditorGUI.EndVerticalBackground();
        }
    }
}