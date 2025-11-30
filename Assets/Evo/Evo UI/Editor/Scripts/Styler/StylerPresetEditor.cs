using System.Linq;
using UnityEditor;
using UnityEngine;
using Evo.EditorTools;

namespace Evo.UI
{
    [CustomEditor(typeof(StylerPreset))]
    public class StylerPresetEditor : Editor
    {
        StylerPreset spTarget;

        // Properties
        SerializedProperty audioItems;
        SerializedProperty colorItems;
        SerializedProperty fontItems;
        SerializedProperty updateMode;

        void OnEnable()
        {
            spTarget = (StylerPreset)target;

            audioItems = serializedObject.FindProperty("audioItems");
            colorItems = serializedObject.FindProperty("colorItems");
            fontItems = serializedObject.FindProperty("fontItems");
            updateMode = serializedObject.FindProperty("updateMode");

            // Register this editor for hover repaints
            EvoEditorGUI.RegisterEditor(this);
        }

        void OnDisable()
        {
            // Unregister from hover repaints
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
            EvoEditorGUI.BeginCenteredInspector(true);

            DrawAudioItems();
            DrawColorItems();
            DrawFontItems();
            DrawSettings();

            EvoEditorGUI.EndCenteredInspector();
            serializedObject.ApplyModifiedProperties();
        }

        void DrawAudioItems()
        {
            EvoEditorGUI.BeginVerticalBackground();

            if (EvoEditorGUI.DrawFoldout(ref spTarget.audioFoldout, "Audio", EvoEditorGUI.GetIcon("UI_Audio")))
            {
                EvoEditorGUI.BeginContainer();
                DrawItemList(audioItems, Styler.ItemType.Audio);
				GUILayout.Space(2);
				if (EvoEditorGUI.DrawButton("New Audio", "Add", height: 20, iconSize: 8, revertBackgroundColor: true))
                {
                    audioItems.arraySize++;
                    var newItem = audioItems.GetArrayElementAtIndex(audioItems.arraySize - 1);
                    newItem.FindPropertyRelative("itemID").stringValue = "New Audio";
                    newItem.FindPropertyRelative("audioAsset").objectReferenceValue = null;
                    newItem.isExpanded = true;
                    EditorUtility.SetDirty(spTarget);
                }
                EvoEditorGUI.EndContainer();
            }

            EvoEditorGUI.EndVerticalBackground();
            EvoEditorGUI.AddFoldoutSpace();
        }

        void DrawColorItems()
        {
            EvoEditorGUI.BeginVerticalBackground();

            if (EvoEditorGUI.DrawFoldout(ref spTarget.colorFoldout, "Color", EvoEditorGUI.GetIcon("UI_Style")))
            {
                EvoEditorGUI.BeginContainer();
                DrawItemList(colorItems, Styler.ItemType.Color);
                GUILayout.Space(2);
                if (EvoEditorGUI.DrawButton("New Color", "Add", height: 20, iconSize: 8, revertBackgroundColor: true))
                {
                    colorItems.arraySize++;
                    var newItem = colorItems.GetArrayElementAtIndex(colorItems.arraySize - 1);
                    newItem.FindPropertyRelative("itemID").stringValue = $"Color {colorItems.arraySize}";
                    newItem.FindPropertyRelative("colorValue").colorValue = Color.white;
                    newItem.isExpanded = true;
                    EditorUtility.SetDirty(spTarget);
                }
                EvoEditorGUI.EndContainer();
            }

            EvoEditorGUI.EndVerticalBackground();
            EvoEditorGUI.AddFoldoutSpace();
        }

        void DrawFontItems()
        {
            EvoEditorGUI.BeginVerticalBackground();

            if (EvoEditorGUI.DrawFoldout(ref spTarget.fontFoldout, "Font", EvoEditorGUI.GetIcon("UI_Text")))
            {
                EvoEditorGUI.BeginContainer();
                DrawItemList(fontItems, Styler.ItemType.Font);
				GUILayout.Space(2);
				if (EvoEditorGUI.DrawButton("New Font", "Add", height: 20, iconSize: 8, revertBackgroundColor: true))
                {
                    fontItems.arraySize++;
                    var newItem = fontItems.GetArrayElementAtIndex(fontItems.arraySize - 1);
                    newItem.FindPropertyRelative("itemID").stringValue = "New Font";
                    newItem.FindPropertyRelative("fontAsset").objectReferenceValue = null;
                    newItem.isExpanded = true;
                    EditorUtility.SetDirty(spTarget);
                }
                EvoEditorGUI.EndContainer();
            }

            EvoEditorGUI.EndVerticalBackground();
            EvoEditorGUI.AddFoldoutSpace();
        }

        void DrawSettings()
        {
            EvoEditorGUI.BeginVerticalBackground();

            if (EvoEditorGUI.DrawFoldout(ref spTarget.settingsFoldout, "Settings", EvoEditorGUI.GetIcon("UI_Settings")))
            {
                EvoEditorGUI.BeginContainer();
                {
                    EvoEditorGUI.BeginVerticalBackground(true);
                    EvoEditorGUI.DrawProperty(updateMode, "Update Mode", "Default update mode for all StylerObjects using this preset.", false, false);
                    EvoEditorGUI.BeginContainer(4);
                    string description = null;
                    if (updateMode.enumValueIndex == 0) { description = "Styler objects are updated in the editor and on every frame at runtime."; }
                    else if(updateMode.enumValueIndex == 1) { description = "Styler objects are always updated in the editor and whenever the object is enabled at runtime."; }
                    GUILayout.Space(2);
                    EvoEditorGUI.DrawInfoBox(description);
                    EvoEditorGUI.EndContainer();
                    EvoEditorGUI.EndVerticalBackground();
                }
                EvoEditorGUI.EndContainer();
            }

            EvoEditorGUI.EndVerticalBackground();
        }

        void DrawItemList(SerializedProperty listProperty, Styler.ItemType itemType)
        {
            for (int i = 0; i < listProperty.arraySize; i++)
            {
                SerializedProperty item = listProperty.GetArrayElementAtIndex(i);
                DrawListItem(item, i, itemType, () =>
                {
                    listProperty.DeleteArrayElementAtIndex(i);
                    EditorUtility.SetDirty(spTarget);
                });
            }
        }

        void DrawListItem(SerializedProperty item, int index, Styler.ItemType itemType, System.Action deleteCallback)
        {
            SerializedProperty itemID = item.FindPropertyRelative("itemID");
            EvoEditorGUI.BeginVerticalBackground(true);

            GUILayout.BeginHorizontal();
            {
                string displayName = string.IsNullOrEmpty(itemID.stringValue) ? $"Item {index}" : itemID.stringValue;

                // Draw color preview box for color items
                if (itemType == Styler.ItemType.Color)
                {
                    SerializedProperty colorValue = item.FindPropertyRelative("colorValue");

                    Rect colorRect = GUILayoutUtility.GetRect(3, 9.5f, GUILayout.ExpandWidth(false));
                    colorRect.x += 8;
                    colorRect.y += 7;
                    EditorGUI.DrawRect(colorRect, colorValue.colorValue);

                    GUILayout.Space(-3);
                    displayName = $"   {displayName}";
                }

                if (EvoEditorGUI.DrawButton(displayName, item.isExpanded ? "Minimize" : "Expand", height: 24, normalColor: Color.clear, iconSize: 8,
                   textAlignment: TextAnchor.MiddleLeft, iconAlignment: EvoEditorGUI.ButtonAlignment.Right))
                {
                    item.isExpanded = !item.isExpanded;
                }

                if (EvoEditorGUI.DrawButton(null, "Delete", "Delete item", iconSize: 8, width: 24, height: 24, normalColor: Color.clear))
                {
                    string itmName = string.IsNullOrEmpty(itemID.stringValue) ? $"Item {index}" : itemID.stringValue;
                    if (EditorUtility.DisplayDialog("Delete Item",
                        $"Are you sure you want to delete '{itmName}'?", "Delete", "Cancel"))
                    {
                        deleteCallback?.Invoke();
                        return;
                    }
                }
            }
            GUILayout.EndHorizontal();

            // Only draw the content if expanded
            if (item.isExpanded)
            {
                EvoEditorGUI.BeginContainer(3);
                {
                    EvoEditorGUI.DrawProperty(itemID, "ID", "Unique identifier for this item.");

                    if (itemType == Styler.ItemType.Audio)
                    {
                        SerializedProperty audioAsset = item.FindPropertyRelative("audioAsset");
                        EvoEditorGUI.DrawProperty(audioAsset, "Audio Clip", "The audio clip for this item.", false);
                    }

                    else if (itemType == Styler.ItemType.Color)
                    {
                        SerializedProperty colorValue = item.FindPropertyRelative("colorValue");
                        EvoEditorGUI.DrawProperty(colorValue, "Color", "The color value for this item.", false);
                    }

                    else if (itemType == Styler.ItemType.Font)
                    {
                        SerializedProperty fontAsset = item.FindPropertyRelative("fontAsset");
                        EvoEditorGUI.DrawProperty(fontAsset, "Font Asset", "The font asset for this item.", false);
                    }
                }
                EvoEditorGUI.EndContainer();
            }

            EvoEditorGUI.EndVerticalBackground(true);
        }
    }
}