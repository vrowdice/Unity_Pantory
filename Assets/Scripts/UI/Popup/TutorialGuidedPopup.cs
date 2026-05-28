using DG.Tweening;
using Evo.UI;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

    private List<TutorialData> _tutorialDataList;
    private string _gameObjectName;
    private int _localizationIndexOverride = -1;
    private string _localizationKeyOverride;
    private System.Action _advanceCallback;
    private System.Action _onDismissedUnexpectedly;

    private bool _allowClose;
    private bool _isRetiring;
    private bool _isFirstPanelPlacement = true;
    private Image _rootBlockerImage;
    private Image _focusPanelImage;

    public bool IsRetiring => _isRetiring;

    public void SetStepIndexOverride(int localizationIndex)
    {
        _localizationIndexOverride = localizationIndex;
        UpdateDescription(animateTransition: true);
    }

    public void SetLocalizationKey(string localizationKey)
    {
        _localizationKeyOverride = localizationKey;
        UpdateDescription(animateTransition: true);
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

        SetRaycastBlocker(!allowWorldInteraction);
    }

    public void Init(List<TutorialData> tutorialDataList, string gameObjectName)
    {
        base.Init();

        _tutorialDataList = tutorialDataList;
        _gameObjectName = gameObjectName;
        _localizationIndexOverride = -1;
        _localizationKeyOverride = null;

        HideUnusedNavigation();
        if (_nextBtn != null)
            _nextBtn.gameObject.SetActive(false);

        UpdateDescription();
        Show();
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
        TutorialStepTransition.Kill(_descriptionText);

        KillCommonTweens();
        transform.DOKill();
        _isRetiring = true;
        _allowClose = true;
        CloseAndDestroy();
    }

    private void UpdateDescription(bool animateTransition = false)
    {
        if (_tutorialDataList == null || _tutorialDataList.Count == 0)
            return;

        TutorialData currentData = _tutorialDataList[0];
        string localizationKey = !string.IsNullOrEmpty(_localizationKeyOverride)
            ? _localizationKeyOverride
            : $"{_gameObjectName}{(_localizationIndexOverride >= 0 ? _localizationIndexOverride : 0)}";
        string descriptionText = localizationKey.Localize(LocalizationUtils.TABLE_TUTORIAL_GUIDED);
        bool isFirstShow = _isFirstPanelPlacement;
        bool shouldAnimate = animateTransition
            && !isFirstShow
            && descriptionText != _descriptionText.text;

        System.Action applyContent = () => ApplyStepContent(currentData, descriptionText, !isFirstShow);

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

    private void ApplyStepContent(TutorialData currentData, string descriptionText, bool animatePanel)
    {
        _descriptionText.text = descriptionText;
        ApplyPanelPosition(currentData.tutorialPanelPosition, currentData.focusGameObject, animatePanel);
        currentData.onStepStart?.Invoke();
        ApplyFocusTarget(currentData.focusGameObject);
    }

    public void RefreshFocusTarget(GameObject focusGameObject)
    {
        if (_tutorialDataList == null || _tutorialDataList.Count == 0)
            return;

        TutorialData currentData = _tutorialDataList[0];
        currentData.focusGameObject = focusGameObject;

        ApplyPanelPosition(currentData.tutorialPanelPosition, focusGameObject, animatePanel: true);
        ApplyFocusTarget(focusGameObject);
    }

    private void HideUnusedNavigation()
    {
        if (_indexTextRoot != null)
            _indexTextRoot.SetActive(false);

        if (_beforeBtnRoot != null)
            _beforeBtnRoot.SetActive(false);
    }

    private void SetRaycastBlocker(bool block)
    {
        if (_rootBlockerImage == null)
            _rootBlockerImage = GetComponent<Image>();

        if (_rootBlockerImage != null)
            _rootBlockerImage.raycastTarget = block;

        if (_focusPanelImage == null && _focusPanel != null)
            _focusPanelImage = _focusPanel.GetComponent<Image>();

        if (_focusPanelImage != null)
            _focusPanelImage.raycastTarget = block;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        if (!_allowClose && !_isRetiring)
            _onDismissedUnexpectedly?.Invoke();

        TutorialStepTransition.Kill(_descriptionText);

        KillCommonTweens();
        transform.DOKill();
    }
}
