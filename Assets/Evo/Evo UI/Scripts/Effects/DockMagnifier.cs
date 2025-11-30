using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Evo.UI
{
    [DisallowMultipleComponent]
    [HelpURL(Constants.HELP_URL + "effects/dock-magnifier")]
    [AddComponentMenu("Evo/UI/Effects/Dock Magnifier")]
    public class DockMagnifier : UIBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
    {
        [Header("Magnification Settings")]
        [Min(1f)] public float maxScale = 1.5f;        // peak size multiplier
        [Min(0.01f)] public float baseScale = 1f;      // 1 = original size
        [Min(1f)] public float influenceRadius = 120f; // pixels in parent local space
        [Min(0.01f)] public float axisBias = 2f;       // widens influence along the main axis
        [Range(0.001f, 1f)] public float smoothTime = 0.05f;

        [Header("Settings")]
        [SerializeField] private Orientation orientation = Orientation.Horizontal;
        [Tooltip("While hovering, refresh pointer from current mouse position every frame. Fixes ScrollRect wheel/touchpad scrolling issues.")]
        [SerializeField] private bool alwaysUpdateOnHover = true;
        [Tooltip("Only refresh pointer-from-mouse when the cursor is actually over this RectTransform.")]
        [SerializeField] private bool requirePointerInsideForRefresh = true;

        // Cache
        Canvas canvas;
        Camera eventCamera;
        RectTransform parentRect;
        ScrollRect parentScrollRect;
        readonly List<ChildState> states = new();
        readonly List<RectTransform> targets = new();

        // Helpers
        float sigma;
        bool isHovering;
        bool requestedRebuild;
        Vector2 lastPointerLocal;

        struct ChildState
        {
            public RectTransform rt;
            public Vector2 baseSize; // captured from rt.rect.size at Awake
            public Vector2 vel;      // for SmoothDamp per axis (w,h)
        }

        public enum Orientation
        {
            Horizontal,
            Vertical,
            Free
        }

        protected override void Awake()
        {
            base.Awake();

            parentRect = transform as RectTransform;
            canvas = GetComponentInParent<Canvas>();
            eventCamera = canvas && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;

            parentScrollRect = GetComponentInParent<ScrollRect>();
            if (parentScrollRect != null) { parentScrollRect.onValueChanged.AddListener(OnScrollRectValueChanged); }

            // Gather immediate children
            targets.Clear();
            for (int i = 0; i < parentRect.childCount; i++)
            {
                if (parentRect.GetChild(i) is RectTransform rt && rt != parentRect)
                {
                    targets.Add(rt);
                }
            }

            // Initialize states and sizes
            states.Clear();
            foreach (var t in targets)
            {
                if (!t) { continue; }
                var size = t.rect.size;
                states.Add(new ChildState { rt = t, baseSize = size, vel = Vector2.zero });
                SetSize(t, size.x * baseScale, size.y * baseScale);
            }

            sigma = Mathf.Max(1f, influenceRadius * 0.5f);
            baseScale = Mathf.Max(0.01f, baseScale);
            maxScale = Mathf.Max(baseScale, maxScale);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (parentScrollRect != null) { parentScrollRect.onValueChanged.RemoveListener(OnScrollRectValueChanged); }
        }

        void Update()
        {
            if (states.Count == 0) { return; }
            if (isHovering && alwaysUpdateOnHover) { TryRefreshPointerFromMouse(); }
            if (!isHovering)
            {
                // Relax back to base size
                for (int i = 0; i < states.Count; i++)
                {
                    var st = states[i];
                    if (!st.rt) { continue; }

                    var cur = st.rt.rect.size;
                    var targetW = st.baseSize.x * baseScale;
                    var targetH = st.baseSize.y * baseScale;

                    float nextW = Mathf.SmoothDamp(cur.x, targetW, ref st.vel.x, smoothTime);
                    float nextH = Mathf.SmoothDamp(cur.y, targetH, ref st.vel.y, smoothTime);

                    if (!Mathf.Approximately(nextW, cur.x) || !Mathf.Approximately(nextH, cur.y))
                    {
                        SetSize(st.rt, nextW, nextH);
                        requestedRebuild = true;
                    }

                    states[i] = st;
                }

                if (requestedRebuild)
                {
                    requestedRebuild = false;
                    LayoutRebuilder.MarkLayoutForRebuild(parentRect);
                }
                return;
            }

            // Hovering: compute target size for each child based on pointer distance
            for (int i = 0; i < states.Count; i++)
            {
                var st = states[i];
                if (!st.rt) continue;

                // child center in parent local space
                Vector2 childCenterLocal = WorldToParentLocal(st.rt, st.rt.rect.center);

                Vector2 delta = childCenterLocal - lastPointerLocal;
                float d;
                switch (orientation)
                {
                    case Orientation.Horizontal:
                        delta.x /= Mathf.Max(0.01f, axisBias);
                        d = delta.magnitude;
                        break;
                    case Orientation.Vertical:
                        delta.y /= Mathf.Max(0.01f, axisBias);
                        d = delta.magnitude;
                        break;
                    default:
                        d = delta.magnitude;
                        break;
                }

                // Gaussian falloff -> scale
                float w = Mathf.Exp(-(d * d) / (2f * sigma * sigma));
                float targetScale = Mathf.Lerp(baseScale, maxScale, w);

                // Smooth width/height toward baseSize * targetScale
                var cur = st.rt.rect.size;
                float targetW = st.baseSize.x * targetScale;
                float targetH = st.baseSize.y * targetScale;

                float nextW = Mathf.SmoothDamp(cur.x, targetW, ref st.vel.x, smoothTime);
                float nextH = Mathf.SmoothDamp(cur.y, targetH, ref st.vel.y, smoothTime);

                if (!Mathf.Approximately(nextW, cur.x) || !Mathf.Approximately(nextH, cur.y))
                {
                    SetSize(st.rt, nextW, nextH);
                    requestedRebuild = true;
                }

                states[i] = st;
            }

            if (requestedRebuild)
            {
                requestedRebuild = false;
                LayoutRebuilder.MarkLayoutForRebuild(parentRect);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isHovering = true;
            UpdatePointerLocal(eventData);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovering = false;
        }

        public void OnPointerMove(PointerEventData eventData)
        {
            if (!isHovering) { return; }
            UpdatePointerLocal(eventData);
        }

        void UpdatePointerLocal(PointerEventData eventData)
        {
            if (!parentRect) { return; }
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, eventData.position, eventCamera, out var local))
            {
                lastPointerLocal = local;
            }
        }

        // Refresh pointer from current mouse (handles ScrollRect movement without mouse delta)
        void TryRefreshPointerFromMouse()
        {
            Vector2 screenPos = Utilities.GetPointerPosition();
            if (requirePointerInsideForRefresh && !RectTransformUtility.RectangleContainsScreenPoint(parentRect, screenPos, eventCamera)) { return; }
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPos, eventCamera, out var local)) { lastPointerLocal = local; }
        }

        void OnScrollRectValueChanged(Vector2 _)
        {
            if (isHovering && alwaysUpdateOnHover)
            {
                TryRefreshPointerFromMouse();
                requestedRebuild = true;
            }
        }

        void SetSize(RectTransform rt, float width, float height)
        {
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Max(0.0001f, width));
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(0.0001f, height));
        }

        Vector2 WorldToParentLocal(RectTransform child, Vector2 childLocalPoint)
        {
            Vector3 world = child.TransformPoint(childLocalPoint);
            Vector3 localInParent3D = parentRect.InverseTransformPoint(world);
            return (Vector2)localInParent3D;
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            sigma = Mathf.Max(1f, influenceRadius * 0.5f);
            baseScale = Mathf.Max(0.01f, baseScale);
            maxScale = Mathf.Max(baseScale, maxScale);
        }
#endif
    }
}
