using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Evo.UI
{
    [HelpURL(Constants.HELP_URL + "layout/pages")]
    [AddComponentMenu("Evo/UI/Layout/Pages")]
    public class Pages : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [EvoHeader("References")]
        [Tooltip("Parent container that holds all pages.")]
        public RectTransform container;

        [EvoHeader("Pages")]
        [SerializeField] private int defaultPageIndex = 0;
        public List<Page> pages = new();

        [EvoHeader("Settings")]
        public bool disableInvisiblePages = true;
        public bool useUnscaledTime = false;
        public bool interruptTransitions = true;

        [EvoHeader("Swipe Settings")]
        [Range(0.1f, 0.9f)] public float swipeThreshold = 0.3f;
        [Range(0.1f, 1f)] public float elasticResistance = 0.3f;
        public float velocityThreshold = 500;
        public SwipeDirection swipeDirection = SwipeDirection.Horizontal;

        [EvoHeader("Animation Settings")]
        public AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [Range(0, 2)] public float transitionDuration = 0.25f;
        public float pageSpacing = 20;

        [EvoHeader("Events")]
        public UnityEvent<int> onPageChanged = new();

        public enum SwipeDirection
        {
            Horizontal,
            Vertical
        }

        [System.Serializable]
        public class Page
        {
            public string pageID = "";
            public RectTransform pageObject;
            public Button pageButton;
        }

        // Public properties
        public int CurrentPageIndex { get; private set; } = -1;
        public bool IsTransitioning { get; private set; } = false;

        // Helpers
        bool isDragging;
        float currentOffset;
        float targetOffset;
        float dragVelocity;
        float dragStartTime;
        Vector2 dragStartPos;
        Canvas canvas;
        Coroutine transitionCoroutine;

        void Start()
        {
            if (container == null)
            {
                Debug.LogError("<b>[Pages]</b> Pages Container is not assigned!");
                return;
            }

            canvas = GetComponentInParent<Canvas>();
            PositionPages();

            // Set initial page
            if (pages.Count > 0) { OpenPage(defaultPageIndex, false); }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            // Allow interrupting transitions
            if (IsTransitioning && !interruptTransitions)
                return;

            isDragging = true;
            dragStartPos = eventData.position;
            dragStartTime = Time.unscaledTime;
            dragVelocity = 0f;

            // Stop any ongoing transition
            if (transitionCoroutine != null)
            {
                StopCoroutine(transitionCoroutine);
                IsTransitioning = false;
            }

            // Activate all pages for smooth dragging
            if (disableInvisiblePages)
            {
                for (int i = 0; i < pages.Count; i++)
                {
                    if (pages[i].pageObject != null)
                    {
                        pages[i].pageObject.gameObject.SetActive(true);
                    }
                }
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!isDragging)
                return;

            float containerSize = swipeDirection == SwipeDirection.Horizontal ? container.rect.width : container.rect.height;
            float scaleFactor = canvas != null ? canvas.scaleFactor : 1f;

            float currentPos = swipeDirection == SwipeDirection.Horizontal ? eventData.position.x : eventData.position.y;
            float startPos = swipeDirection == SwipeDirection.Horizontal ? dragStartPos.x : dragStartPos.y;
            float delta = (currentPos - startPos) / scaleFactor;

            // Invert delta for vertical scrolling to match expected behavior
            if (swipeDirection == SwipeDirection.Vertical) { delta = -delta; }

            // Calculate velocity (smoothed over time)
            float timeDelta = Time.unscaledTime - dragStartTime;
            if (timeDelta > 0) { dragVelocity = delta / timeDelta; }

            // Limit drag to one page in each direction
            float pageSize = containerSize + pageSpacing;
            float maxDrag = pageSize;
            delta = Mathf.Clamp(delta, -maxDrag, maxDrag);

            // Apply elastic resistance at bounds
            float targetDragOffset = targetOffset + delta;
            float minOffset = -(pages.Count - 1) * pageSize;
            float maxOffset = 0;

            if (targetDragOffset > maxOffset)
            {
                float overflow = targetDragOffset - maxOffset;
                delta -= overflow * (1f - elasticResistance);
            }
            else if (targetDragOffset < minOffset)
            {
                float overflow = minOffset - targetDragOffset;
                delta += overflow * (1f - elasticResistance);
            }

            currentOffset = targetOffset + delta;
            ApplyOffset(currentOffset);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!isDragging)
                return;

            isDragging = false;
            float containerSize = swipeDirection == SwipeDirection.Horizontal ? container.rect.width : container.rect.height;
            float scaleFactor = canvas != null ? canvas.scaleFactor : 1f;

            float currentPos = swipeDirection == SwipeDirection.Horizontal ? eventData.position.x : eventData.position.y;
            float startPos = swipeDirection == SwipeDirection.Horizontal ? dragStartPos.x : dragStartPos.y;
            float dragDelta = (currentPos - startPos) / scaleFactor;

            // Invert delta for vertical scrolling to match expected behavior
            if (swipeDirection == SwipeDirection.Vertical)
            {
                dragDelta = -dragDelta;
            }

            float dragDistance = Mathf.Abs(dragDelta);
            float dragPercent = dragDistance / containerSize;
            int targetPage = CurrentPageIndex;
            bool isQuickSwipe = Mathf.Abs(dragVelocity) > velocityThreshold;

            if (isQuickSwipe)
            {
                // Quick swipe - move one page in swipe direction
                if (dragVelocity > 0 && CurrentPageIndex > 0) { targetPage = CurrentPageIndex - 1; }
                else if (dragVelocity < 0 && CurrentPageIndex < pages.Count - 1) { targetPage = CurrentPageIndex + 1; }
            }
            else if (dragPercent >= swipeThreshold)
            {
                // Slow drag past threshold - move one page in drag direction
                if (dragDelta > 0 && CurrentPageIndex > 0) { targetPage = CurrentPageIndex - 1; }
                else if (dragDelta < 0 && CurrentPageIndex < pages.Count - 1) { targetPage = CurrentPageIndex + 1; }
            }

            // Clamp target page
            targetPage = Mathf.Clamp(targetPage, 0, pages.Count - 1);
            OpenPage(targetPage, true);
        }

        void UpdateButtonStates(int selectedTabIndex)
        {
            for (int i = 0; i < pages.Count; i++)
            {
                var tab = pages[i];
                if (tab.pageButton != null) 
                {
                    tab.pageButton.SetState(i == selectedTabIndex 
                        ? InteractionState.Selected 
                        : InteractionState.Normal); 
                }
            }
        }

        void ApplyOffset(float offset)
        {
            if (container == null)
                return;

            // Move each page instead of the container
            float containerSize = swipeDirection == SwipeDirection.Horizontal ? container.rect.width : container.rect.height;
            for (int i = 0; i < pages.Count; i++)
            {
                if (pages[i].pageObject == null)
                    continue;

                float basePosition = i * (containerSize + pageSpacing);

                if (swipeDirection == SwipeDirection.Horizontal)
                {
                    pages[i].pageObject.anchoredPosition = new Vector2(basePosition + offset, 0);
                }
                else
                {
                    pages[i].pageObject.anchoredPosition = new Vector2(0, -(basePosition + offset));
                }
            }
        }

        void UpdatePageVisibility()
        {
            if (!disableInvisiblePages) { return; }
            for (int i = 0; i < pages.Count; i++)
            {
                if (pages[i].pageObject == null)
                    continue;

                bool shouldBeActive = i == CurrentPageIndex;

                // Keep adjacent pages active for smooth transitions
                if (Mathf.Abs(i - CurrentPageIndex) <= 1) { shouldBeActive = true; }
                pages[i].pageObject.gameObject.SetActive(shouldBeActive);
            }
        }

        IEnumerator AnimateToPage(float targetPos)
        {
            IsTransitioning = true;
            float startOffset = currentOffset;
            float distance = Mathf.Abs(targetPos - startOffset);
            float containerSize = swipeDirection == SwipeDirection.Horizontal ? container.rect.width : container.rect.height;

            // Adjust duration based on distance (shorter transitions for nearby pages)
            float dynamicDuration = transitionDuration * Mathf.Clamp01(distance / containerSize);
            dynamicDuration = Mathf.Max(dynamicDuration, 0.1f); // Minimum duration

            float elapsed = 0f;

            while (elapsed < dynamicDuration)
            {
                // Check if dragging started (interruption)
                if (isDragging && interruptTransitions)
                {
                    IsTransitioning = false;
                    yield break;
                }

                elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / dynamicDuration);
                float curvedT = transitionCurve.Evaluate(t);

                currentOffset = Mathf.Lerp(startOffset, targetPos, curvedT);
                ApplyOffset(currentOffset);

                yield return null;
            }

            currentOffset = targetPos;
            ApplyOffset(currentOffset);
            UpdatePageVisibility();
            IsTransitioning = false;
        }

        public void PositionPages()
        {
            if (container == null || pages.Count == 0)
                return;

            float containerSize = swipeDirection == SwipeDirection.Horizontal ? container.rect.width : container.rect.height;

            for (int i = 0; i < pages.Count; i++)
            {
                if (pages[i].pageObject == null)
                    continue;

                RectTransform pageRect = pages[i].pageObject;

                // Set anchors to stretch
                pageRect.anchorMin = new Vector2(0, 0);
                pageRect.anchorMax = new Vector2(1, 1);
                pageRect.sizeDelta = Vector2.zero;

                // Position based on index and direction
                float offset = i * (containerSize + pageSpacing);
                if (swipeDirection == SwipeDirection.Horizontal) { pageRect.anchoredPosition = new Vector2(offset, 0); }
                else { pageRect.anchoredPosition = new Vector2(0, -offset); }

                // Set active state
                if (disableInvisiblePages && i != CurrentPageIndex) { pageRect.gameObject.SetActive(false); }
                else { pages[i].pageObject.gameObject.SetActive(true); }

                // Setup button listeners
                if (pages[i].pageButton != null)
                {
                    int index = i; // Capture for closure
                    pages[index].pageButton.onClick.RemoveAllListeners();
                    pages[index].pageButton.onClick.AddListener(() => OpenPage(index));
                }
            }
        }

        public void OpenPage(int index, bool animate = true)
        {
            if (index < 0 || index >= pages.Count)
                return;

            // Allow opening pages during transitions now
            bool pageChanged = CurrentPageIndex != index;
            CurrentPageIndex = index;

            float containerSize = swipeDirection == SwipeDirection.Horizontal ? container.rect.width : container.rect.height;
            targetOffset = -index * (containerSize + pageSpacing);

            // Enable target page and adjacent pages
            if (pages[index].pageObject != null) { pages[index].pageObject.gameObject.SetActive(true); }
            if (index > 0 && pages[index - 1].pageObject != null) { pages[index - 1].pageObject.gameObject.SetActive(true); }
            if (index < pages.Count - 1 && pages[index + 1].pageObject != null) { pages[index + 1].pageObject.gameObject.SetActive(true); }

            if (animate && Application.isPlaying)
            {
                if (transitionCoroutine != null) { StopCoroutine(transitionCoroutine); }
                transitionCoroutine = StartCoroutine(AnimateToPage(targetOffset));
            }
            else
            {
                currentOffset = targetOffset;
                ApplyOffset(currentOffset);
                UpdatePageVisibility();
            }

            // Check for event
            if (pageChanged) { onPageChanged?.Invoke(CurrentPageIndex); }

            // Update buttons
            UpdateButtonStates(CurrentPageIndex);
        }

        public void OpenNextPage()
        {
            if (CurrentPageIndex < pages.Count - 1)
            {
                OpenPage(CurrentPageIndex + 1);
            }
        }

        public void OpenPreviousPage()
        {
            if (CurrentPageIndex > 0)
            {
                OpenPage(CurrentPageIndex - 1);
            }
        }

#if UNITY_EDITOR
        [HideInInspector] public bool objectFoldout = true;
        [HideInInspector] public bool settingsFoldout = false;
        [HideInInspector] public bool eventsFoldout = false;
        int lastDefaultTabIndex = -1;
        bool pendingEditorUpdate = false;

        void OnValidate()
        {
            if (Application.isPlaying)
                return;

            // Clamp default tab index
            if (pages.Count > 0) { defaultPageIndex = Mathf.Clamp(defaultPageIndex, 0, pages.Count - 1); }

            // Update editor preview if index changed - defer to avoid SendMessage warnings
            if (lastDefaultTabIndex != defaultPageIndex)
            {
                if (!pendingEditorUpdate)
                {
                    pendingEditorUpdate = true;
                    UnityEditor.EditorApplication.delayCall += () =>
                    {
                        if (this != null)
                        {
                            UpdateEditorPreview();
                            pendingEditorUpdate = false;
                        }
                    };
                }
                lastDefaultTabIndex = defaultPageIndex;
            }
        }

        void UpdateEditorPreview()
        {
            if (pages == null || pages.Count == 0) { return; }
            for (int i = 0; i < pages.Count; i++)
            {
                // Check tab object
                var tab = pages[i];
                if (tab.pageObject == null) { continue; }

                // Tab visibility
                if (i == defaultPageIndex) { tab.pageObject.gameObject.SetActive(true); }
                else if (tab.pageObject.gameObject.activeInHierarchy) { tab.pageObject.gameObject.SetActive(false); }

                // Button state 
                if (tab.pageButton != null) { tab.pageButton.SetState(i == defaultPageIndex ? InteractionState.Selected : InteractionState.Normal); }
            }
        }
#endif
    }
}