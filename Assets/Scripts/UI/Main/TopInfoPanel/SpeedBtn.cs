using System;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class SpeedBtn : MonoBehaviour
{
    [SerializeField] private GameObject _sigPanel;
    [SerializeField] private TextMeshProUGUI _text;
    [SerializeField, Min(0.01f)] private float _sigPanelTweenDuration = 0.2f;

    public float Speed { get; private set; }
    public int Index { get; private set; }

    private Action<SpeedBtn> _onClicked;

    public void Init(float speed, int index, Action<SpeedBtn> onClicked)
    {
        Speed = speed;
        Index = index;
        _onClicked = onClicked;

        if (_text != null)
        {
            _text.text = $"x{speed}";
        }
        if (_sigPanel != null)
        {
            _sigPanel.SetActive(false);
        }
    }

    public void SetSigPanelVisible(bool visible)
    {
        if (_sigPanel == null)
        {
            return;
        }

        _sigPanel.transform.DOKill();

        if (visible)
        {
            _sigPanel.SetActive(true);
            _sigPanel.transform.localScale = Vector3.zero;
            _sigPanel.transform.DOScale(Vector3.one, _sigPanelTweenDuration).SetEase(Ease.OutBack).SetUpdate(true).SetLink(_sigPanel);
        }
        else
        {
            _sigPanel.transform.DOScale(Vector3.zero, _sigPanelTweenDuration).SetEase(Ease.InBack).SetUpdate(true).SetLink(_sigPanel).OnComplete(() => _sigPanel.SetActive(false));
        }
    }

    /// <summary>
    /// 버튼 클릭 시 호출. (Inspector에서 Button onClick에 연결)
    /// </summary>
    public void OnClick()
    {
        _onClicked?.Invoke(this);
    }
}
