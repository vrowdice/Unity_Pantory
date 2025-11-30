using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

namespace Evo.UI
{
    [DisallowMultipleComponent]
    [HelpURL(Constants.HELP_URL + "ui-elements/input-field")]
    [AddComponentMenu("Evo/UI/UI Elements/Input Field Enhancer")]
    public class InputFieldEnhancer : MonoBehaviour
    {
        [EvoHeader("Settings", Constants.CUSTOM_EDITOR_ID)]
        [SerializeField] private PlaceholderAnimation animationType = PlaceholderAnimation.Slide;
        [SerializeField] private Vector2 slideOffset = new(0, 20);
        [SerializeField, Range(0, 1)] private float fadeAlpha = 0;
        [SerializeField, Range(0.1f, 3)] private float scaleMultiplier = 0.8f;
        [SerializeField, Range(0.1f, 2)] private float animationDuration = 0.25f;
        [SerializeField] private AnimationCurve animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [EvoHeader("References", Constants.CUSTOM_EDITOR_ID)]
        public TMP_InputField source;
        [SerializeField] private Interactive interactableObject;

        [EvoHeader("Events", Constants.CUSTOM_EDITOR_ID)]
        public SubmitEvent onSubmit = new();

        public enum PlaceholderAnimation
        {
            Fade = 0,
            FadeScale = 1,
            Slide = 2
        }

        [System.Serializable] public class SubmitEvent : UnityEvent<string> { }

        // Cache
        TextMeshProUGUI placeholderText;
        RectTransform placeholderRect;

        // Animation state
        Coroutine currentAnimation;
        Vector2 originalPlaceholderPosition;
        Vector3 originalPlaceholderScale;
        Color originalPlaceholderColor;

        void Awake()
        {
            Initialize();
        }

        void OnEnable()
        {
            if (source == null)
                return;

            AnimatePlaceholder(!string.IsNullOrEmpty(source.text));
        }

        void Initialize()
        {
            if (source == null) { source = GetComponentInChildren<TMP_InputField>(); }
            if (source == null)
            {
                Debug.LogError("[Input Field Enhancer] TMP Input Field is missing in children!", this);
                return;
            }

            source.onEndEdit.AddListener(OnEndEdit);
            source.onValueChanged.AddListener(OnValueChanged);
            source.onSelect.AddListener(delegate { AnimatePlaceholder(true); });
            source.onDeselect.AddListener(delegate { if (string.IsNullOrEmpty(source.text)) { AnimatePlaceholder(false); } });

            if (source.placeholder != null)
            {
                placeholderText = source.placeholder.GetComponent<TextMeshProUGUI>();
                placeholderRect = placeholderText.rectTransform;
                source.placeholder = null;
                placeholderText.enabled = true;

                originalPlaceholderPosition = placeholderRect.anchoredPosition;
                originalPlaceholderScale = placeholderRect.localScale;
                originalPlaceholderColor = placeholderText.color;
            }

            if (interactableObject != null)
            {
                interactableObject.OnStateChanged += OnInteractableStateChanged;
                interactableObject.onSelect.AddListener(() => StartCoroutine(SelectHelper()));

                source.onSelect.AddListener(delegate { interactableObject.SetState(InteractionState.Selected); });
                source.onDeselect.AddListener(delegate { interactableObject.SetState(source.interactable ? InteractionState.Normal : InteractionState.Disabled); });
            }
        }

        void OnEndEdit(string value)
        {
            if (Utilities.WasEnterKeyPressed())
            {
                onSubmit?.Invoke(value);
            }
        }

        void OnValueChanged(string value)
        {
            if (string.IsNullOrEmpty(value)) { AnimatePlaceholder(false); }
            else { AnimatePlaceholder(true); }
        }

        void OnInteractableStateChanged(InteractionState newState)
        {
            if (newState == InteractionState.Disabled && source.interactable) { source.interactable = false; }
            if (!source.interactable) { interactableObject.SetState(InteractionState.Disabled); }
        }

        void AnimatePlaceholder(bool animate)
        {
            if (!gameObject.activeInHierarchy || placeholderRect == null || placeholderText == null) { return; }
            if (currentAnimation != null) { StopCoroutine(currentAnimation); }

            switch (animationType)
            {
                case PlaceholderAnimation.Fade:
                    currentAnimation = StartCoroutine(AnimateFade(animate));
                    break;

                case PlaceholderAnimation.FadeScale:
                    currentAnimation = StartCoroutine(AnimateFadeScale(animate));
                    break;

                case PlaceholderAnimation.Slide:
                    currentAnimation = StartCoroutine(AnimateSlide(animate));
                    break;
            }
        }

        IEnumerator SelectHelper()
        {
            yield return new WaitForEndOfFrame();
            source.Select();
        }

        IEnumerator AnimateFade(bool animate)
        {
            Color startColor = placeholderText.color;
            Color sourceColor = animate ?
                new Color(originalPlaceholderColor.r, originalPlaceholderColor.g, originalPlaceholderColor.b, fadeAlpha) :
                originalPlaceholderColor;

            float elapsed = 0f;

            while (elapsed < animationDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / animationDuration;
                t = animationCurve.Evaluate(t);

                placeholderText.color = Color.Lerp(startColor, sourceColor, t);

                yield return null;
            }

            placeholderText.color = sourceColor;
            currentAnimation = null;
        }

        IEnumerator AnimateFadeScale(bool animate)
        {
            Color startColor = placeholderText.color;
            Color sourceColor = animate ?
                new Color(originalPlaceholderColor.r, originalPlaceholderColor.g, originalPlaceholderColor.b, fadeAlpha) :
                originalPlaceholderColor;

            Vector3 startScale = placeholderRect.localScale;
            Vector3 sourceScale = animate ? originalPlaceholderScale * scaleMultiplier : originalPlaceholderScale;

            float elapsed = 0f;

            while (elapsed < animationDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / animationDuration;
                t = animationCurve.Evaluate(t);

                placeholderText.color = Color.Lerp(startColor, sourceColor, t);
                placeholderRect.localScale = Vector3.Lerp(startScale, sourceScale, t);

                yield return null;
            }

            placeholderText.color = sourceColor;
            placeholderRect.localScale = sourceScale;
            currentAnimation = null;
        }

        IEnumerator AnimateSlide(bool animate)
        {
            Vector2 startPosition = placeholderRect.anchoredPosition;
            Vector2 sourcePosition = animate ? originalPlaceholderPosition + slideOffset : originalPlaceholderPosition;

            Vector3 startScale = placeholderRect.localScale;
            Vector3 sourceScale = animate ? originalPlaceholderScale * scaleMultiplier : originalPlaceholderScale;

            float elapsed = 0f;

            while (elapsed < animationDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / animationDuration;
                t = animationCurve.Evaluate(t);

                placeholderRect.anchoredPosition = Vector2.Lerp(startPosition, sourcePosition, t);
                placeholderRect.localScale = Vector3.Lerp(startScale, sourceScale, t);

                yield return null;
            }

            placeholderRect.anchoredPosition = sourcePosition;
            placeholderRect.localScale = sourceScale;
            currentAnimation = null;
        }

        public void Focus()
        {
            source.Select();
            source.ActivateInputField();
        }

#if UNITY_EDITOR
        [HideInInspector] public bool settingsFoldout = true;
        [HideInInspector] public bool referencesFoldout = false;
        [HideInInspector] public bool eventsFoldout = false;
#endif
    }
}