using DG.Tweening;
using Evo.UI;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TutorialPopup : TutorialPopupBase
{
    [SerializeField] private TextMeshProUGUI _indexText;
    [SerializeField] private TextMeshProUGUI _descriptionText;

    [SerializeField] private Button _beforeBtn;
    [SerializeField] private Button _nextBtn;

    [SerializeField] private float _stepTransitionDuration = 0.25f;
    [SerializeField] private Ease _stepTransitionEase = Ease.OutCubic;

    private List<TutorialData> _tutorialDataList;
    private string _gameObjectName;
    private int _currentIndex = 0;
    private bool _isFirstPanelPlacement = true;

    public void Init(List<TutorialData> tutorialDataList, string gameObjectName)
    {
        base.Init();

        _tutorialDataList = tutorialDataList;
        _gameObjectName = gameObjectName;
        _currentIndex = 0;

        UpdateDescription();
        Show();
    }

    public void OnClickBeforeBtn()
    {
        _currentIndex = Mathf.Max(0, _currentIndex - 1);
        UpdateDescription(animateTransition: true);
    }

    public void OnClickNextBtn()
    {
        if (_currentIndex >= _tutorialDataList.Count - 1)
        {
            OnClickExit();
            return;
        }

        _currentIndex++;
        UpdateDescription(animateTransition: true);
    }

    private void UpdateDescription(bool animateTransition = false)
    {
        if (_tutorialDataList == null || _tutorialDataList.Count == 0)
            return;

        _beforeBtn.interactable = _currentIndex > 0;
        _nextBtn.interactable = true;

        if (_currentIndex < 0 || _currentIndex >= _tutorialDataList.Count)
            return;

        TutorialData currentData = _tutorialDataList[_currentIndex];
        string indexText = $"{_currentIndex + 1} / {_tutorialDataList.Count}";
        string descriptionText = $"{_gameObjectName + _currentIndex}".Localize(LocalizationUtils.TABLE_TUTORIAL);
        bool isFirstShow = _isFirstPanelPlacement;
        bool shouldAnimate = animateTransition
            && !isFirstShow
            && (descriptionText != _descriptionText.text || indexText != _indexText.text);

        System.Action applyContent = () => ApplyStepContent(currentData, indexText, descriptionText, !isFirstShow);

        if (shouldAnimate)
        {
            TutorialStepTransition.Play(
                _descriptionText,
                _indexText,
                applyContent,
                _stepTransitionDuration,
                _stepTransitionEase);
        }
        else
        {
            TutorialStepTransition.EnsureVisible(_descriptionText, _indexText);
            applyContent();
        }

        _isFirstPanelPlacement = false;
    }

    private void ApplyStepContent(
        TutorialData currentData,
        string indexText,
        string descriptionText,
        bool animatePanel)
    {
        _indexText.text = indexText;
        _descriptionText.text = descriptionText;

        ApplyPanelPosition(currentData.tutorialPanelPosition, currentData.focusGameObject, animatePanel);
        currentData.onStepStart?.Invoke();
        ApplyFocusTarget(currentData.focusGameObject);
    }

    public void OnClickExit()
    {
        TutorialStepTransition.Kill(_descriptionText, _indexText);

        KillCommonTweens();
        transform.DOKill();
        DataManager.Instance?.Player?.MarkTutorialSequenceFinishedForOwner(_gameObjectName);
        CloseAndDestroy();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        TutorialStepTransition.Kill(_descriptionText, _indexText);

        KillCommonTweens();
        transform.DOKill();
    }
}
