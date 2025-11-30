using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Evo.UI
{
    [DisallowMultipleComponent]
    [HelpURL(Constants.HELP_URL + "ui-elements/tooltip")]
    [AddComponentMenu("Evo/UI/UI Elements/Tooltip")]
    public class Tooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [EvoHeader("Content", Constants.CUSTOM_EDITOR_ID)]
        public GameObject tooltipPreset;
        public Sprite icon;
        public string title;
        [TextArea(2, 4)] public string description;

#if EVO_LOCALIZATION
        [EvoHeader("Localization", Constants.CUSTOM_EDITOR_ID)]
        public bool enableLocalization = true;
        public Localization.LocalizedObject localizedObject;
        public string titleKey;
        public string descriptionKey;
#endif

        [EvoHeader("Settings", Constants.CUSTOM_EDITOR_ID)]
        [SerializeField] private bool followCursor = true;
        public bool is3DObject = false;
        [SerializeField] private float maxWidth = 400;
        [SerializeField, Range(0, 10)] private float showDelay = 0;
        [SerializeField, Range(0, 40)] private float movementSmoothing = 0;

        [EvoHeader("Animation", Constants.CUSTOM_EDITOR_ID)]
        public AnimationType animationType = AnimationType.Slide;
        [Range(0.01f, 1)] public float animationDuration = 0.1f;
        public AnimationCurve animationCurve = new(new Keyframe(0, 0, 0, 2), new Keyframe(1, 1, 0, 0));
        [SerializeField, Range(0f, 1f)] private float scaleFrom = 0.7f;
        [SerializeField] private Vector2 slideOffset = new(0, -20);

        [EvoHeader("Position & Offset", Constants.CUSTOM_EDITOR_ID)]
        [SerializeField] private OffsetPosition offsetPosition = OffsetPosition.BottomRight;
        [SerializeField] private Vector2 customOffset = new(10, 10);
        [SerializeField] private float offsetDistance = 30;
        [SerializeField] private float screenEdgePadding = 10;

        [EvoHeader("References", Constants.CUSTOM_EDITOR_ID)]
        [SerializeField] private Canvas tooltipCanvas;
        public GameObject customContent;

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

        // Cache
        TooltipPreset tooltipInstance;
        Coroutine showCoroutine;
        Coroutine positionCoroutine;
        Coroutine animationCoroutine;

        Canvas ActiveCanvas
        {
            get
            {
                if (tooltipCanvas != null) { return tooltipCanvas; }
                else
                {
                    var tempCanvas = GetComponentInParent<Canvas>();
                    if (tempCanvas != null)
                    {
                        tooltipCanvas = tempCanvas;
                        return tooltipCanvas;
                    }
                    return Globals.GetCanvas();
                }
            }
        }

#if EVO_LOCALIZATION
        void Start()
        {
            if (enableLocalization && customContent == null)
            {
                localizedObject = Localization.LocalizedObject.Check(gameObject);
                if (localizedObject != null)
                {
                    Localization.LocalizationManager.OnLanguageChanged += UpdateLocalization;
                    UpdateLocalization();
                }
            }
        }
#endif

        void OnDisable()
        {
            Hide();
        }

        void OnDestroy()
        {
#if EVO_LOCALIZATION
            if (enableLocalization && localizedObject != null) { Localization.LocalizationManager.OnLanguageChanged -= UpdateLocalization; }
#endif
            Hide();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!is3DObject)
            {
                Show();
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!is3DObject)
            {
                Hide();
            }
        }

        void OnMouseEnter()
        {
            if (is3DObject)
            {
                Show();
            }
        }

        void OnMouseExit()
        {
            if (is3DObject)
            {
                Hide();
            }
        }

        void StopAllTooltipCoroutines()
        {
            if (showCoroutine != null)
            {
                StopCoroutine(showCoroutine);
                showCoroutine = null;
            }
            if (positionCoroutine != null)
            {
                StopCoroutine(positionCoroutine);
                positionCoroutine = null;
            }
            if (animationCoroutine != null)
            {
                StopCoroutine(animationCoroutine);
                animationCoroutine = null;
            }
        }

        void SetInitialAnimationState()
        {
            if (tooltipInstance == null)
                return;

            switch (animationType)
            {
                case AnimationType.Fade:
                    tooltipInstance.canvasGroup.alpha = 0f;
                    break;

                case AnimationType.Scale:
                    tooltipInstance.canvasGroup.alpha = 0f;
                    tooltipInstance.transform.localScale = Vector3.one * scaleFrom;
                    break;

                case AnimationType.Slide:
                    tooltipInstance.canvasGroup.alpha = 0f;
                    // Store the current position as base, then apply offset
                    RectTransform tooltipRect = tooltipInstance.GetComponent<RectTransform>();
                    Vector3 currentPos = tooltipRect.anchoredPosition;
                    tooltipRect.anchoredPosition = currentPos + (Vector3)slideOffset;
                    break;
            }
        }

        void ApplyAnimationState(float progress, bool isIn, Vector3 startScale = default)
        {
            if (tooltipInstance == null)
                return;

            switch (animationType)
            {
                case AnimationType.Fade:
                    tooltipInstance.canvasGroup.alpha = progress;
                    break;

                case AnimationType.Scale:
                    tooltipInstance.canvasGroup.alpha = progress;
                    if (isIn)
                    {
                        tooltipInstance.transform.localScale = Vector3.Lerp(Vector3.one * scaleFrom, Vector3.one, progress);
                    }
                    else
                    {
                        tooltipInstance.transform.localScale = Vector3.Lerp(startScale, Vector3.one * scaleFrom, 1f - progress);
                    }
                    break;

                case AnimationType.Slide:
                    tooltipInstance.canvasGroup.alpha = progress;
                    if (isIn)
                    {
                        RectTransform tooltipRect = tooltipInstance.GetComponent<RectTransform>();
                        Vector2 currentTarget = CalculateCurrentTargetPosition();
                        Vector2 slideStartPos = currentTarget + slideOffset;
                        tooltipRect.anchoredPosition = Vector2.Lerp(slideStartPos, currentTarget, progress);
                    }
                    break;
            }
        }

        bool SetupTooltipContent()
        {
            if (tooltipInstance == null)
            {
                Debug.LogError($"Tooltip prefab must have 'Tooltip Preset' component!", this);
                return false;
            }

            if (customContent != null) { tooltipInstance.SetupCustomContent(customContent, maxWidth); }
            else { tooltipInstance.SetupTooltip(title, description, icon, maxWidth); }

            return true;
        }

        Vector2 CalculateTargetPosition(Camera targetCamera)
        {
            Canvas canvas = ActiveCanvas;
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();

            Vector2 screenPoint;

            // Get screen point based on object type
            if (followCursor) { screenPoint = Utilities.GetPointerPosition(); }
            else if (TryGetComponent<RectTransform>(out var rectTransform))
            {
                // For UI elements
                if (canvas.renderMode == RenderMode.ScreenSpaceOverlay) { screenPoint = rectTransform.position; }
                else
                {
                    Camera cam = canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
                    if (cam != null) { screenPoint = RectTransformUtility.WorldToScreenPoint(cam, rectTransform.position); }
                    else { screenPoint = rectTransform.position; }
                }

                // Adjust for non-centered pivot
                Vector2 pivot = rectTransform.pivot;
                Vector2 pivotOffset = new(
                    (pivot.x - 0.5f) * rectTransform.rect.width * rectTransform.lossyScale.x,
                    (pivot.y - 0.5f) * rectTransform.rect.height * rectTransform.lossyScale.y
                );

                screenPoint.x -= pivotOffset.x;
                screenPoint.y -= pivotOffset.y;
            }
            else
            {
                // For 3D objects
                if (targetCamera == null) { targetCamera = Camera.main; }
                screenPoint = targetCamera != null ? targetCamera.WorldToScreenPoint(transform.position) : Vector2.zero;
            }

            // Convert screen point to canvas anchored position
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPoint,
                canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
                out Vector2 anchoredPosition
            );

            return anchoredPosition;
        }

        Camera GetTargetCamera()
        {
            Canvas canvas = ActiveCanvas;
            if (canvas.worldCamera != null) { return canvas.worldCamera; }

            if (TryGetComponent<RectTransform>(out _))
            {
                Canvas parentCanvas = GetComponentInParent<Canvas>();
                if (parentCanvas != null && parentCanvas.worldCamera != null) { return parentCanvas.worldCamera; }
            }

            return Camera.main;
        }

        Vector2 GetOffsetVector()
        {
            if (offsetPosition == OffsetPosition.Custom) { return customOffset; }
            if (tooltipInstance == null) { return Vector2.zero; }
            if (!tooltipInstance.TryGetComponent<RectTransform>(out var tooltipRect)) { return Vector2.zero; }

            // Force layout update to get accurate size
            Canvas.ForceUpdateCanvases();

            float width = tooltipRect.rect.width;
            float height = tooltipRect.rect.height;

            Vector2 offset = Vector2.zero;

            switch (offsetPosition)
            {
                case OffsetPosition.TopLeft:
                    offset = new Vector2(-width * 0.5f - offsetDistance, height * 0.5f + offsetDistance);
                    break;
                case OffsetPosition.TopRight:
                    offset = new Vector2(width * 0.5f + offsetDistance, height * 0.5f + offsetDistance);
                    break;
                case OffsetPosition.BottomLeft:
                    offset = new Vector2(-width * 0.5f - offsetDistance, -height * 0.5f - offsetDistance);
                    break;
                case OffsetPosition.BottomRight:
                    offset = new Vector2(width * 0.5f + offsetDistance, -height * 0.5f - offsetDistance);
                    break;
                case OffsetPosition.Top:
                    offset = new Vector2(0, height * 0.5f + offsetDistance);
                    break;
                case OffsetPosition.Bottom:
                    offset = new Vector2(0, -height * 0.5f - offsetDistance);
                    break;
                case OffsetPosition.Left:
                    offset = new Vector2(-width * 0.5f - offsetDistance, 0);
                    break;
                case OffsetPosition.Right:
                    offset = new Vector2(width * 0.5f + offsetDistance, 0);
                    break;
            }

            return offset;
        }

        Vector2 CalculateCurrentTargetPosition()
        {
            if (tooltipInstance == null)
                return Vector2.zero;

            Camera targetCamera = GetTargetCamera();
            Vector2 currentTarget = CalculateTargetPosition(targetCamera);
            currentTarget += GetOffsetVector();
            return ClampToCanvasBounds(currentTarget, tooltipInstance.GetComponent<RectTransform>());
        }

        Vector2 ClampToCanvasBounds(Vector2 anchoredPosition, RectTransform tooltipRect)
        {
            Canvas canvas = ActiveCanvas;
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();

            float width = tooltipRect.rect.width;
            float height = tooltipRect.rect.height;

            // Calculate canvas bounds in local space
            float canvasWidth = canvasRect.rect.width;
            float canvasHeight = canvasRect.rect.height;

            // Account for padding
            float minX = -canvasWidth * 0.5f + width * 0.5f + screenEdgePadding;
            float maxX = canvasWidth * 0.5f - width * 0.5f - screenEdgePadding;
            float minY = -canvasHeight * 0.5f + height * 0.5f + screenEdgePadding;
            float maxY = canvasHeight * 0.5f - height * 0.5f - screenEdgePadding;

            // Clamp position
            anchoredPosition.x = Mathf.Clamp(anchoredPosition.x, minX, maxX);
            anchoredPosition.y = Mathf.Clamp(anchoredPosition.y, minY, maxY);

            return anchoredPosition;
        }

        IEnumerator ShowTooltipDelayed()
        {
            yield return new WaitForSeconds(showDelay);

            // Verify object is still active and valid
            if (this == null || !gameObject.activeInHierarchy)
                yield break;

            // Instantiate tooltip
            GameObject toGo = Instantiate(tooltipPreset, ActiveCanvas.transform);
            tooltipInstance = toGo.GetComponent<TooltipPreset>();

            // Setup tooltip content
            if (!SetupTooltipContent())
            {
                HideImmediate();
                yield break;
            }

            // Start position updates immediately for all animations
            positionCoroutine = StartCoroutine(UpdateTooltipPosition());

            // Start animations
            if (tooltipInstance != null && animationType != AnimationType.None) { animationCoroutine = StartCoroutine(AnimateTooltipIn()); }

            // Set coroutine
            showCoroutine = null;
        }

        IEnumerator UpdateTooltipPosition()
        {
            if (!tooltipInstance.TryGetComponent<RectTransform>(out var tooltipRect))
                yield break;

            // Cache camera reference
            Camera targetCamera = GetTargetCamera();

            while (tooltipInstance != null && this != null && gameObject.activeInHierarchy)
            {
                Vector2 newTargetPosition = CalculateTargetPosition(targetCamera);
                newTargetPosition += GetOffsetVector();
                newTargetPosition = ClampToCanvasBounds(newTargetPosition, tooltipRect);

                if (movementSmoothing == 0) { tooltipRect.anchoredPosition = newTargetPosition; }
                else
                {
                    Vector2 currentPosition = tooltipRect.anchoredPosition;
                    Vector2 smoothedPosition = Vector2.Lerp(currentPosition, newTargetPosition, movementSmoothing * Time.unscaledDeltaTime);
                    tooltipRect.anchoredPosition = smoothedPosition;
                }

                yield return null;
            }
        }

        IEnumerator AnimateTooltipIn()
        {
            if (tooltipInstance == null || tooltipInstance.canvasGroup == null)
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

            // Set coroutine
            animationCoroutine = null;
        }

        IEnumerator AnimateTooltipOut()
        {
            if (tooltipInstance == null || tooltipInstance.canvasGroup == null)
            {
                HideImmediate();
                yield break;
            }

            // Store initial values
            float startingAlpha = tooltipInstance.canvasGroup.alpha;
            Vector3 startingScale = tooltipInstance.transform.localScale;
            Vector2 startPosition = tooltipInstance.GetComponent<RectTransform>().anchoredPosition;

            float elapsedTime = 0f;

            while (elapsedTime < animationDuration)
            {
                elapsedTime += Time.unscaledDeltaTime;
                float progress = elapsedTime / animationDuration;

                switch (animationType)
                {
                    case AnimationType.Fade:
                        tooltipInstance.canvasGroup.alpha = Mathf.Lerp(startingAlpha, 0f, progress);
                        break;

                    case AnimationType.Scale:
                        tooltipInstance.canvasGroup.alpha = Mathf.Lerp(startingAlpha, 0f, progress);
                        tooltipInstance.transform.localScale = Vector3.Lerp(startingScale, Vector3.one * scaleFrom, progress);
                        break;

                    case AnimationType.Slide:
                        tooltipInstance.canvasGroup.alpha = Mathf.Lerp(startingAlpha, 0f, progress);
                        Vector2 endPos = startPosition + slideOffset;
                        tooltipInstance.GetComponent<RectTransform>().anchoredPosition = Vector2.Lerp(startPosition, endPos, progress);
                        break;
                }

                yield return null;
            }

            HideImmediate();
        }

        public void Show()
        {
            if (tooltipPreset == null)
            {
                Debug.LogWarning($"No tooltip preset assigned to {gameObject.name}. Please assign a prefab with 'Tooltip Preset' component.");
                return;
            }

            // Cancel existing operations
            StopAllTooltipCoroutines();

            // Hide any existing tooltip
            HideImmediate();

            // Start delayed show
            showCoroutine = StartCoroutine(ShowTooltipDelayed());

            // Invoke events
            onShow?.Invoke();
        }

        public void Hide()
        {
            // Cancel existing operations
            StopAllTooltipCoroutines();

            // Hide the tooltip
            if (!gameObject.activeInHierarchy || animationType == AnimationType.None) { HideImmediate(); }
            else if (tooltipInstance != null) { animationCoroutine = StartCoroutine(AnimateTooltipOut()); }

            // Invoke events
            onHide?.Invoke();
        }

        public void HideImmediate()
        {
            if (tooltipInstance != null)
            {
                Destroy(tooltipInstance.gameObject);
                tooltipInstance = null;
            }
        }

        public void SetContent(string newTitle, string newDescription, Sprite newIcon = null, GameObject newCustomContent = null)
        {
            title = newTitle;
            description = newDescription;
            icon = newIcon;
            customContent = newCustomContent;

            if (tooltipInstance == null)
                return;

            if (customContent != null) { tooltipInstance.SetupCustomContent(customContent, maxWidth); }
            else { tooltipInstance.SetupTooltip(title, description, icon, maxWidth); }
        }

        public void SetContent(GameObject newCustomContent)
        {
            SetContent(null, null, null, newCustomContent);
        }

        public bool IsVisible() => tooltipInstance != null;

#if EVO_LOCALIZATION
        void UpdateLocalization()
        {
            if (!string.IsNullOrEmpty(titleKey)) { title = localizedObject.GetString(titleKey); }
            if (!string.IsNullOrEmpty(descriptionKey)) { description = localizedObject.GetString(descriptionKey); }
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