using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Evo.UI
{
    [DisallowMultipleComponent]
    [HelpURL(Constants.HELP_URL + "ui-elements/context-menu")]
    public class ContextMenuSection : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Header("References")]
        public Button sectionButton;
        public Transform submenuContainer;
        public GameObject submenuPrefab;

        [Header("Settings")]
        [Range(0, 2)] public float expandDelay = 0.2f;
        public Vector2 submenuOffset = new(5f, 0f);

        [Header("Animation")]
        public bool enableAnimation = true;
        public float animationDuration = 0.2f;
        public AnimationCurve animationCurve = new(new Keyframe(0, 0, 0, 2), new Keyframe(1, 1, 0, 0));

        // Instance variables
        bool isExpanded = false;
        bool submenuOnLeft = false;
        ContextMenu sourceMenu;
        ContextMenu.Item sectionItem;
        Coroutine expandCoroutine;
        Coroutine hoverCheckCoroutine;
        Coroutine animationCoroutine;
        [HideInInspector] public ContextMenuPreset submenuInstance;

        // Static tracking for hover sections
        static ContextMenuSection currentHoveredSection;

        void OnDestroy()
        {
            StopAllCoroutines();
            DestroySubmenuImmediate();
            if (currentHoveredSection == this) { currentHoveredSection = null; }
        }

        public void Setup(ContextMenu source, ContextMenu.Item item)
        {
            sourceMenu = source;
            sectionItem = item;

            if (sectionButton != null)
            {
                sectionButton.onClick.AddListener(OnSectionClicked);
                sectionButton.text = item.itemName;
                sectionButton.icon = item.icon;
                sectionButton.UpdateUI();
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            // Close other section submenus (but not child sections)
            if (currentHoveredSection != null && currentHoveredSection != this && !IsChildOfSection(currentHoveredSection)) { currentHoveredSection.CollapseSubmenu(); }
            currentHoveredSection = this;

            // Stop any pending close check
            if (hoverCheckCoroutine != null)
            {
                StopCoroutine(hoverCheckCoroutine);
                hoverCheckCoroutine = null;
            }

            // Start expand timer if hover expand is enabled
            if (sectionItem != null && sectionItem.expandOnHover && !isExpanded)
            {
                if (expandCoroutine != null) { StopCoroutine(expandCoroutine); }
                expandCoroutine = StartCoroutine(ExpandAfterDelay());
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            // Cancel any pending expand operation
            if (expandCoroutine != null)
            {
                StopCoroutine(expandCoroutine);
                expandCoroutine = null;
            }

            // Start checking if we should close the submenu
            if (isExpanded && sectionItem != null && sectionItem.expandOnHover)
            {
                if (hoverCheckCoroutine != null) { StopCoroutine(hoverCheckCoroutine); }
                hoverCheckCoroutine = StartCoroutine(CheckAndCloseSubmenu());
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            OnSectionClicked();
        }

        void OnSectionClicked()
        {
            // Close other section submenus (but not child sections)
            if (currentHoveredSection != null && currentHoveredSection != this && !IsChildOfSection(currentHoveredSection)) { currentHoveredSection.CollapseSubmenu(); }
            currentHoveredSection = this;

            if (isExpanded) { CollapseSubmenu(); }
            else { ExpandSubmenu(); }
        }

        void ExpandSubmenu()
        {
            if (sectionItem == null || sectionItem.sectionItems.Count == 0 || isExpanded)
                return;

            CreateSubmenu();
            isExpanded = true;
            sectionButton.SetState(InteractionState.Selected);
        }

        public void CollapseSubmenu(bool immediate = false)
        {
            if (!isExpanded)
                return;

            StopAllCoroutines();

            // Also collapse any child submenus
            CollapseAllChildSubmenus();

            if (immediate || !enableAnimation || submenuInstance == null) { DestroySubmenu(); }
            else
            {
                if (animationCoroutine != null) { StopCoroutine(animationCoroutine); }
                animationCoroutine = StartCoroutine(AnimateSubmenuOut());
            }

            isExpanded = false;

            if (currentHoveredSection == this) 
            { 
                currentHoveredSection = null; 
            }

            sectionButton.SetState(InteractionState.Normal);
        }

        void CreateSubmenu()
        {
            if (submenuPrefab == null)
                return;

            // Create as child of main menu for proper fade inheritance
            ContextMenuPreset mainMenu = GetComponentInParent<ContextMenuPreset>();
            Transform parentTransform = mainMenu != null ? mainMenu.transform : sourceMenu.ActiveCanvas.transform;

            GameObject submenuGO = Instantiate(submenuPrefab, parentTransform);
            submenuInstance = submenuGO.GetComponent<ContextMenuPreset>();

            if (submenuInstance == null)
            {
                Destroy(submenuGO);
                return;
            }

            // Use the SectionItem setup method instead of Item setup
            submenuInstance.Setup(sourceMenu, sectionItem.sectionItems);

            if (submenuInstance.canvasGroup == null) { submenuInstance.canvasGroup = submenuInstance.gameObject.AddComponent<CanvasGroup>(); }
            if (sectionItem.expandOnHover) { SetupSubmenuHoverHandlers(); }

            PositionSubmenu();

            if (enableAnimation)
            {
                if (animationCoroutine != null) { StopCoroutine(animationCoroutine); }
                animationCoroutine = StartCoroutine(AnimateSubmenuIn());
            }
        }

        void SetupSubmenuHoverHandlers()
        {
            if (submenuInstance == null) { return; }
            if (!submenuInstance.gameObject.TryGetComponent<EventTrigger>(out var trigger)) { trigger = submenuInstance.gameObject.AddComponent<EventTrigger>(); }

            // Cancel close when entering submenu
            EventTrigger.Entry enterEntry = new() { eventID = EventTriggerType.PointerEnter };
            enterEntry.callback.AddListener((data) => {
                if (hoverCheckCoroutine != null)
                {
                    StopCoroutine(hoverCheckCoroutine);
                    hoverCheckCoroutine = null;
                }
            });
            trigger.triggers.Add(enterEntry);

            // Check for close when leaving submenu
            EventTrigger.Entry exitEntry = new() { eventID = EventTriggerType.PointerExit };
            exitEntry.callback.AddListener((data) => {
                if (hoverCheckCoroutine != null) { StopCoroutine(hoverCheckCoroutine); }
                hoverCheckCoroutine = StartCoroutine(CheckAndCloseSubmenu());
            });
            trigger.triggers.Add(exitEntry);
        }

        // Check if this section is a child of another section
        bool IsChildOfSection(ContextMenuSection otherSection)
        {
            if (otherSection == null || otherSection.submenuInstance == null) { return false; }
            return transform.IsChildOf(otherSection.submenuInstance.transform);
        }

        // Check if mouse is specifically over this section button
        bool IsMouseOverSection()
        {
            PointerEventData pointerData = new(EventSystem.current) { position = Utilities.GetPointerPosition() };
            List<RaycastResult> results = new();
            EventSystem.current.RaycastAll(pointerData, results);

            foreach (var result in results)
            {
                // Check if mouse is over this section button specifically
                if (result.gameObject.transform.IsChildOf(transform))
                {
                    return true;
                }
            }

            return false;
        }

        // Hover detection that accounts for nested submenus
        bool IsMouseOverSectionOrSubmenu()
        {
            PointerEventData pointerData = new(EventSystem.current) { position = Utilities.GetPointerPosition() };
            List<RaycastResult> results = new();
            EventSystem.current.RaycastAll(pointerData, results);

            foreach (var result in results)
            {
                // Check if mouse is over this section button
                if (result.gameObject.transform.IsChildOf(transform)) { return true; }

                // Check if mouse is over this submenu or any nested submenus
                if (IsMouseOverSubmenuHierarchy(result.gameObject.transform)) { return true; }
            }

            return false;
        }

        // Recursively check if mouse is over submenu hierarchy
        bool IsMouseOverSubmenuHierarchy(Transform target)
        {
            if (submenuInstance == null) { return false; }
            if (target.IsChildOf(submenuInstance.transform)) { return true; }

            // Check all child sections' submenus recursively
            ContextMenuSection[] childSections = submenuInstance.GetComponentsInChildren<ContextMenuSection>();
            foreach (var childSection in childSections)
            {
                if (childSection != this && childSection.IsMouseOverSubmenuHierarchy(target))
                {
                    return true;
                }
            }

            return false;
        }

        // Collapse all child submenus recursively
        void CollapseAllChildSubmenus()
        {
            if (submenuInstance == null) return;

            ContextMenuSection[] childSections = submenuInstance.GetComponentsInChildren<ContextMenuSection>();
            foreach (var childSection in childSections)
            {
                if (childSection != this)
                {
                    childSection.CollapseSubmenu(true);
                }
            }
        }

        void PositionSubmenu()
        {
            if (submenuInstance == null)
                return;

            RectTransform submenuRect = submenuInstance.GetComponent<RectTransform>();
            RectTransform sectionRect = GetComponent<RectTransform>();

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(submenuRect);

            // Get the canvas for scale calculations
            Canvas canvas = sourceMenu != null ? sourceMenu.ActiveCanvas : GetComponentInParent<Canvas>();
            float canvasScaleFactor = canvas != null ? canvas.scaleFactor : 1f;

            Vector3 sectionWorldPos = sectionRect.position;
            Vector3 submenuPos = sectionWorldPos;

            // Calculate scaled dimensions
            float sectionWidth = sectionRect.rect.width * sectionRect.lossyScale.x;
            float submenuWidth = submenuRect.rect.width * submenuRect.lossyScale.x;
            float scaledOffsetX = submenuOffset.x * canvasScaleFactor;
            float scaledOffsetY = submenuOffset.y * canvasScaleFactor;
            float screenPadding = 10f;

            // Try right side first
            submenuPos.x += sectionWidth + scaledOffsetX;
            submenuPos.y += scaledOffsetY;

            // Check if submenu goes off right edge of screen
            float submenuRightEdge = submenuPos.x + submenuWidth;
            if (submenuRightEdge > Screen.width - screenPadding)
            {
                // Position on left side
                submenuPos.x = sectionWorldPos.x - submenuWidth - scaledOffsetX;
                submenuOnLeft = true;
            }
            else
            {
                submenuOnLeft = false;
            }

            submenuPos = ClampSubmenuToScreen(submenuPos, submenuRect);
            submenuRect.position = submenuPos;
        }

        void DestroySubmenu()
        {
            if (submenuInstance != null)
            {
                Destroy(submenuInstance.gameObject);
                submenuInstance = null;
            }
        }

        public void DestroySubmenuImmediate()
        {
            if (submenuInstance != null)
            {
                Destroy(submenuInstance.gameObject);
                submenuInstance = null;
                isExpanded = false;
            }
        }

        Vector3 ClampSubmenuToScreen(Vector3 position, RectTransform submenuRect)
        {
            // Use scaled dimensions
            float width = submenuRect.rect.width * submenuRect.lossyScale.x;
            float height = submenuRect.rect.height * submenuRect.lossyScale.y;
            float padding = 10f;

            position.x = Mathf.Clamp(position.x, padding, Screen.width - width - padding);
            position.y = Mathf.Clamp(position.y, padding, Screen.height - height - padding);

            return position;
        }

        IEnumerator ExpandAfterDelay()
        {
            yield return new WaitForSeconds(expandDelay);

            // Only expand if we're still being hovered and not already expanded
            if (!isExpanded && IsMouseOverSection()) { ExpandSubmenu(); }

            expandCoroutine = null;
        }

        // Close checking with longer delay for nested menus
        IEnumerator CheckAndCloseSubmenu()
        {
            // Use a longer delay for nested menus to give user time to navigate
            float delay = GetSubmenuDepth() > 1 ? 0.25f : 0.15f;
            yield return new WaitForSecondsRealtime(delay);

            if (!IsMouseOverSectionOrSubmenu()) { CollapseSubmenu(); }
            hoverCheckCoroutine = null;
        }

        // Get the depth of nested submenus
        int GetSubmenuDepth()
        {
            int depth = 0;
            Transform current = transform;

            while (current != null)
            {
                ContextMenuPreset menu = current.GetComponentInParent<ContextMenuPreset>();
                if (menu == null) break;

                ContextMenuSection parentSection = menu.GetComponentInParent<ContextMenuSection>();
                if (parentSection == null) break;

                depth++;
                current = parentSection.transform;
            }

            return depth;
        }

        IEnumerator AnimateSubmenuIn()
        {
            if (submenuInstance == null)
                yield break;

            CanvasGroup canvasGroup = submenuInstance.canvasGroup;
            RectTransform submenuRect = submenuInstance.GetComponent<RectTransform>();

            if (canvasGroup == null || submenuRect == null)
                yield break;

            Vector3 finalPosition = submenuRect.position;
            Vector3 startPosition = finalPosition;
            startPosition.x += submenuOnLeft ? 20f : -20f;

            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
            submenuRect.position = startPosition;

            float elapsedTime = 0f;
            while (elapsedTime < animationDuration)
            {
                elapsedTime += Time.unscaledDeltaTime;
                float progress = elapsedTime / animationDuration;
                float curveValue = animationCurve.Evaluate(progress);

                canvasGroup.alpha = curveValue;
                submenuRect.position = Vector3.Lerp(startPosition, finalPosition, curveValue);

                yield return null;
            }

            canvasGroup.alpha = 1f;
            submenuRect.position = finalPosition;
            animationCoroutine = null;
        }

        IEnumerator AnimateSubmenuOut()
        {
            if (submenuInstance == null)
                yield break;

            CanvasGroup canvasGroup = submenuInstance.canvasGroup;
            RectTransform submenuRect = submenuInstance.GetComponent<RectTransform>();

            if (canvasGroup == null || submenuRect == null)
            {
                DestroySubmenu();
                yield break;
            }

            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            Vector3 startPosition = submenuRect.position;
            Vector3 endPosition = startPosition;
            endPosition.x += submenuOnLeft ? 20f : -20f;

            float startAlpha = canvasGroup.alpha;
            float elapsedTime = 0f;

            while (elapsedTime < animationDuration)
            {
                elapsedTime += Time.unscaledDeltaTime;
                float progress = elapsedTime / animationDuration;

                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, progress);
                submenuRect.position = Vector3.Lerp(startPosition, endPosition, progress);

                yield return null;
            }

            DestroySubmenu();
            animationCoroutine = null;
        }
    }
}