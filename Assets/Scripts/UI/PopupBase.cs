using DG.Tweening;
using System;
using System.Collections;
using UnityEngine;

public class PopupBase : TutorialBase
{
    [SerializeField] private RectTransform _animationTarget;
    [SerializeField] private BasePanelEffectType.TYPE _effectType = BasePanelEffectType.TYPE.FadeInFadeOut;

    [Header("Sound")]
    [SerializeField] private AudioClip _openPopupSfx;

    public const float DISPLAY_TIME = 0.6f;
    public const float MOVE_DURATION = 0.5f;
    public const float PUNCH_SCALE_DURATION = 1f;
    public const float INOUT_DURATION = 0.1f;

    private CanvasGroup _canvasGroup = null;
    protected Coroutine _showCoroutine = null;
    protected Coroutine _closeCoroutine = null;

    private Vector3? _originalScale = null;

    private Action _onShowCompleteCallback = null;
    private Action _cachedClose;

    protected DataManager _dataManager;
    private bool _isDayEventSubscribed;
    private bool _isHourEventSubscribed;
    private bool _isClosing;
    private bool _destroyAfterClose;

    private void OnDisable()
    {
        UnsubscribeDayEvents();
        UnsubscribeHourEvents();

        if (_cachedClose != null && UIManager.Instance != null)
        {
            UIManager.Instance.RemoveCloseable(_cachedClose);
        }
    }

    private void OnDestroy()
    {
        Transform target = GetAnimationTarget();
        if (target != null)
        {
            target.DOKill();
        }

        if (_canvasGroup != null)
        {
            _canvasGroup.DOKill();
        }
    }

    private Transform GetAnimationTarget()
    {
        return _animationTarget != null ? _animationTarget : transform;
    }

    private Vector3 GetOriginalScale()
    {
        Transform target = GetAnimationTarget();
        _originalScale ??= target.localScale;
        return _originalScale.Value;
    }

    private bool UsesAnimatedClose()
    {
        return _effectType == BasePanelEffectType.TYPE.FadeInFadeOut
            || _effectType == BasePanelEffectType.TYPE.ScaleInScaleOut;
    }

    private Tween ConfigureTween(Tween tween)
    {
        return tween.SetUpdate(true).SetLink(GetAnimationTarget().gameObject);
    }

    private void KillActiveTweens()
    {
        Transform target = GetAnimationTarget();
        target.DOKill();

        if (_canvasGroup != null)
        {
            _canvasGroup.DOKill();
        }
    }

    public virtual void Init()
    {
        GameObjectUtils.ApplyPrefabInstanceName(gameObject);

        Transform target = GetAnimationTarget();
        _originalScale ??= target.localScale;
        _canvasGroup = target.GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
        {
            _canvasGroup = target.gameObject.AddComponent<CanvasGroup>();
        }

        SetDataManager(DataManager.Instance);
        SubscribeDayEvents();

        gameObject.SetActive(true);
    }

    protected void SetDataManager(DataManager dataManager)
    {
        _dataManager = dataManager ?? DataManager.Instance;
    }

    protected void SubscribeDayEvents()
    {
        if (_isDayEventSubscribed || _dataManager?.Time == null)
        {
            return;
        }

        _dataManager.Time.OnDayChanged -= HandleDayChanged;
        _dataManager.Time.OnDayChanged += HandleDayChanged;
        _isDayEventSubscribed = true;
    }

    protected void UnsubscribeDayEvents()
    {
        if (!_isDayEventSubscribed || _dataManager?.Time == null)
        {
            return;
        }

        _dataManager.Time.OnDayChanged -= HandleDayChanged;
        _isDayEventSubscribed = false;
    }

    protected void SubscribeHourEvents()
    {
        if (_isHourEventSubscribed || _dataManager?.Time == null)
        {
            return;
        }

        _dataManager.Time.OnHourChanged -= HandleHourChanged;
        _dataManager.Time.OnHourChanged += HandleHourChanged;
        _isHourEventSubscribed = true;
    }

    protected void UnsubscribeHourEvents()
    {
        if (!_isHourEventSubscribed || _dataManager?.Time == null)
        {
            return;
        }

        _dataManager.Time.OnHourChanged -= HandleHourChanged;
        _isHourEventSubscribed = false;
    }

    protected virtual void HandleDayChanged()
    {
    }

    protected virtual void HandleHourChanged()
    {
    }

    public virtual void Show()
    {
        _isClosing = false;
        if (_cachedClose == null) _cachedClose = Close;
        if (UIManager.Instance != null)
        {
            UIManager.Instance.RemoveCloseable(_cachedClose);
            UIManager.Instance.PushCloseable(_cachedClose);
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
        if (_openPopupSfx != null && SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX(_openPopupSfx);
        }

        KillActiveTweens();
        Transform target = GetAnimationTarget();

        switch (_effectType)
        {
            case BasePanelEffectType.TYPE.FadeInFadeOut:
                if (_canvasGroup != null)
                {
                    _canvasGroup.alpha = 0f;
                    Tween fadeTween = ConfigureTween(_canvasGroup.DOFade(1f, INOUT_DURATION));
                    yield return fadeTween.WaitForCompletion();
                }
                break;

            case BasePanelEffectType.TYPE.ScaleInScaleOut:
                Vector3 originalScale = GetOriginalScale();
                target.localScale = Vector3.zero;
                Tween scaleTween = ConfigureTween(target.DOScale(originalScale, INOUT_DURATION).SetEase(Ease.OutBack));
                yield return scaleTween.WaitForCompletion();
                break;

            default:
                break;
        }
        _onShowCompleteCallback?.Invoke();
    }

    public virtual void Close()
    {
        RequestClose(false);
    }

    protected void CloseAndDestroy()
    {
        RequestClose(true);
    }

    private void RequestClose(bool destroyAfterClose)
    {
        if (this == null) return;
        if (_isClosing) return;

        if (_cachedClose != null && UIManager.Instance != null)
            UIManager.Instance.RemoveCloseable(_cachedClose);

        if (_showCoroutine != null)
        {
            if (gameObject != null && gameObject.activeInHierarchy)
            {
                StopCoroutine(_showCoroutine);
            }
            _showCoroutine = null;
        }

        if (UsesAnimatedClose() && gameObject.activeInHierarchy)
        {
            _isClosing = true;
            _destroyAfterClose = destroyAfterClose;

            if (_closeCoroutine != null)
                StopCoroutine(_closeCoroutine);
            _closeCoroutine = StartCoroutine(CloseEffectCoroutine());
            return;
        }

        FinalizeClose(destroyAfterClose);
    }

    private void FinalizeClose(bool destroyAfterClose)
    {
        _isClosing = false;
        _closeCoroutine = null;
        gameObject.SetActive(false);

        if (destroyAfterClose)
            Destroy(gameObject);
    }

    protected virtual IEnumerator CloseEffectCoroutine()
    {
        KillActiveTweens();
        Transform target = GetAnimationTarget();

        switch (_effectType)
        {
            case BasePanelEffectType.TYPE.FadeInFadeOut:
                if (_canvasGroup != null)
                {
                    Tween fadeTween = ConfigureTween(_canvasGroup.DOFade(0f, INOUT_DURATION));
                    yield return fadeTween.WaitForCompletion();
                }
                break;

            case BasePanelEffectType.TYPE.ScaleInScaleOut:
                Tween scaleTween = ConfigureTween(target.DOScale(Vector3.zero, INOUT_DURATION).SetEase(Ease.InBack));
                yield return scaleTween.WaitForCompletion();
                break;

            default:
                break;
        }

        _closeCoroutine = null;
        FinalizeClose(_destroyAfterClose);
    }
}
