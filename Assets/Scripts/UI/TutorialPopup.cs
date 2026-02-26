using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Evo.UI;
using DG.Tweening;

public class TutorialPopup : PopupBase
{
    [SerializeField] private TextMeshProUGUI _indexText;
    [SerializeField] private TextMeshProUGUI _descriptionText;
    [SerializeField] private GameObject _panel;
    [SerializeField] private GameObject _focusPanel;

    [SerializeField] private Button _beforeBtn;
    [SerializeField] private Button _nextBtn;

    [SerializeField] private float _focusPulseScale = 1.1f;
    [SerializeField] private float _focusPulseDuration = 0.6f;

    private List<TutorialData> _tutorialDataList;
    private int _currentIndex = 0;
    private int _totalCount = 0;

    public void Init(List<TutorialData> tutorialDataList)
    {
        base.Init();

        _tutorialDataList = tutorialDataList;
        _currentIndex = 0;
        _totalCount = _tutorialDataList.Count;

        UpdateDescription();

        Show();
    }

    public void OnClickBeforeBtn()
    {
        _currentIndex = Mathf.Max(0, _currentIndex - 1);
        UpdateDescription();
    }

    public void OnClickNextBtn()
    {
        _currentIndex = Mathf.Min(_tutorialDataList.Count - 1, _currentIndex + 1);
        UpdateDescription();
    }
    
    private void UpdateDescription()
    {
        if (_tutorialDataList == null || _tutorialDataList.Count == 0) return;

        _beforeBtn.interactable = _currentIndex > 0;
        _nextBtn.interactable = _currentIndex < _tutorialDataList.Count - 1;

        if (_currentIndex >= 0 && _currentIndex < _tutorialDataList.Count)
        {
            TutorialData currentData = _tutorialDataList[_currentIndex];
            _indexText.text = $"{_currentIndex + 1} / {_totalCount}";
            _descriptionText.text = currentData._description;
            _panel.GetComponent<RectTransform>().anchoredPosition = currentData._tutorialPanelPosition;

            if (currentData._focusGameObject != null)
            {
                _focusPanel.SetActive(true);

                Transform focusTransform = _focusPanel.transform;
                Transform targetTransform = currentData._focusGameObject.transform;

                focusTransform.position = targetTransform.position;
                focusTransform.DOKill();

                Vector3 baseScale = targetTransform.localScale;
                focusTransform.localScale = baseScale;

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
    }

    public void OnClickExit()
    {
        if (_focusPanel != null)
        {
            _focusPanel.transform.DOKill();
        }

        transform.DOKill();
        Close();
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (_focusPanel != null)
        {
            _focusPanel.transform.DOKill();
        }

        transform.DOKill();
    }
}
