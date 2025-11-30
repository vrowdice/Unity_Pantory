using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Evo.UI
{
    [DisallowMultipleComponent]
    [HelpURL(Constants.HELP_URL + "ui-elements/context-menu")]
    [AddComponentMenu("Evo/UI/UI Elements/Context Menu")]
    public class ContextMenu : MonoBehaviour, IPointerClickHandler
    {
        [EvoHeader("Content", Constants.CUSTOM_EDITOR_ID)]
        public List<Item> menuItems = new();

        [EvoHeader("Settings", Constants.CUSTOM_EDITOR_ID)]
        public bool is3DObject = false;
        public bool closeOnItemClick = true;
        public bool closeOnOutsideClick = true;
        public MouseButton triggerButton = MouseButton.Right;

        [EvoHeader("Animation", Constants.CUSTOM_EDITOR_ID)]
        public AnimationType animationType = AnimationType.Slide;
        [Range(0.01f, 1)] public float animationDuration = 0.1f;
        public AnimationCurve animationCurve = new(new Keyframe(0, 0, 0, 2), new Keyframe(1, 1, 0, 0));
        [Range(0f, 1f)] public float scaleFrom = 0.8f;
        public Vector2 slideOffset = new(0, -20);

        [EvoHeader("Position & Offset", Constants.CUSTOM_EDITOR_ID)]
        public OffsetPosition offsetPosition = OffsetPosition.BottomRight;
        public Vector2 customOffset = new(10, 10);
        public float offsetDistance = 10;
        public float screenEdgePadding = 10;

#if EVO_LOCALIZATION
        [EvoHeader("Localization", Constants.CUSTOM_EDITOR_ID)]
        public bool enableLocalization = true;
        public Localization.LocalizedObject localizedObject;
#endif

        [EvoHeader("References", Constants.CUSTOM_EDITOR_ID)]
        public GameObject menuPreset;
        public Canvas targetCanvas;

        [EvoHeader("Events", Constants.CUSTOM_EDITOR_ID)]
        public UnityEvent onShow = new();
        public UnityEvent onHide = new();

        public enum AnimationType
        {
            None = 0,
            Fade = 1,
            Scale = 2,
            Slide = 3
        }

        public enum MouseButton
        {
            Left = 0,
            Right = 1,
            Middle = 2
        }

        // Instance variables
        ContextMenuPreset menuInstance;
        Coroutine animationCoroutine;
        Coroutine outsideCoroutine;
        Vector3 lastClickPosition;
        Vector3 targetPosition;
        readonly static List<ContextMenu> activeMenus = new();

        [System.Serializable]
        public class Item
        {
            [Header("Basic Properties")]
            public string itemName = "Menu Item";
            public ItemType itemType = ItemType.Button;
            public Sprite icon;

#if EVO_LOCALIZATION
            [Header("Localization")]
            public string tableKey;
#endif

            [Header("Button Settings")]
            public UnityEvent onClick = new();

            [Header("Section Settings")]
            public bool expandOnHover = true;
            public List<SectionItem> sectionItems = new();

            [Header("Custom Object")]
            public GameObject customPrefab;

            public enum ItemType
            {
                Button = 0,
                Separator = 1,
                Section = 2,
                CustomObject = 3
            }
        }

        [System.Serializable]
        public class SectionItem
        {
            [Header("Basic Properties")]
            public string itemName = "Menu Item";
            public ItemType itemType = ItemType.Button;
            public Sprite icon;
            public bool enabled = true;

#if EVO_LOCALIZATION
            [Header("Localization")]
            public string tableKey;
#endif

            [Header("Button Settings")]
            public UnityEvent onClick = new();

            [Header("Custom Object")]
            public GameObject customPrefab;

            public enum ItemType
            {
                Button = 0,
                Separator = 1,
                CustomObject = 2
            }
        }

        void Start()
        {
            if (menuPreset == null)
            {
                Debug.LogError("[Context Menu] 'Menu Preset' is missing. Please select the object and assign a valid context menu preset via the References tab.", this);
                return;
            }

#if EVO_LOCALIZATION
            if (enableLocalization)
            {
                localizedObject = Localization.LocalizedObject.Check(gameObject);
                if (localizedObject != null)
                {
                    Localization.LocalizationManager.OnLanguageChanged += UpdateLocalization;
                    UpdateLocalization();
                }
            }
#endif
        }

        void OnDisable()
        {
            Hide();
        }

        void OnDestroy()
        {
            activeMenus.Remove(this);
#if EVO_LOCALIZATION
            if (enableLocalization && localizedObject != null)
            {
                Localization.LocalizationManager.OnLanguageChanged -= UpdateLocalization;
            }
#endif
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!is3DObject && IsCorrectMouseButton(eventData))
            {
                lastClickPosition = eventData.position;
                Show();
            }
        }

        void OnMouseDown()
        {
            if (is3DObject && Utilities.WasMouseKeyPressed((int)triggerButton))
            {
                lastClickPosition = Utilities.GetPointerPosition();
                Show();
            }
        }

        void SetupMenu()
        {
            if (menuInstance == null)
                return;

            menuInstance.Setup(this, menuItems);
        }

        void PositionMenu()
        {
            if (menuInstance == null) { return; }
            if (!menuInstance.TryGetComponent<RectTransform>(out var menuRect)) { return; }

            // Set pivot based on offset position for proper positioning
            SetMenuPivot(menuRect);

            // Force layout update to get accurate size
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(menuRect);

            // Calculate position
            targetPosition = lastClickPosition + GetOffsetVector();

            // Clamp to screen
            targetPosition = ClampToScreen(targetPosition, menuRect);

            // Set position directly for non-slide animations
            if (animationType != AnimationType.Slide)
            {
                menuRect.position = targetPosition;
            }
        }

        void SetMenuPivot(RectTransform menuRect)
        {
            Vector2 pivot = Vector2.zero;

            switch (offsetPosition)
            {
                case OffsetPosition.TopLeft:
                    pivot = new Vector2(1, 0); // Menu grows from bottom-right corner
                    break;

                case OffsetPosition.TopRight:
                    pivot = new Vector2(0, 0); // Menu grows from bottom-left corner
                    break;

                case OffsetPosition.BottomLeft:
                    pivot = new Vector2(1, 1); // Menu grows from top-right corner
                    break;

                case OffsetPosition.BottomRight:
                    pivot = new Vector2(0, 1); // Menu grows from top-left corner
                    break;

                case OffsetPosition.Top:
                    pivot = new Vector2(0.5f, 0); // Menu grows from bottom-center
                    break;

                case OffsetPosition.Bottom:
                    pivot = new Vector2(0.5f, 1); // Menu grows from top-center
                    break;

                case OffsetPosition.Left:
                    pivot = new Vector2(1, 0.5f); // Menu grows from right-center
                    break;

                case OffsetPosition.Right:
                    pivot = new Vector2(0, 0.5f); // Menu grows from left-center
                    break;

                case OffsetPosition.Custom:
                    pivot = new Vector2(0, 1); // Default to top-left for custom
                    break;
            }

            menuRect.pivot = pivot;
        }

        void SetInitialAnimationState()
        {
            if (menuInstance == null)
                return;

            RectTransform menuRect = menuInstance.GetComponent<RectTransform>();

            switch (animationType)
            {
                case AnimationType.Fade:
                    menuInstance.canvasGroup.alpha = 0f;
                    menuInstance.canvasGroup.blocksRaycasts = true;
                    menuInstance.canvasGroup.interactable = true;
                    break;

                case AnimationType.Scale:
                    menuInstance.canvasGroup.alpha = 0f;
                    menuInstance.canvasGroup.blocksRaycasts = true;
                    menuInstance.canvasGroup.interactable = true;
                    menuInstance.transform.localScale = Vector3.one * scaleFrom;
                    break;

                case AnimationType.Slide:
                    menuInstance.canvasGroup.alpha = 0f;
                    menuInstance.canvasGroup.blocksRaycasts = true;
                    menuInstance.canvasGroup.interactable = true;
                    Vector3 startPos = targetPosition + (Vector3)slideOffset;
                    menuRect.position = startPos;
                    break;
            }
        }

        void ApplyAnimationState(float progress, bool isIn)
        {
            if (menuInstance == null)
                return;

            RectTransform menuRect = menuInstance.GetComponent<RectTransform>();

            switch (animationType)
            {
                case AnimationType.Fade:
                    menuInstance.canvasGroup.alpha = progress;
                    break;

                case AnimationType.Scale:
                    menuInstance.canvasGroup.alpha = progress;
                    menuInstance.transform.localScale = Vector3.one * progress;
                    break;

                case AnimationType.Slide:
                    menuInstance.canvasGroup.alpha = progress;
                    if (isIn)
                    {
                        Vector3 startPos = targetPosition + (Vector3)slideOffset;
                        menuRect.position = Vector3.Lerp(startPos, targetPosition, progress);
                    }
                    else
                    {
                        Vector3 endPos = targetPosition + (Vector3)slideOffset;
                        menuRect.position = Vector3.Lerp(targetPosition, endPos, 1f - progress);
                    }
                    break;
            }
        }

        public void Show()
        {
            if (menuPreset == null || IsVisible())
                return;

            // Hide other active menus first
            HideAllActiveMenus();

            // Hide any existing menu
            HideImmediate();

            // Instantiate menu
            GameObject menuGO = Instantiate(menuPreset, ActiveCanvas.transform);
            menuInstance = menuGO.GetComponent<ContextMenuPreset>();

            if (menuInstance == null)
            {
                Destroy(menuGO);
                return;
            }

            // Setup menu
            SetupMenu();

            // Position menu
            PositionMenu();

            // Add to active menus
            activeMenus.Add(this);

            // Check for outside detection
            if (closeOnOutsideClick)
            {
                if (outsideCoroutine != null) { StopCoroutine(outsideCoroutine); outsideCoroutine = null; }
                outsideCoroutine = StartCoroutine(DetectOutsideClick());
            }

            // Start animation
            if (animationType != AnimationType.None) 
            {
                if (animationCoroutine != null) { StopCoroutine(animationCoroutine); }
                animationCoroutine = StartCoroutine(AnimateMenuIn()); 
            }

            // Invoke events
            onShow?.Invoke();
        }

        public void Hide()
        {
            if (animationCoroutine != null)
            {
                StopCoroutine(animationCoroutine);
                animationCoroutine = null;
            }

            // Check for outside detection
            if (closeOnOutsideClick && outsideCoroutine != null)
            {
                StopCoroutine(outsideCoroutine);
                outsideCoroutine = null;
            }

            // Start animation
            if (menuInstance != null && animationType != AnimationType.None && gameObject.activeInHierarchy) 
            {
                if (animationCoroutine != null) { StopCoroutine(animationCoroutine); }
                animationCoroutine = StartCoroutine(AnimateMenuOut());
            }
            else { HideImmediate(); }

            // Remove from active menus
            activeMenus.Remove(this);

            // Invoke events
            onHide?.Invoke();
        }

        public void HideImmediate()
        {
            if (menuInstance != null)
            {
                // Destroy all submenus first
                ContextMenuSection[] sections = menuInstance.GetComponentsInChildren<ContextMenuSection>();
                foreach (var section in sections) { section.DestroySubmenuImmediate(); }

                Destroy(menuInstance.gameObject);
                menuInstance = null;
            }
        }

        public void OnItemClicked(Item item)
        {
            if (closeOnItemClick && item.itemType != Item.ItemType.Section)
            {
                Hide();
            }
        }

        public bool IsVisible()
        {
            return menuInstance != null;
        }

        public void SetTriggerButton(int index)
        {
            triggerButton = (MouseButton)index;
        }

        bool IsCorrectMouseButton(PointerEventData eventData)
        {
            return triggerButton switch
            {
                MouseButton.Left => eventData.button == PointerEventData.InputButton.Left,
                MouseButton.Right => eventData.button == PointerEventData.InputButton.Right,
                MouseButton.Middle => eventData.button == PointerEventData.InputButton.Middle,
                _ => false,
            };
        }

        bool IsPointerOverMenuOrSubmenus()
        {
            if (menuInstance == null)
                return false;

            // Get all UI elements under the mouse
            PointerEventData pointerData = new(EventSystem.current) { position = Utilities.GetPointerPosition() };
            List<RaycastResult> results = new();
            EventSystem.current.RaycastAll(pointerData, results);

            // Check if any of the hit elements belong to our menu or any submenu
            foreach (var result in results)
            {
                Transform current = result.gameObject.transform;

                // Check if it's part of the main menu
                if (current.IsChildOf(menuInstance.transform)) { return true; }

                // Check all sections for submenus
                ContextMenuSection[] allSections = menuInstance.GetComponentsInChildren<ContextMenuSection>();
                foreach (var section in allSections)
                {
                    if (section.submenuInstance != null && current.IsChildOf(section.submenuInstance.transform))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        Vector3 GetOffsetVector()
        {
            if (offsetPosition == OffsetPosition.Custom) { return (Vector3)customOffset; }
            Vector3 offset = Vector3.zero;

            switch (offsetPosition)
            {
                case OffsetPosition.TopLeft:
                    offset = new Vector3(-offsetDistance, offsetDistance, 0);
                    break;
                case OffsetPosition.TopRight:
                    offset = new Vector3(offsetDistance, offsetDistance, 0);
                    break;
                case OffsetPosition.BottomLeft:
                    offset = new Vector3(-offsetDistance, -offsetDistance, 0);
                    break;
                case OffsetPosition.BottomRight:
                    offset = new Vector3(offsetDistance, -offsetDistance, 0);
                    break;
                case OffsetPosition.Top:
                    offset = new Vector3(0, offsetDistance, 0);
                    break;
                case OffsetPosition.Bottom:
                    offset = new Vector3(0, -offsetDistance, 0);
                    break;
                case OffsetPosition.Left:
                    offset = new Vector3(-offsetDistance, 0, 0);
                    break;
                case OffsetPosition.Right:
                    offset = new Vector3(offsetDistance, 0, 0);
                    break;
            }

            return offset;
        }

        Vector3 ClampToScreen(Vector3 position, RectTransform menuRect)
        {
            // Get menu size
            float width = menuRect.rect.width * menuRect.lossyScale.x;
            float height = menuRect.rect.height * menuRect.lossyScale.y;

            // Get pivot offsets
            float pivotOffsetX = width * menuRect.pivot.x;
            float pivotOffsetY = height * menuRect.pivot.y;

            // Calculate actual bounds of the menu
            float minX = position.x - pivotOffsetX;
            float maxX = position.x + (width - pivotOffsetX);
            float minY = position.y - pivotOffsetY;
            float maxY = position.y + (height - pivotOffsetY);

            // Adjust position if menu goes out of screen bounds
            if (minX < screenEdgePadding) { position.x += (screenEdgePadding - minX); }
            if (maxX > Screen.width - screenEdgePadding) { position.x -= (maxX - (Screen.width - screenEdgePadding)); }
            if (minY < screenEdgePadding) { position.y += (screenEdgePadding - minY); }
            if (maxY > Screen.height - screenEdgePadding) { position.y -= (maxY - (Screen.height - screenEdgePadding)); }

            return position;
        }

        IEnumerator DetectOutsideClick()
        {
            while (true)
            {
                // Check for ESC key to close menu
                if (IsVisible() && Utilities.WasEscapeKeyPressed())
                {
                    Hide();
                    yield return null;
                    continue;
                }

                // Check for any mouse button clicks
                if (IsVisible() && Utilities.WasPointerPressed())
                {
                    // Wait one frame to let UI events process
                    yield return null;

                    // Check if the click was outside the menu
                    if (!IsPointerOverMenuOrSubmenus()) { Hide(); }
                }

                yield return null;
            }
        }

        IEnumerator AnimateMenuIn()
        {
            if (menuInstance == null || menuInstance.canvasGroup == null)
                yield break;

            SetInitialAnimationState();

            float elapsedTime = 0f;
            while (elapsedTime < animationDuration)
            {
                elapsedTime += Time.unscaledDeltaTime;
                float progress = elapsedTime / animationDuration;
                float curveValue = animationCurve.Evaluate(progress);

                ApplyAnimationState(curveValue, true);

                yield return null;
            }

            // Ensure final state
            ApplyAnimationState(1f, true);
            animationCoroutine = null;
        }

        IEnumerator AnimateMenuOut()
        {
            if (menuInstance == null || menuInstance.canvasGroup == null)
            {
                HideImmediate();
                yield break;
            }

            menuInstance.canvasGroup.blocksRaycasts = false;
            menuInstance.canvasGroup.interactable = false;

            float elapsedTime = 0f;
            while (elapsedTime < animationDuration)
            {
                elapsedTime += Time.unscaledDeltaTime;
                float progress = elapsedTime / animationDuration;
                float curveValue = 1f - progress; // Reverse for out animation

                ApplyAnimationState(curveValue, false);

                yield return null;
            }

            HideImmediate();
        }

        static void HideAllActiveMenus()
        {
            for (int i = activeMenus.Count - 1; i >= 0; i--)
            {
                if (activeMenus[i] != null) { activeMenus[i].Hide(); }
                else { activeMenus.RemoveAt(i); }
            }
        }

        public Canvas ActiveCanvas
        {
            get
            {
                if (targetCanvas != null) { return targetCanvas; }
                else
                {
                    var tempCanvas = GetComponentInParent<Canvas>();
                    if (tempCanvas != null)
                    {
                        targetCanvas = tempCanvas;
                        return targetCanvas;
                    }
                    return Globals.GetCanvas();
                }
            }
        }

#if EVO_LOCALIZATION
        void UpdateLocalization()
        {
            foreach (Item item in menuItems)
            {
                if (!string.IsNullOrEmpty(item.tableKey))
                {
                    item.itemName = localizedObject.GetString(item.tableKey);
                }
            }
        }
#endif


#if UNITY_EDITOR
        [HideInInspector] public bool contentFoldout = true;
        [HideInInspector] public bool settingsFoldout = false;
        [HideInInspector] public bool referencesFoldout = false;
        [HideInInspector] public bool eventsFoldout = false;
#endif
    }
}