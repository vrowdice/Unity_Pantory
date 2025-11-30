using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Evo.UI
{
    [HelpURL(Constants.HELP_URL + "ui-elements/tooltip")]
    [RequireComponent(typeof(CanvasGroup))]
    [RequireComponent(typeof(RectTransform))]
    public class TooltipPreset : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RectTransform tooltipRect;
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private ContentSizeFitter contentSizeFitter;
        [SerializeField] private GameObject contentContainer;
        [SerializeField] private LayoutGroup layoutGroup;
        public LayoutElement layoutElement;
        public CanvasGroup canvasGroup;

        public void SetupTooltip(string title, string description, Sprite icon, float maxWidth)
        {
            CheckForReferences();

            if (string.IsNullOrEmpty(title) && icon == null) { titleText.transform.parent.gameObject.SetActive(false); }
            else
            {
                SetIcon(icon);
                SetTitle(title);
            }

            SetDescription(description);
            ForceLayoutUpdate(maxWidth);
        }

        public void SetupCustomContent(GameObject customContentPrefab, float maxWidth)
        {
            CheckForReferences();

            // Hide content container, show custom container
            if (contentContainer != null) { contentContainer.SetActive(false); }
            if (customContentPrefab != null) { Instantiate(customContentPrefab, transform); }

            ForceLayoutUpdate(maxWidth);
        }

        void CheckForReferences()
        {
            if (canvasGroup == null) { canvasGroup = GetComponent<CanvasGroup>(); }
            if (tooltipRect == null) { tooltipRect = GetComponent<RectTransform>(); }
        }

        void SetTitle(string title)
        {
            titleText.text = title;
            titleText.gameObject.SetActive(!string.IsNullOrEmpty(title));
        }

        void SetDescription(string description)
        {
            descriptionText.text = description;
            descriptionText.gameObject.SetActive(!string.IsNullOrEmpty(description));
        }

        void SetIcon(Sprite icon)
        {
            bool hasIcon = icon != null;
            iconImage.sprite = icon;
            iconImage.gameObject.SetActive(hasIcon);
        }

        void ForceLayoutUpdate(float maxWidth)
        {
            if (contentSizeFitter != null)
            {
                contentSizeFitter.SetLayoutHorizontal();
                contentSizeFitter.SetLayoutVertical();
            }

            if (layoutGroup != null)
            {
                layoutGroup.SetLayoutHorizontal();
                layoutGroup.SetLayoutVertical();
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipRect);
            Canvas.ForceUpdateCanvases();

            if (layoutElement != null)
            {
                layoutElement.enabled = tooltipRect.sizeDelta.x > maxWidth;
                layoutElement.preferredWidth = maxWidth;
            }
        }
    }
}