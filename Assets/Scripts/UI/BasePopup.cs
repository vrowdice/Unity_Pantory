using DG.Tweening;
using System;
using System.Collections;
using UnityEngine;

public class BasePopup : MonoBehaviour
{
    public const float DISPLAY_TIME = 0.6f;
    public const float MOVE_DURATION = 0.5f;
    public const float PUNCH_SCALE_DURATION = 1f;
    public const float INOUT_DURATION = 0.5f;

    private RectTransform _rectTransform = null;
    private Canvas _canvas = null;

    [SerializeField] private BasePanelEffectType.TYPE _effectType = BasePanelEffectType.TYPE.ZoomScale;
    [SerializeField] private bool _enabled = true;
    protected Coroutine _showCoroutine = null;

    private Vector3? _originalScale = null;

    private Action _onShowCompleteCallback = null;

    public virtual void Init()
    {
        _originalScale ??= transform.localScale;
        _rectTransform = GetComponent<RectTransform>();
        _canvas = FindAnyObjectByType<Canvas>();
        gameObject.SetActive(false);
    }
    public virtual void Init(Vector3 argPosition)
    {
        transform.position = argPosition;
        _originalScale ??= transform.localScale;
        _rectTransform = GetComponent<RectTransform>();
        _canvas = FindAnyObjectByType<Canvas>();
        gameObject.SetActive(false);
    }
    public virtual void Init(Vector3 argPosition, BasePanelEffectType.TYPE argType)
    {
        Init(argPosition);
        _effectType = argType;
    }
    public virtual void Show()
    {
        if (!_enabled)
        {
            return;
        }

        gameObject.SetActive(true);
        if (_showCoroutine != null)
        {
            StopCoroutine(_showCoroutine);
        }
        _showCoroutine = StartCoroutine(ShowEffectCoroutine());
    }
    public virtual Coroutine ShowCoroutine()
    {
        gameObject.SetActive(true);
        if (_showCoroutine != null)
        {
            StopCoroutine(_showCoroutine);
        }
        _showCoroutine = StartCoroutine(ShowEffectCoroutine());
        return _showCoroutine;
    }

    public virtual IEnumerator ShowEffectCoroutine()
    {
        switch(_effectType)
        {
            case BasePanelEffectType.TYPE.ZoomScale:
                transform.localScale = Vector3.zero;
                yield return transform.DOScale(_originalScale.Value, 0.5f).SetEase(Ease.OutBack).SetUpdate(true).WaitForCompletion();
                break;
            case BasePanelEffectType.TYPE.L2R:
                float canvasWidth = _canvas.GetComponent<RectTransform>().sizeDelta.x;
                float halfUIWidth = _rectTransform.sizeDelta.x / 2;

                float startX = -(canvasWidth / 2) - halfUIWidth;
                float midX = 0;
                float endX = (canvasWidth / 2) + halfUIWidth;

                _rectTransform.anchoredPosition = new Vector2(startX, _rectTransform.anchoredPosition.y);

                Sequence sequence = DOTween.Sequence();

                sequence.Append(_rectTransform.DOAnchorPosX(midX, MOVE_DURATION).SetEase(Ease.OutQuad)
                .OnStart(() =>
                {
                    
                }));
                sequence.AppendInterval(DISPLAY_TIME);
                sequence.Append(_rectTransform.DOAnchorPosX(endX, MOVE_DURATION).SetEase(Ease.InQuad)
                .OnStart(() =>
                {
                    
                }));
                yield return sequence.WaitForCompletion();
                break;
            case BasePanelEffectType.TYPE.PunchScale:
                transform.localScale = new Vector3(0f,0f,1f);
                Sequence seq = DOTween.Sequence();
                seq.Append(transform.DOScale(_originalScale.Value, INOUT_DURATION).SetEase(Ease.OutCirc));
                seq.Append(transform.DOPunchScale( _originalScale.Value * 0.3f, PUNCH_SCALE_DURATION, 4 , 0.2f)).SetEase(Ease.OutQuad);
                seq.Append(transform.DOScale(0f, INOUT_DURATION).SetEase(Ease.OutQuad));
                yield return seq.WaitForCompletion();
                break;
        }
        _onShowCompleteCallback?.Invoke();
        yield break;
    }

    public virtual void Close()
    {
        if (_showCoroutine != null)
        {
            StopCoroutine(_showCoroutine);
            _showCoroutine = null;
        }

        gameObject.SetActive(false);
    }

    public void SubscribeShowCompleteCallback(Action argCallback)
    {
        _onShowCompleteCallback += argCallback;
    }
    public void UnsubscribeShowCompleteCallback(Action argCallback)
    {
        _onShowCompleteCallback -= argCallback;
    }
    public void ClearShowCompleteCallback()
    {
        _onShowCompleteCallback = null;
    }
}
