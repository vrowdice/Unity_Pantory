using System;
using UnityEngine;
using UnityEngine.UI;

public class PlayPauseBtn : MonoBehaviour
{
    [SerializeField] private Image _playIcon;
    [SerializeField] private Image _pauseIcon;

    private Action _onClicked;

    public void Init(Action onClicked)
    {
        _onClicked = onClicked;
        SetPausedVisual(true);
    }

    public void SetPausedVisual(bool isPaused)
    {
        if (_playIcon != null)
        {
            _playIcon.gameObject.SetActive(isPaused);
        }

        if (_pauseIcon != null)
        {
            _pauseIcon.gameObject.SetActive(!isPaused);
        }
    }

    public void OnClick()
    {
        _onClicked?.Invoke();
    }
}
