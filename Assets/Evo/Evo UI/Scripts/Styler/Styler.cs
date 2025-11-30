using UnityEngine;
using TMPro;

namespace Evo.UI
{
    public class Styler
    {
        /// <summary>
        /// Identify the item type.
        /// </summary>
        public enum ItemType
        {
            Audio,
            Color,
            Font
        }

        /// <summary>
        /// Used in StylerPreset to set the update mode.
        /// </summary>
        public enum UpdateMode
        {
            Always = 0,     // Updates every frame in both Editor and Play mode
            Adaptive = 1,   // Updates every frame in the Editor, but only once in Play mode
        }

        /// <summary>
        /// Used to specify AudioClip values. 
        /// </summary>
        [System.Serializable]
        public class AudioItem
        {
            public string itemID;
            public AudioClip audioAsset;
#if UNITY_EDITOR
            [System.NonSerialized] public bool isExpanded = true;
#endif
            public AudioItem(string id = "", AudioClip asset = null)
            {
                itemID = id;
                audioAsset = asset;
            }
        }

        /// <summary>
        /// Used to specify Color values. 
        /// </summary>
        [System.Serializable]
        public class ColorItem
        {
            public string itemID;
            public Color colorValue;
#if UNITY_EDITOR
            [System.NonSerialized] public bool isExpanded = true;
#endif
            public ColorItem(string id = "", Color color = default)
            {
                itemID = id;
                colorValue = color;
            }
        }

        /// <summary>
        /// Used to specify TMP Font values. 
        /// </summary>
        [System.Serializable]
        public class FontItem
        {
            public string itemID;
            public TMP_FontAsset fontAsset;
#if UNITY_EDITOR
            [System.NonSerialized] public bool isExpanded = true;
#endif
            public FontItem(string id = "", TMP_FontAsset asset = null)
            {
                itemID = id;
                fontAsset = asset;
            }
        }

        /// <summary>
        /// Gets the default preset from the Resources folder.
        /// </summary>
        public static StylerPreset GetDefaultPreset(bool generateLog = true)
        {
            // Try the expected path first
            StylerPreset defaultPreset = Resources.Load<StylerPreset>(Constants.DEFAULT_STYLER_PRESET);
            if (defaultPreset != null) { return defaultPreset; }

            // Return null if no asset is fetched
            if (generateLog) { Debug.LogWarning($"No Styler Preset found anywhere in Resources folders!"); }
            return null;
        }

        /// <summary>
        /// Get an audio clip from the provided mapping based on the specified source.
        /// </summary>
        public static AudioClip GetAudio(StylingSource source, AudioMapping mapping, StylerPreset stylerPreset)
        {
            // Early return in case source is set to none
            if (source == StylingSource.None) { return null; }

            // Check for StylerPreset
            if (source == StylingSource.StylerPreset && stylerPreset != null) { return stylerPreset.GetAudio(mapping.stylerID); }

            // Fallback to custom audio
            return mapping.audioClip;
        }

        /// <summary>
        /// Get a color from the provided mapping based on the specified source.
        /// </summary>
        public static Color GetColor(StylingSource source, ColorMapping mapping, StylerPreset stylerPreset)
        {
            // Early return in case source is set to none
            if (source == StylingSource.None) { return Color.clear; }

            // Check for StylerPreset
            if (source == StylingSource.StylerPreset && stylerPreset != null)
            {
                // If stylerID is empty or null, return transparent
                if (string.IsNullOrEmpty(mapping.stylerID)) { return Color.clear; }
                return stylerPreset.GetColor(mapping.stylerID);
            }

            // Fallback to custom color
            return mapping.color;
        }

        /// <summary>
        /// Get a TMP font from the provided mapping based on the specified source.
        /// </summary>
        public static TMP_FontAsset GetFont(StylingSource source, FontMapping mapping, StylerPreset stylerPreset)
        {
            // Early return in case source is set to none
            if (source == StylingSource.None) { return null; }

            // Check for StylerPreset
            if (source == StylingSource.StylerPreset && stylerPreset != null) { return stylerPreset.GetFont(mapping.stylerID); }

            // Fallback to custom font
            return mapping.font;
        }
    }
}