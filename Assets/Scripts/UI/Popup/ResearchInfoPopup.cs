using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ResearchInfoPopup : PopupBase
{
    [Header("Complete Feedback")]
    [SerializeField] private RectTransform _researchActionButton;
    [SerializeField] private AudioClip _researchCompleteSfx;

    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _tireText;
    [SerializeField] private TextMeshProUGUI _descriptionText;
    [SerializeField] private Image _iconImage;
    [SerializeField] private Transform _researchEffectScrollViewContentTransform;
    [SerializeField] private TextMeshProUGUI _costPanelText;

    private ResearchEntry _currentResearchEntry;
    private GameManager _gameManager;
    private static bool _skipCanvasCompleteSfxOnce;

    public static bool ConsumeCanvasCompleteSfxSkip()
    {
        bool skip = _skipCanvasCompleteSfxOnce;
        _skipCanvasCompleteSfxOnce = false;
        return skip;
    }

    public void Init(ResearchEntry researchEntry)
    {
        base.Init();

        _gameManager = GameManager.Instance;
        _currentResearchEntry = researchEntry;

        RefreshAllUI();

        Show();
    }

    protected override void HandleDayChanged()
    {
        if (gameObject.activeSelf)
        {
            RefreshAllUI();
        }
    }

    public void RefreshAllUI()
    {
        string researchId = _currentResearchEntry.data.id;
        _nameText.text = researchId.Localize(LocalizationUtils.TABLE_RESEARCH);
        _descriptionText.text = (researchId + LocalizationUtils.KEY_SUFFIX_DESC).Localize(LocalizationUtils.TABLE_RESEARCH);
        _iconImage.sprite = _currentResearchEntry.data.icon;
        _costPanelText.text = _currentResearchEntry.data.researchPointCost.ToString();

        PoolingManager.Instance.ClearChildrenToPool(_researchEffectScrollViewContentTransform);
        foreach (EffectData effectData in _currentResearchEntry.data.effects)
        {
            UIManager.Instance.CreateEffectTextPairPanel(_researchEffectScrollViewContentTransform, new EffectState(effectData));
        }
    }

    public void OnClickResearchBtn()
    {
        if (!_dataManager.Research.TryUnlockResearch(_currentResearchEntry.data.id))
        {
            UIManager.Instance.ShowWarningPopup(WarningMessage.ResearchCannotUnlock);
            return;
        }

        _skipCanvasCompleteSfxOnce = true;

        Transform feedbackTarget = _researchActionButton != null
            ? _researchActionButton
            : transform;
        RequirementCompleteFeedbackUtils.Play(feedbackTarget, _researchCompleteSfx);
        Close();
    }
}
