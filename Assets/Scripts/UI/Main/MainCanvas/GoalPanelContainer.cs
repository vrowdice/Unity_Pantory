using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 현재 활성 목표와 진행도를 표시합니다. 참조가 없으면 런타임에 기본 UI를 생성합니다.
/// </summary>
public class GoalPanelContainer : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _titleText;
    [SerializeField] private TextMeshProUGUI _descriptionText;
    [SerializeField] private TextMeshProUGUI _progressText;
    [SerializeField] private TextMeshProUGUI _rewardText;
    [SerializeField] private Slider _progressSlider;
    [SerializeField] private CanvasGroup _canvasGroup;

    private DataManager _dataManager;

    public void Init(DataManager dataManager)
    {
        _dataManager = dataManager;
        EnsureDefaultUi();
        SubscribeEvents();
        Refresh();
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
    }

    private void SubscribeEvents()
    {
        if (_dataManager?.Goal == null)
            return;

        _dataManager.Goal.OnActiveGoalsChanged -= HandleActiveGoalsChanged;
        _dataManager.Goal.OnGoalCompleted -= HandleGoalCompleted;
        _dataManager.Goal.OnAllGoalsCompleted -= HandleAllGoalsCompleted;
        _dataManager.Goal.OnActiveGoalsChanged += HandleActiveGoalsChanged;
        _dataManager.Goal.OnGoalCompleted += HandleGoalCompleted;
        _dataManager.Goal.OnAllGoalsCompleted += HandleAllGoalsCompleted;
    }

    private void UnsubscribeEvents()
    {
        if (_dataManager?.Goal == null)
            return;

        _dataManager.Goal.OnActiveGoalsChanged -= HandleActiveGoalsChanged;
        _dataManager.Goal.OnGoalCompleted -= HandleGoalCompleted;
        _dataManager.Goal.OnAllGoalsCompleted -= HandleAllGoalsCompleted;
    }

    private void HandleActiveGoalsChanged()
    {
        Refresh();
    }

    private void HandleGoalCompleted(GoalState goalState)
    {
        Refresh();
    }

    private void HandleAllGoalsCompleted()
    {
        SetVisible(false);
    }

    private void Refresh()
    {
        GoalDataHandler goalHandler = _dataManager?.Goal;
        if (goalHandler == null || goalHandler.AllGoalsCompleted || goalHandler.ActiveGoals.Count == 0)
        {
            SetVisible(false);
            return;
        }

        SetVisible(true);
        GoalState activeGoal = goalHandler.ActiveGoals[0];
        GoalData activeGoalData = goalHandler.GetGoalData(activeGoal.goalId);

        _titleText.text = "GoalPanelTitle".Localize(LocalizationUtils.TABLE_COMMON);
        _descriptionText.text = GoalBtn.GetTitle(activeGoalData);
        _progressText.text = GoalBtn.FormatProgress(activeGoal);
        _rewardText.text = GoalBtn.FormatReward(activeGoal.rewardCredit);
        _progressSlider.minValue = 0f;
        _progressSlider.maxValue = 1f;
        _progressSlider.value = activeGoal.ProgressRatio;
    }

    private void SetVisible(bool visible)
    {
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = visible ? 1f : 0f;
            _canvasGroup.interactable = visible;
            _canvasGroup.blocksRaycasts = visible;
            return;
        }

        gameObject.SetActive(visible);
    }

    private void EnsureDefaultUi()
    {
        if (_titleText != null && _descriptionText != null && _progressSlider != null)
            return;

        RectTransform root = transform as RectTransform;
        if (root == null)
            root = gameObject.AddComponent<RectTransform>();

        root.anchorMin = new Vector2(0f, 1f);
        root.anchorMax = new Vector2(0f, 1f);
        root.pivot = new Vector2(0f, 1f);
        root.anchoredPosition = new Vector2(24f, -120f);
        root.sizeDelta = new Vector2(380f, 100f);

        if (_canvasGroup == null)
            _canvasGroup = gameObject.GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();

        Image background = gameObject.GetComponent<Image>();
        if (background == null)
        {
            background = gameObject.AddComponent<Image>();
            background.color = new Color(0.08f, 0.1f, 0.14f, 0.88f);
            background.raycastTarget = false;
        }

        TMP_FontAsset font = FindReferenceFont();

        if (_titleText == null)
            _titleText = CreateText("TitleText", root, new Vector2(16f, -10f), new Vector2(348f, 28f), 22f, FontStyles.Bold, font);

        if (_descriptionText == null)
            _descriptionText = CreateText("DescriptionText", root, new Vector2(16f, -38f), new Vector2(348f, 28f), 18f, FontStyles.Normal, font);

        if (_progressText == null)
            _progressText = CreateText("ProgressText", root, new Vector2(16f, -68f), new Vector2(120f, 24f), 16f, FontStyles.Normal, font);

        if (_rewardText == null)
            _rewardText = CreateText("RewardText", root, new Vector2(140f, -68f), new Vector2(224f, 24f), 16f, FontStyles.Normal, font, TextAlignmentOptions.Right);

        if (_progressSlider == null)
            _progressSlider = CreateProgressSlider(root);
    }

    private static TMP_FontAsset FindReferenceFont()
    {
        TextMeshProUGUI reference = Object.FindAnyObjectByType<TextMeshProUGUI>();
        return reference != null ? reference.font : null;
    }

    private static TextMeshProUGUI CreateText(
        string objectName,
        RectTransform parent,
        Vector2 anchoredPosition,
        Vector2 sizeDelta,
        float fontSize,
        FontStyles fontStyle,
        TMP_FontAsset font,
        TextAlignmentOptions alignment = TextAlignmentOptions.Left)
    {
        GameObject textObj = new GameObject(objectName, typeof(RectTransform));
        textObj.transform.SetParent(parent, false);

        RectTransform rect = textObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }

    private static Slider CreateProgressSlider(RectTransform parent)
    {
        GameObject sliderObj = new GameObject("ProgressSlider", typeof(RectTransform));
        sliderObj.transform.SetParent(parent, false);

        RectTransform sliderRect = sliderObj.GetComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0f, 1f);
        sliderRect.anchorMax = new Vector2(0f, 1f);
        sliderRect.pivot = new Vector2(0f, 1f);
        sliderRect.anchoredPosition = new Vector2(16f, -92f);
        sliderRect.sizeDelta = new Vector2(348f, 12f);

        Image backgroundImage = sliderObj.AddComponent<Image>();
        backgroundImage.color = new Color(1f, 1f, 1f, 0.15f);
        backgroundImage.raycastTarget = false;

        Slider slider = sliderObj.AddComponent<Slider>();
        slider.interactable = false;
        slider.transition = Selectable.Transition.None;

        GameObject fillAreaObj = new GameObject("Fill Area", typeof(RectTransform));
        fillAreaObj.transform.SetParent(sliderObj.transform, false);
        RectTransform fillAreaRect = fillAreaObj.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = Vector2.zero;
        fillAreaRect.offsetMax = Vector2.zero;

        GameObject fillObj = new GameObject("Fill", typeof(RectTransform));
        fillObj.transform.SetParent(fillAreaObj.transform, false);
        RectTransform fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        Image fillImage = fillObj.AddComponent<Image>();
        fillImage.color = new Color(0.35f, 0.82f, 0.45f, 1f);
        fillImage.raycastTarget = false;

        slider.fillRect = fillRect;
        slider.targetGraphic = fillImage;
        return slider;
    }
}
