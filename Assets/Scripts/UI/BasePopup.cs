using DG.Tweening;
using System;
using System.Collections;
using UnityEngine;

public class BasePopup : MonoBehaviour
{
    public const float DISPLAY_TIME = 0.6f;
    public const float MOVE_DURATION = 0.5f;
    public const float PUNCH_SCALE_DURATION = 1f;
    public const float INOUT_DURATION = 0.1f;

    private RectTransform _rectTransform = null;
    private Canvas _canvas = null;
    private CanvasGroup _canvasGroup = null;

    [SerializeField] private BasePanelEffectType.TYPE _effectType = BasePanelEffectType.TYPE.FadeInFadeOut;
    [SerializeField] private bool _enabled = true;
    protected Coroutine _showCoroutine = null;
    protected Coroutine _closeCoroutine = null;

    private Vector3? _originalScale = null;

    private Action _onShowCompleteCallback = null;
    private Action _cachedClose;

    private void OnDestroy()
    {
        if (_canvasGroup != null)
            _canvasGroup.DOKill();
    }

    public virtual void Init()
    {
        _originalScale ??= transform.localScale;
        _rectTransform = GetComponent<RectTransform>();
        _canvas = FindAnyObjectByType<Canvas>();
        if (_canvasGroup == null)
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        gameObject.SetActive(false);
    }
    public virtual void Init(Vector3 argPosition)
    {
        transform.position = argPosition;
        _originalScale ??= transform.localScale;
        _rectTransform = GetComponent<RectTransform>();
        _canvas = FindAnyObjectByType<Canvas>();
        if (_canvasGroup == null)
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
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

        if (_cachedClose == null) _cachedClose = Close;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.PushCloseable(_cachedClose);
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
        switch (_effectType)
        {
            case BasePanelEffectType.TYPE.FadeInFadeOut:
                if (_canvasGroup != null)
                {
                    _canvasGroup.alpha = 0f;
                    Tween fadeTween = _canvasGroup.DOFade(1f, INOUT_DURATION).SetUpdate(true).SetLink(gameObject);
                    yield return fadeTween.WaitForCompletion();
                }
                break;
            default:
                break;
        }
        _onShowCompleteCallback?.Invoke();
    }

    public virtual void Close()
    {
        if (_cachedClose != null && GameManager.Instance != null)
            GameManager.Instance.RemoveCloseable(_cachedClose);

        if (_showCoroutine != null)
        {
            StopCoroutine(_showCoroutine);
            _showCoroutine = null;
        }

        if (_effectType == BasePanelEffectType.TYPE.FadeInFadeOut && gameObject.activeInHierarchy)
        {
            if (_closeCoroutine != null)
                StopCoroutine(_closeCoroutine);
            _closeCoroutine = StartCoroutine(CloseEffectCoroutine());
            return;
        }

        gameObject.SetActive(false);
    }

    protected virtual IEnumerator CloseEffectCoroutine()
    {
        if (_canvasGroup != null)
        {
            Tween fadeTween = _canvasGroup.DOFade(0f, INOUT_DURATION).SetUpdate(true).SetLink(gameObject);
            yield return fadeTween.WaitForCompletion();
        }

        _closeCoroutine = null;
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
