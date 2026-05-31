using System.Collections;
using DG.Tweening;
using Evo.UI;
using TMPro;
using UnityEngine;

/// <summary>
/// 튜토리얼 씬 TutorialDirector가 제어하는 강제 가이드 패널.
/// </summary>
public class TutorialGuidedPopup : TutorialPopupBase
{
    [SerializeField] private TextMeshProUGUI _descriptionText;
    [SerializeField] private GameObject _indexTextRoot;
    [SerializeField] private GameObject _beforeBtnRoot;
    [SerializeField] private Evo.UI.Button _nextBtn;

    [SerializeField] private float _stepTransitionDuration = 0.25f;
    [SerializeField] private Ease _stepTransitionEase = Ease.OutCubic;

    private int _stepIndex;
    private GameObject _layoutFocusObject;
    private string _layoutFocusObjectName;
    private Vector2 _panelPosition;
    private Coroutine _focusRetryCoroutine;
    private System.Action _advanceCallback;
    private System.Action _onDismissedUnexpectedly;

    private bool _allowClose;
    private bool _isRetiring;
    private bool _isFirstPanelPlacement = true;

    public bool IsRetiring => _isRetiring;

    public override void Init()
    {
        base.Init();

        ClearFocusHighlight();

        HideUnusedNavigation();
        if (_nextBtn != null)
            _nextBtn.gameObject.SetActive(false);

        Show();
    }

    public void ApplyStep(
        int stepIndex,
        GameObject focusObject,
        Vector2 panelPosition,
        bool showNextButton,
        string focusObjectName = null)
    {
        _stepIndex = stepIndex;
        _layoutFocusObject = focusObject;
        _layoutFocusObjectName = focusObjectName;
        _panelPosition = panelPosition;

        StopFocusRetry();
        ClearFocusHighlight();

        ConfigureStepPresentation(allowWorldInteraction: true, showNextButton);
        UpdateDescription(animateTransition: stepIndex > 0);
    }

    public void SetAdvanceCallback(System.Action advanceCallback)
    {
        _advanceCallback = advanceCallback;
    }

    public void SetOnDismissedUnexpectedly(System.Action onDismissedUnexpectedly)
    {
        _onDismissedUnexpectedly = onDismissedUnexpectedly;
    }

    public void ConfigureStepPresentation(bool allowWorldInteraction, bool showNextButton)
    {
        _isRetiring = false;
        _allowClose = false;

        HideUnusedNavigation();
        if (_nextBtn != null)
        {
            _nextBtn.gameObject.SetActive(showNextButton);
            if (showNextButton)
                _nextBtn.interactable = true;
        }
    }

    public override void Close()
    {
        if (_allowClose || _isRetiring)
        {
            base.Close();
            return;
        }

        UIManager.Instance?.ShowConfirmPopup(
            ConfirmMessage.TutorialSkipConfirm,
            () => TutorialDirector.Instance?.CompleteTutorial());
        UIManager.Instance?.PushCloseable(Close);
    }

    public void OnClickNextBtn()
    {
        _advanceCallback?.Invoke();
    }

    public void OnClickResetBtn()
    {
        UIManager.Instance?.ShowConfirmPopup(
            ConfirmMessage.ReplayTutorialConfirm,
            () => TutorialDirector.Instance?.ResetTutorial());
    }

    public void Dismiss()
    {
        StopFocusRetry();
        TutorialStepTransition.Kill(_descriptionText);

        KillCommonTweens();
        transform.DOKill();
        _isRetiring = true;
        _allowClose = true;
        CloseAndDestroy();
    }

    private void UpdateDescription(bool animateTransition = false)
    {
        string localizationKey = $"TutorialScene{_stepIndex}";
        string descriptionText = localizationKey.Localize(LocalizationUtils.TABLE_TUTORIAL_GUIDED);
        bool isFirstShow = _isFirstPanelPlacement;
        bool shouldAnimate = animateTransition
            && !isFirstShow
            && descriptionText != _descriptionText.text;

        System.Action applyContent = () => ApplyStepContent(descriptionText);

        if (shouldAnimate)
        {
            TutorialStepTransition.Play(
                _descriptionText,
                null,
                applyContent,
                _stepTransitionDuration,
                _stepTransitionEase);
        }
        else
        {
            TutorialStepTransition.EnsureVisible(_descriptionText);
            applyContent();
        }

        _isFirstPanelPlacement = false;
    }

    private void ApplyStepContent(string descriptionText)
    {
        _descriptionText.text = descriptionText;

        GameObject focusObject = ResolveLayoutFocusObject();
        ApplyPanelPosition(_panelPosition, focusObject, animatePanel: _stepIndex > 0);
        TryApplyFocusTarget(focusObject);
    }

    private GameObject ResolveLayoutFocusObject()
    {
        if (_layoutFocusObject != null)
            return _layoutFocusObject;

        if (string.IsNullOrWhiteSpace(_layoutFocusObjectName))
            return null;

        return TutorialFocusResolver.FindFocusObject(_layoutFocusObjectName);
    }

    private void TryApplyFocusTarget(GameObject focusObject)
    {
        if (focusObject != null)
        {
            StopFocusRetry();
            ApplyFocusTarget(focusObject);
            return;
        }

        if (!string.IsNullOrWhiteSpace(_layoutFocusObjectName))
            StartFocusRetry();
    }

    private void StartFocusRetry()
    {
        StopFocusRetry();
        _focusRetryCoroutine = StartCoroutine(FocusRetryRoutine());
    }

    private void StopFocusRetry()
    {
        if (_focusRetryCoroutine == null)
            return;

        StopCoroutine(_focusRetryCoroutine);
        _focusRetryCoroutine = null;
    }

    private IEnumerator FocusRetryRoutine()
    {
        const int maxFrames = 90;

        for (int i = 0; i < maxFrames; i++)
        {
            yield return null;

            GameObject found = ResolveLayoutFocusObject();
            if (found == null)
                continue;

            _layoutFocusObject = found;
            ApplyFocusTarget(found);
            yield break;
        }
    }

    private void HideUnusedNavigation()
    {
        if (_indexTextRoot != null)
            _indexTextRoot.SetActive(false);

        if (_beforeBtnRoot != null)
            _beforeBtnRoot.SetActive(false);
    }

    protected override void OnDestroy()
    {
        StopFocusRetry();
        base.OnDestroy();

        if (!_allowClose && !_isRetiring)
            _onDismissedUnexpectedly?.Invoke();

        TutorialStepTransition.Kill(_descriptionText);

        KillCommonTweens();
        transform.DOKill();
    }
}
