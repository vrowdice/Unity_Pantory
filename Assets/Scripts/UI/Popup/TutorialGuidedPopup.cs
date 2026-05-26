using DG.Tweening;
using Evo.UI;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 튜토리얼 씬 TutorialDirector가 제어하는 강제 가이드 패널.
/// </summary>
public class TutorialGuidedPopup : PopupBase
{
    [SerializeField] private TextMeshProUGUI _descriptionText;
    [SerializeField] private GameObject _panel;
    [SerializeField] private GameObject _focusPanel;
    [SerializeField] private GameObject _indexTextRoot;
    [SerializeField] private GameObject _beforeBtnRoot;
    [SerializeField] private Evo.UI.Button _nextBtn;

    [SerializeField] private float _focusPulseScale = 1.1f;
    [SerializeField] private float _focusPulseDuration = 0.6f;
    [SerializeField] private float _panelMoveDuration = 0.35f;
    [SerializeField] private Ease _panelMoveEase = Ease.OutCubic;
    [SerializeField] private float _screenEdgePadding = 16f;

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
        UpdateDescription();
    }

    public void SetLocalizationKey(string localizationKey)
    {
        _localizationKeyOverride = localizationKey;
        UpdateDescription();
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
        if (_focusPanel != null)
            _focusPanel.transform.DOKill();

        if (_panel != null)
            _panel.GetComponent<RectTransform>()?.DOKill();

        transform.DOKill();
        _isRetiring = true;
        _allowClose = true;
        CloseAndDestroy();
    }

    private void UpdateDescription()
    {
        if (_tutorialDataList == null || _tutorialDataList.Count == 0)
            return;

        TutorialData currentData = _tutorialDataList[0];
        string localizationKey = !string.IsNullOrEmpty(_localizationKeyOverride)
            ? _localizationKeyOverride
            : $"{_gameObjectName}{(_localizationIndexOverride >= 0 ? _localizationIndexOverride : 0)}";
        _descriptionText.text = localizationKey.Localize(LocalizationUtils.TABLE_TUTORIAL_GUIDED);

        ApplyPanelPosition(currentData.tutorialPanelPosition, currentData.focusGameObject);
        currentData.onStepStart?.Invoke();

        ApplyFocusTarget(currentData.focusGameObject);
    }

    public void RefreshFocusTarget(GameObject focusGameObject)
    {
        if (_tutorialDataList == null || _tutorialDataList.Count == 0)
            return;

        TutorialData currentData = _tutorialDataList[0];
        currentData.focusGameObject = focusGameObject;

        ApplyPanelPosition(currentData.tutorialPanelPosition, focusGameObject);
        ApplyFocusTarget(focusGameObject);
    }

    private void ApplyPanelPosition(Vector2 desiredPosition, GameObject focusTarget)
    {
        RectTransform panelRect = _panel.GetComponent<RectTransform>();
        bool animate = !_isFirstPanelPlacement;
        TutorialPanelLayout.MovePanelToPosition(
            panelRect,
            desiredPosition,
            focusTarget,
            _screenEdgePadding,
            animate,
            _panelMoveDuration,
            _panelMoveEase);
        _isFirstPanelPlacement = false;
    }

    private void ApplyFocusTarget(GameObject focusGameObject)
    {
        if (focusGameObject != null)
        {
            _focusPanel.SetActive(true);

            RectTransform focusTransform = _focusPanel.GetComponent<RectTransform>();
            RectTransform targetTransform = focusGameObject.GetComponent<RectTransform>();

            focusTransform.position = targetTransform.position;
            Vector3 baseScale = targetTransform.localScale;
            focusTransform.localScale = baseScale;
            RectTransformUtils.SyncSizeToTarget(focusTransform, targetTransform);
            focusTransform.DOKill();

            focusTransform
                .DOScale(baseScale * _focusPulseScale, _focusPulseDuration)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }
        else
        {
            _focusPanel.transform.DOKill();
            _focusPanel.SetActive(false);
        }
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

    private void OnDestroy()
    {
        if (!_allowClose && !_isRetiring)
            _onDismissedUnexpectedly?.Invoke();

        if (_focusPanel != null)
            _focusPanel.transform.DOKill();

        if (_panel != null)
            _panel.GetComponent<RectTransform>()?.DOKill();

        transform.DOKill();
    }
}
