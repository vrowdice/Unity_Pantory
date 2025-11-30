using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;

namespace Evo.UI
{
    [DisallowMultipleComponent]
    [HelpURL(Constants.HELP_URL + "layout/reorderable-list")]
    [AddComponentMenu("Evo/UI/Layout/Reorderable List")]
    [RequireComponent(typeof(RectTransform))]
    public class ReorderableList : MonoBehaviour
    {
        [EvoHeader("Settings", Constants.CUSTOM_EDITOR_ID)]
        [SerializeField] private bool instantSnap = false;
        [SerializeField] private float itemSpacing = 10; // Used if there is no layout group attached
        [SerializeField, Range(0.05f, 2)] private float animationDuration = 0.3f;
        [SerializeField] private AnimationCurve animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [EvoHeader("Drag Settings", Constants.CUSTOM_EDITOR_ID)]
        [Range(0.1f, 1)] public float dragAlpha = 1;
        [Range(1, 2)] public float dragScale = 1.2f;

        [EvoHeader("References", Constants.CUSTOM_EDITOR_ID)]
        [SerializeField] private RectTransform listContainer;
        [SerializeField] private Canvas canvas;

        [EvoHeader("Events", Constants.CUSTOM_EDITOR_ID)]
        public OrderChangedEvent onOrderChanged = new();

        // Cache
        Camera uiCamera;
        LayoutGroup layoutGroup;
        ContentSizeFitter contentSizeFitter;
        ReorderableListItem draggedItem;
        readonly List<ReorderableListItem> items = new();
        readonly List<Coroutine> activeAnimations = new();

        // Layout group alignment cache
        TextAnchor childAlignment = TextAnchor.MiddleCenter;
        RectOffset layoutPadding;

        // Helpers
        int draggedFromIndex = -1;
        int previewInsertIndex = -1;
        bool isHorizontalLayout = true;

        [System.Serializable] public class OrderChangedEvent : UnityEvent<ReorderableListItem, int, int> { }

        void Awake()
        {
            // Get canvas
            if (canvas == null) { canvas = GetComponentInParent<Canvas>(); }

            // Set default container if not assigned
            if (listContainer == null) { listContainer = GetComponent<RectTransform>(); }

            // Detect layout group and content size fitter
            layoutGroup = listContainer.GetComponent<LayoutGroup>();
            contentSizeFitter = listContainer.GetComponent<ContentSizeFitter>();

            // Cache UI camera
            uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

            if (layoutGroup != null)
            {
                isHorizontalLayout = layoutGroup is HorizontalLayoutGroup;
                itemSpacing = GetLayoutSpacing();
                childAlignment = layoutGroup.childAlignment;
                layoutPadding = layoutGroup.padding;
            }

            // Initialize padding
            layoutPadding ??= new RectOffset(0, 0, 0, 0);
        }

        void Start()
        {
            RefreshItemsList();
        }

        void StopAllActiveAnimations()
        {
            for (int i = activeAnimations.Count - 1; i >= 0; i--)
            {
                if (activeAnimations[i] != null)
                {
                    StopCoroutine(activeAnimations[i]);
                }
            }
            activeAnimations.Clear();
        }

        public void ResetState()
        {
            draggedItem = null;
            draggedFromIndex = -1;
            previewInsertIndex = -1;
        }

        public void RefreshItemsList()
        {
            items.Clear();

            // Fetch childs
            for (int i = 0; i < listContainer.childCount; i++)
            {
                Transform child = listContainer.GetChild(i);
                if (!child.gameObject.activeInHierarchy) { continue; }
                if (!child.TryGetComponent<ReorderableListItem>(out var item)) { item = child.gameObject.AddComponent<ReorderableListItem>(); }
                item.Initialize(this);
                items.Add(item);
            }

            // Update drag indices if we're currently dragging
            if (draggedItem != null && items.Contains(draggedItem))
            {
                int newDraggedIndex = items.IndexOf(draggedItem);
                draggedFromIndex = newDraggedIndex;
                previewInsertIndex = Mathf.Clamp(previewInsertIndex, 0, items.Count - 1);
            }

            StartCoroutine(DelayedLayoutRefresh());
        }

        public void RefreshLayout()
        {
            if (instantSnap)
            {
                if (draggedItem == null) { StartCoroutine(SnapToNormalPositions()); }
                else { StartCoroutine(SnapToPreviewPositions()); }
            }
            else
            {
                if (draggedItem == null) { StartCoroutine(AnimateToNormalPositions()); }
                else { StartCoroutine(AnimateToPreviewPositions()); }
            }
        }

        public void CancelDrag()
        {
            if (draggedItem == null)
                return;

            // Reset visual properties
            if (instantSnap)
            {
                draggedItem.transform.localScale = Vector3.one;
                draggedItem.canvasGroup.alpha = 1f;
            }
            else
            {
                StartCoroutine(AnimateScale(draggedItem.transform, Vector3.one, 0.2f));
                StartCoroutine(AnimateFade(draggedItem.canvasGroup, 1f, 0.2f));
            }

            // Reset state and refresh
            ResetState();
            RefreshItemsList();
            RefreshLayout();
        }

        public void OnItemBeginDrag(ReorderableListItem item)
        {
            if (item == null || !items.Contains(item))
                return;

            draggedItem = item;
            draggedFromIndex = items.IndexOf(item);
            previewInsertIndex = draggedFromIndex;

            // Store current position before any changes
            Vector3 currentPosition = item.transform.localPosition;

            // Immediately disable layout group and content size fitter to prevent fighting with manual positioning
            if (layoutGroup != null) { layoutGroup.enabled = false; }
            if (contentSizeFitter != null) { contentSizeFitter.enabled = false; }

            // Bring dragged item to front
            item.transform.SetAsLastSibling();

            // Immediately restore position to prevent flicker
            item.transform.localPosition = currentPosition;

            // Start drag animations
            if (instantSnap)
            {
                // Set scale and alpha instantly
                item.transform.localScale = Vector3.one * dragScale;
                item.canvasGroup.alpha = dragAlpha;
            }
            else
            {
                StartCoroutine(AnimateScale(item.transform, Vector3.one * dragScale, 0.1f));
                StartCoroutine(AnimateFade(item.canvasGroup, dragAlpha, 0.1f));
            }

            RefreshLayout();
        }

        public void OnItemDrag(ReorderableListItem item, Vector2 screenPosition, Vector2 offset)
        {
            if (item != draggedItem)
                return;

            // Convert screen position to local position
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(listContainer, screenPosition, uiCamera, out Vector2 localPoint))
            {
                if (isHorizontalLayout)
                {
                    localPoint += offset;

                    // Calculate Y position based on vertical alignment
                    float yPos = 0f;
                    float containerHeight = listContainer.rect.height;
                    float itemHeight = item.CachedRectTransform.rect.height;

                    switch (childAlignment)
                    {
                        case TextAnchor.UpperLeft:
                        case TextAnchor.UpperCenter:
                        case TextAnchor.UpperRight:
                            yPos = containerHeight * 0.5f - layoutPadding.top - itemHeight * 0.5f;
                            break;

                        case TextAnchor.MiddleLeft:
                        case TextAnchor.MiddleCenter:
                        case TextAnchor.MiddleRight:
                            yPos = 0f; // Center
                            break;

                        case TextAnchor.LowerLeft:
                        case TextAnchor.LowerCenter:
                        case TextAnchor.LowerRight:
                            yPos = -containerHeight * 0.5f + layoutPadding.bottom + itemHeight * 0.5f;
                            break;
                    }

                    // Calculate bounds for horizontal dragging
                    float itemWidth = item.CachedRectTransform.rect.width;
                    float containerWidth = listContainer.rect.width;

                    // Calculate min and max positions based on all items
                    float minPos = -containerWidth * 0.5f + layoutPadding.left + itemWidth * 0.5f;
                    float maxPos = containerWidth * 0.5f - layoutPadding.right - itemWidth * 0.5f;

                    // Clamp the X position to stay within bounds
                    float clampedX = Mathf.Clamp(localPoint.x, minPos, maxPos);

                    item.transform.localPosition = new Vector3(clampedX, yPos, 0);
                }
                else
                {
                    // Calculate X position based on horizontal alignment
                    float xPos = 0f;
                    float containerWidth = listContainer.rect.width;
                    float itemWidth = item.CachedRectTransform.rect.width;

                    switch (childAlignment)
                    {
                        case TextAnchor.UpperLeft:
                        case TextAnchor.MiddleLeft:
                        case TextAnchor.LowerLeft:
                            xPos = -containerWidth * 0.5f + layoutPadding.left + itemWidth * 0.5f;
                            break;

                        case TextAnchor.UpperCenter:
                        case TextAnchor.MiddleCenter:
                        case TextAnchor.LowerCenter:
                            xPos = 0f; // Center
                            break;

                        case TextAnchor.UpperRight:
                        case TextAnchor.MiddleRight:
                        case TextAnchor.LowerRight:
                            xPos = containerWidth * 0.5f - layoutPadding.right - itemWidth * 0.5f;
                            break;
                    }

                    // Calculate bounds for vertical dragging
                    float itemHeight = item.CachedRectTransform.rect.height;
                    float containerHeight = listContainer.rect.height;

                    // Calculate min and max positions based on all items
                    float maxPos = containerHeight * 0.5f - layoutPadding.top - itemHeight * 0.5f;
                    float minPos = -containerHeight * 0.5f + layoutPadding.bottom + itemHeight * 0.5f;

                    // Clamp the Y position to stay within bounds
                    float clampedY = Mathf.Clamp(localPoint.y, minPos, maxPos);

                    item.transform.localPosition = new Vector3(xPos, clampedY, 0);
                }
            }

            // Check for insertion point
            int newInsertIndex = GetInsertionIndex();
            if (newInsertIndex != previewInsertIndex)
            {
                previewInsertIndex = newInsertIndex;
                RefreshLayout();
            }
        }

        public void OnItemEndDrag(ReorderableListItem item)
        {
            if (item != draggedItem)
                return;

            // Store original index before changes
            int originalIndex = draggedFromIndex;

            // Clamp insert index to valid range
            int finalInsertIndex = Mathf.Clamp(previewInsertIndex, 0, items.Count - 1);

            // Remove from current position
            items.Remove(item);

            // Insert at final position
            if (finalInsertIndex >= 0 && finalInsertIndex <= items.Count)
            {
                items.Insert(finalInsertIndex, item);
                item.transform.SetSiblingIndex(finalInsertIndex);
            }

            // Reset visual properties (respect instant snap setting)
            if (instantSnap)
            {
                // Reset scale and alpha instantly
                item.transform.localScale = Vector3.one;
                item.canvasGroup.alpha = 1f;
            }
            else
            {
                StartCoroutine(AnimateScale(item.transform, Vector3.one, 0.2f));
                StartCoroutine(AnimateFade(item.canvasGroup, 1f, 0.2f));
            }

            // Fire event only if order actually changed
            if (originalIndex != finalInsertIndex) { onOrderChanged?.Invoke(item, originalIndex, finalInsertIndex); }

            // Reset drag state
            ResetState();
            RefreshLayout();
        }

        float CalculateStartPosition()
        {
            float totalSize = 0f;

            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] == null)
                    continue;

                RectTransform itemRect = items[i].CachedRectTransform;
                totalSize += GetItemSize(itemRect);
                if (i < items.Count - 1) { totalSize += itemSpacing; }
            }

            // Get container size
            float containerSize = isHorizontalLayout ? listContainer.rect.width : listContainer.rect.height;

            // Calculate start position based on alignment
            float startPos = 0f;

            if (isHorizontalLayout)
            {
                // Horizontal alignment
                switch (childAlignment)
                {
                    case TextAnchor.UpperLeft:
                    case TextAnchor.MiddleLeft:
                    case TextAnchor.LowerLeft:
                        startPos = -containerSize * 0.5f + layoutPadding.left;
                        break;

                    case TextAnchor.UpperCenter:
                    case TextAnchor.MiddleCenter:
                    case TextAnchor.LowerCenter:
                        startPos = -totalSize * 0.5f;
                        break;

                    case TextAnchor.UpperRight:
                    case TextAnchor.MiddleRight:
                    case TextAnchor.LowerRight:
                        startPos = containerSize * 0.5f - layoutPadding.right - totalSize;
                        break;
                }
            }
            else
            {
                // Vertical alignment
                switch (childAlignment)
                {
                    case TextAnchor.UpperLeft:
                    case TextAnchor.UpperCenter:
                    case TextAnchor.UpperRight:
                        startPos = containerSize * 0.5f - layoutPadding.top;
                        break;

                    case TextAnchor.MiddleLeft:
                    case TextAnchor.MiddleCenter:
                    case TextAnchor.MiddleRight:
                        startPos = totalSize * 0.5f;
                        break;

                    case TextAnchor.LowerLeft:
                    case TextAnchor.LowerCenter:
                    case TextAnchor.LowerRight:
                        startPos = -containerSize * 0.5f + layoutPadding.bottom + totalSize;
                        break;
                }
            }

            return startPos;
        }

        float GetItemSize(RectTransform itemRect)
        {
            return isHorizontalLayout ? itemRect.rect.width : itemRect.rect.height;
        }

        float GetLayoutSpacing()
        {
            if (layoutGroup == null) { return 10f; }
            else if (layoutGroup is HorizontalLayoutGroup hlg) { return hlg.spacing; }
            else if (layoutGroup is VerticalLayoutGroup vlg) { return vlg.spacing; }

            return 10f;
        }

        int GetInsertionIndex()
        {
            if (items.Count == 0 || draggedItem == null)
                return 0;

            // Get the dragged item's position and size
            float draggedItemSize = GetItemSize(draggedItem.CachedRectTransform);
            float draggedItemCenter = isHorizontalLayout ? draggedItem.transform.localPosition.x : draggedItem.transform.localPosition.y;

            float startPos = CalculateStartPosition();
            float currentPos = startPos;

            // Calculate center points of each item
            for (int i = 0; i < items.Count; i++)
            {
                RectTransform itemRect = items[i].CachedRectTransform;
                float itemSize = GetItemSize(itemRect);

                // Skip the dragged item in position calculations
                if (items[i] == draggedItem)
                {
                    if (isHorizontalLayout) { currentPos += itemSize + itemSpacing; }
                    else { currentPos -= itemSize + itemSpacing; }
                    continue;
                }

                // Calculate this item's center position
                float itemCenter = currentPos + (isHorizontalLayout ? itemSize * 0.5f : -itemSize * 0.5f);

                // Determine which edge to check based on drag direction
                bool shouldInsertHere;
                if (isHorizontalLayout)
                {
                    if (draggedFromIndex < i)
                    {
                        // Dragging left to right: use right edge of dragged item
                        float draggedRightEdge = draggedItemCenter + (draggedItemSize * 0.5f);
                        shouldInsertHere = draggedRightEdge < itemCenter;
                    }
                    else
                    {
                        // Dragging right to left: use left edge of dragged item
                        float draggedLeftEdge = draggedItemCenter - (draggedItemSize * 0.5f);
                        shouldInsertHere = draggedLeftEdge < itemCenter;
                    }
                }
                else
                {
                    if (draggedFromIndex < i)
                    {
                        // Dragging top to bottom: use bottom edge of dragged item
                        float draggedBottomEdge = draggedItemCenter - (draggedItemSize * 0.5f);
                        shouldInsertHere = draggedBottomEdge > itemCenter;
                    }
                    else
                    {
                        // Dragging bottom to top: use top edge of dragged item
                        float draggedTopEdge = draggedItemCenter + (draggedItemSize * 0.5f);
                        shouldInsertHere = draggedTopEdge > itemCenter;
                    }
                }

                if (shouldInsertHere)
                {
                    int insertIndex = i;
                    if (draggedFromIndex < i && draggedFromIndex >= 0) { insertIndex = i - 1; }
                    return Mathf.Clamp(insertIndex, 0, items.Count);
                }

                if (isHorizontalLayout) { currentPos += itemSize + itemSpacing; }
                else { currentPos -= itemSize + itemSpacing; }
            }

            // If we're past all items, insert at the end
            int finalIndex = items.Count;
            if (draggedFromIndex >= 0) { finalIndex = items.Count - 1; }

            return Mathf.Clamp(finalIndex, 0, items.Count);
        }

        Vector3 GetItemPosition(float currentPos, RectTransform itemRect)
        {
            float yPos = 0f;
            float xPos = 0f;

            if (isHorizontalLayout)
            {
                float itemWidth = itemRect.rect.width;
                xPos = currentPos + itemWidth * 0.5f;

                // Calculate Y position based on vertical alignment
                float containerHeight = listContainer.rect.height;
                float itemHeight = itemRect.rect.height;

                switch (childAlignment)
                {
                    case TextAnchor.UpperLeft:
                    case TextAnchor.UpperCenter:
                    case TextAnchor.UpperRight:
                        yPos = containerHeight * 0.5f - layoutPadding.top - itemHeight * 0.5f;
                        break;

                    case TextAnchor.MiddleLeft:
                    case TextAnchor.MiddleCenter:
                    case TextAnchor.MiddleRight:
                        yPos = 0f; // Center
                        break;

                    case TextAnchor.LowerLeft:
                    case TextAnchor.LowerCenter:
                    case TextAnchor.LowerRight:
                        yPos = -containerHeight * 0.5f + layoutPadding.bottom + itemHeight * 0.5f;
                        break;
                }

                return new Vector3(xPos, yPos, 0);
            }
            else
            {
                float itemHeight = itemRect.rect.height;
                yPos = currentPos - itemHeight * 0.5f;

                // Calculate X position based on horizontal alignment
                float containerWidth = listContainer.rect.width;
                float itemWidth = itemRect.rect.width;

                switch (childAlignment)
                {
                    case TextAnchor.UpperLeft:
                    case TextAnchor.MiddleLeft:
                    case TextAnchor.LowerLeft:
                        xPos = -containerWidth * 0.5f + layoutPadding.left + itemWidth * 0.5f;
                        break;

                    case TextAnchor.UpperCenter:
                    case TextAnchor.MiddleCenter:
                    case TextAnchor.LowerCenter:
                        xPos = 0f; // Center
                        break;

                    case TextAnchor.UpperRight:
                    case TextAnchor.MiddleRight:
                    case TextAnchor.LowerRight:
                        xPos = containerWidth * 0.5f - layoutPadding.right - itemWidth * 0.5f;
                        break;
                }

                return new Vector3(xPos, yPos, 0);
            }
        }

        IEnumerator DelayedLayoutRefresh()
        {
            yield return new WaitForEndOfFrame();
            RefreshLayout();
        }

        IEnumerator AnimateToNormalPositions()
        {
            if (layoutGroup != null) { layoutGroup.enabled = false; }
            if (contentSizeFitter != null) { contentSizeFitter.enabled = false; }

            StopAllActiveAnimations();

            if (items.Count == 0)
            {
                if (layoutGroup != null) { layoutGroup.enabled = true; }
                if (contentSizeFitter != null) { contentSizeFitter.enabled = true; }
                yield break;
            }

            // Calculate normal positions
            float currentPos = CalculateStartPosition();

            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] == null) continue;

                RectTransform itemRect = items[i].CachedRectTransform;
                Vector3 targetPos = GetItemPosition(currentPos, itemRect);

                Coroutine anim = StartCoroutine(SmoothMove(items[i].transform, targetPos, animationDuration));
                activeAnimations.Add(anim);

                if (isHorizontalLayout) { currentPos += GetItemSize(itemRect) + itemSpacing; }
                else { currentPos -= GetItemSize(itemRect) + itemSpacing; }
            }

            yield return new WaitForSeconds(animationDuration);

            if (layoutGroup != null) { layoutGroup.enabled = true; }
            if (contentSizeFitter != null) { contentSizeFitter.enabled = true; }
        }

        IEnumerator AnimateToPreviewPositions()
        {
            if (layoutGroup != null) { layoutGroup.enabled = false; }
            if (contentSizeFitter != null) { contentSizeFitter.enabled = false; }

            StopAllActiveAnimations();

            if (items.Count == 0)
            {
                if (layoutGroup != null) { layoutGroup.enabled = true; }
                if (contentSizeFitter != null) { contentSizeFitter.enabled = true; }
                yield break;
            }

            // Create preview list with dragged item inserted at preview position
            List<ReorderableListItem> previewItems = new(items);

            // Remove dragged item from its current position
            if (draggedItem != null && previewItems.Contains(draggedItem)) { previewItems.Remove(draggedItem); }

            // Insert at preview position
            if (draggedItem != null && previewInsertIndex >= 0 && previewInsertIndex <= previewItems.Count) { previewItems.Insert(previewInsertIndex, draggedItem); }

            float currentPos = CalculateStartPosition();

            // Animate all items except the dragged one to their preview positions
            for (int i = 0; i < previewItems.Count; i++)
            {
                if (previewItems[i] == null) { continue; }
                if (previewItems[i] == draggedItem)
                {
                    // Skip dragged item but reserve space
                    RectTransform draggedRect = draggedItem.CachedRectTransform;
                    if (isHorizontalLayout) { currentPos += GetItemSize(draggedRect) + itemSpacing; }
                    else { currentPos -= GetItemSize(draggedRect) + itemSpacing; }
                    continue;
                }

                RectTransform itemRect = previewItems[i].CachedRectTransform;
                Vector3 targetPos = GetItemPosition(currentPos, itemRect);

                Coroutine anim = StartCoroutine(SmoothMove(previewItems[i].transform, targetPos, animationDuration * 0.5f));
                activeAnimations.Add(anim);

                if (isHorizontalLayout) { currentPos += GetItemSize(itemRect) + itemSpacing; }
                else { currentPos -= GetItemSize(itemRect) + itemSpacing; }
            }

            yield return new WaitForSeconds(animationDuration * 0.5f);
        }

        IEnumerator SnapToNormalPositions()
        {
            if (layoutGroup != null) { layoutGroup.enabled = false; }
            if (contentSizeFitter != null) { contentSizeFitter.enabled = false; }

            StopAllActiveAnimations();

            if (items.Count == 0)
            {
                if (layoutGroup != null) { layoutGroup.enabled = true; }
                if (contentSizeFitter != null) { contentSizeFitter.enabled = true; }
                yield break;
            }

            // Calculate and set positions instantly
            float currentPos = CalculateStartPosition();

            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] == null)
                    continue;

                RectTransform itemRect = items[i].CachedRectTransform;
                Vector3 targetPos = GetItemPosition(currentPos, itemRect);

                // Set position instantly
                items[i].transform.localPosition = targetPos;

                if (isHorizontalLayout) { currentPos += GetItemSize(itemRect) + itemSpacing; }
                else { currentPos -= GetItemSize(itemRect) + itemSpacing; }
            }

            // Wait one frame then re-enable layout components
            yield return null;

            if (layoutGroup != null) { layoutGroup.enabled = true; }
            if (contentSizeFitter != null) { contentSizeFitter.enabled = true; }
        }

        IEnumerator SnapToPreviewPositions()
        {
            if (layoutGroup != null) { layoutGroup.enabled = false; }
            if (contentSizeFitter != null) { contentSizeFitter.enabled = false; }

            StopAllActiveAnimations();

            if (items.Count == 0)
            {
                if (layoutGroup != null) { layoutGroup.enabled = true; }
                if (contentSizeFitter != null) { contentSizeFitter.enabled = true; }
                yield break;
            }

            // Create preview list with dragged item inserted at preview position
            List<ReorderableListItem> previewItems = new(items);

            // Remove dragged item from its current position
            if (draggedItem != null && previewItems.Contains(draggedItem)) { previewItems.Remove(draggedItem); }

            // Insert at preview position
            if (draggedItem != null && previewInsertIndex >= 0 && previewInsertIndex <= previewItems.Count) { previewItems.Insert(previewInsertIndex, draggedItem); }

            float currentPos = CalculateStartPosition();

            // Set all positions instantly except the dragged item
            for (int i = 0; i < previewItems.Count; i++)
            {
                if (previewItems[i] == null) { continue; }
                if (previewItems[i] == draggedItem)
                {
                    // Skip dragged item but reserve space
                    RectTransform draggedRect = draggedItem.CachedRectTransform;
                    if (isHorizontalLayout) { currentPos += GetItemSize(draggedRect) + itemSpacing; }
                    else { currentPos -= GetItemSize(draggedRect) + itemSpacing; }
                    continue;
                }

                RectTransform itemRect = previewItems[i].CachedRectTransform;
                Vector3 targetPos = GetItemPosition(currentPos, itemRect);

                // Set position instantly
                previewItems[i].transform.localPosition = targetPos;

                if (isHorizontalLayout) { currentPos += GetItemSize(itemRect) + itemSpacing; }
                else { currentPos -= GetItemSize(itemRect) + itemSpacing; }
            }

            // Wait one frame
            yield return null;
        }

        IEnumerator SmoothMove(Transform target, Vector3 destination, float duration)
        {
            if (target == null)
                yield break;

            Vector3 startPos = target.localPosition;
            float elapsed = 0f;
            float invDuration = 1f / duration; // Cache division

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed * invDuration;
                t = animationCurve.Evaluate(t);
                target.localPosition = Vector3.Lerp(startPos, destination, t);
                yield return null;
            }

            target.localPosition = destination;
        }

        IEnumerator AnimateScale(Transform target, Vector3 targetScale, float duration)
        {
            if (target == null)
                yield break;

            Vector3 startScale = target.localScale;
            Vector3 endScale = targetScale;
            float elapsed = 0f;
            float invDuration = 1f / duration;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed * invDuration;
                t = animationCurve.Evaluate(t);
                target.localScale = Vector3.Lerp(startScale, endScale, t);
                yield return null;
            }

            target.localScale = endScale;
        }

        IEnumerator AnimateFade(CanvasGroup target, float targetAlpha, float duration)
        {
            if (target == null)
                yield break;

            float startAlpha = target.alpha;
            float elapsed = 0f;
            float invDuration = 1f / duration;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed * invDuration;
                t = animationCurve.Evaluate(t);
                target.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                yield return null;
            }

            target.alpha = targetAlpha;
        }

        public bool IsDragging(ReorderableListItem item) => draggedItem == item;
        public List<ReorderableListItem> Items() => items;
    }

    [RequireComponent(typeof(CanvasGroup))]
    [RequireComponent(typeof(RectTransform))]
    public class ReorderableListItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        // References
        public CanvasGroup canvasGroup;
        public RectTransform CachedRectTransform { get; private set; }

        // Helpers
        bool isDragging;
        ReorderableList targetList;
        Vector2 dragOffset;

        void OnEnable()
        {
            if (targetList != null && !targetList.Items().Contains(this))
            {
                targetList.RefreshItemsList();
            }
        }

        void OnDisable()
        {
            if (targetList != null && targetList.gameObject.activeInHierarchy)
            {
                if (targetList.IsDragging(this)) { targetList.CancelDrag(); }
                else
                {
                    transform.SetAsLastSibling();
                    targetList.RefreshItemsList();
                    targetList.RefreshLayout();
                }
            }
        }

        public void Initialize(ReorderableList parentList)
        {
            targetList = parentList;
            CachedRectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (targetList == null)
                return;

            isDragging = true;

            // Calculate offset between pointer and item position
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                transform.parent as RectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 localPointerPos);

            dragOffset = (Vector2)transform.localPosition - localPointerPos;

            targetList.OnItemBeginDrag(this);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!isDragging || targetList == null)
                return;

            targetList.OnItemDrag(this, eventData.position, dragOffset);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!isDragging || targetList == null)
                return;

            isDragging = false;
            targetList.OnItemEndDrag(this);
        }
    }
}