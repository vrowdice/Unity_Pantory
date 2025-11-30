using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Evo.UI
{
    [DisallowMultipleComponent]
    [HelpURL(Constants.HELP_URL + "ui-elements/radio-button")]
    [AddComponentMenu("Evo/UI/UI Elements/Radio Button Group")]
    public class RadioButtonGroup : MonoBehaviour
    {
        [EvoHeader("Settings", Constants.CUSTOM_EDITOR_ID)]
        public int selectedIndex = -1;
        [SerializeField] private bool allowDeselection = false;

        [EvoHeader("Events", Constants.CUSTOM_EDITOR_ID)]
        [SerializeField] private UnityEvent<int> onSelectionChanged = new(); 

        // Helpers
        Button selectedButton;
        readonly List<Button> availableButtons = new();

        void Awake()
        {
            Initialize();
        }

        public void Initialize()
        {
            selectedButton = null;
            availableButtons.Clear();

            // Get all Button components from direct children only
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (child.TryGetComponent<Button>(out Button btn))
                {
                    availableButtons.Add(btn);
                    int btnIndex = availableButtons.Count - 1;
                    btn.onClick.AddListener(() => SetButton(btnIndex));
                }

                else
                {
                    // Add null to maintain index consistency
                    availableButtons.Add(null);
                }
            }

            // Set default selection if specified
            if (selectedIndex >= 0 && selectedIndex < availableButtons.Count)
            {
                SetButton(selectedIndex);
            }
        }

        public void SetButton(int index)
        {
            if (index < 0 || index >= availableButtons.Count)
            {
                Debug.LogWarning($"[Radio Button Group] Invalid button index {index}. Valid range: 0-{availableButtons.Count - 1}", this);
                return;
            }

            Button targetButton = availableButtons[index];
            if (targetButton == null)
            {
                Debug.LogWarning($"[Radio Button Group] No button found at index {index}", this);
                return;
            }

            // Allow deselection if enabled and clicking the same button
            if (allowDeselection && selectedButton == targetButton)
            {
                DeselectAll();
                return;
            }

            // Don't do anything if this button is already selected
            if (selectedButton == targetButton) { return; }

            // Deselect current button
            if (selectedButton != null) 
            {
                selectedButton.SetState(InteractionState.Normal);
            }

            // Select new button
            targetButton.SetState(InteractionState.Selected);
            selectedButton = targetButton;
            selectedIndex = index;

            // Notify listeners
            onSelectionChanged?.Invoke(index);
        }

        public void DeselectAll()
        {
            if (selectedButton != null)
            {
                selectedButton.SetState(InteractionState.Normal);
                selectedButton = null;
                selectedIndex = -1;
                onSelectionChanged?.Invoke(-1);
            }
        }

        public bool IsButtonSelected(int index)
        {
            return selectedIndex == index;
        }

        public void SetInteractable(bool interactable)
        {
            foreach (Button btn in availableButtons)
            {
                if (btn != null)
                {
                    btn.interactable = interactable;
                }
            }
        }
    }
}