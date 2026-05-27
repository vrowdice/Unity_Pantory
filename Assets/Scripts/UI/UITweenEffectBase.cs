using DG.Tweening;
using System;
using System.Collections;
using UnityEngine;

public abstract class UITweenEffectBase : MonoBehaviour
{
    [SerializeField] private RectTransform _animationTarget;
    [SerializeField] private BasePanelEffectType.TYPE _effectType = BasePanelEffectType.TYPE.ScaleInScaleOut;

    public const float INOUT_DURATION = 0.1f;

    private CanvasGroup _canvasGroup;
    private Vector3? _originalScale;

    protected BasePanelEffectType.TYPE EffectType => _effectType;

    protected virtual bool PlayEffectOnEnable => false;

    protected virtual void OnEnable()
    {
        if (PlayEffectOnEnable)
            PlayShowEffect();
    }

    protected virtual void OnDisable()
    {
        KillActiveTweens();
    }

    protected virtual void OnDestroy()
    {
        KillActiveTweens();
    }

    protected Transform GetAnimationTarget()
    {
        return _animationTarget != null ? _animationTarget : transform;
    }

    protected void CacheOriginalScale()
    {
        Transform target = GetAnimationTarget();
        _originalScale ??= target.localScale;
    }

    protected Vector3 GetOriginalScale()
    {
        CacheOriginalScale();
        return _originalScale.Value;
    }

    protected void EnsureCanvasGroup()
    {
        if (_effectType != BasePanelEffectType.TYPE.FadeInFadeOut)
            return;

        Transform target = GetAnimationTarget();
        _canvasGroup = target.GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            _canvasGroup = target.gameObject.AddComponent<CanvasGroup>();
    }

    protected bool HasShowEffect()
    {
        return _effectType != BasePanelEffectType.TYPE.None;
    }

    protected bool UsesAnimatedClose()
    {
        return _effectType == BasePanelEffectType.TYPE.FadeInFadeOut
            || _effectType == BasePanelEffectType.TYPE.ScaleInScaleOut;
    }

    protected Tween ConfigureTween(Tween tween)
    {
        return tween.SetUpdate(true).SetLink(GetAnimationTarget().gameObject);
    }

    protected void KillActiveTweens()
    {
        Transform target = GetAnimationTarget();
        target.DOKill();

        if (_canvasGroup != null)
            _canvasGroup.DOKill();
    }

    protected void PlayShowEffect()
    {
        if (!HasShowEffect())
            return;

        KillActiveTweens();
        CacheOriginalScale();
        EnsureCanvasGroup();
        StartShowTween();
    }

    protected IEnumerator PlayShowEffectCoroutine()
    {
        if (!HasShowEffect())
            yield break;

        KillActiveTweens();
        CacheOriginalScale();
        EnsureCanvasGroup();

        yield return WaitForShowTween();
    }

    protected IEnumerator PlayCloseEffectCoroutine()
    {
        if (!UsesAnimatedClose())
            yield break;

        KillActiveTweens();
        yield return WaitForCloseTween();
    }

    private void StartShowTween()
    {
        Transform target = GetAnimationTarget();

        switch (_effectType)
        {
            case BasePanelEffectType.TYPE.FadeInFadeOut:
                if (_canvasGroup != null)
                {
                    _canvasGroup.alpha = 0f;
                    ConfigureTween(_canvasGroup.DOFade(1f, INOUT_DURATION));
                }
                break;

            case BasePanelEffectType.TYPE.ScaleInScaleOut:
                Vector3 originalScale = GetOriginalScale();
                target.localScale = Vector3.zero;
                ConfigureTween(target.DOScale(originalScale, INOUT_DURATION).SetEase(Ease.OutBack));
                break;
        }
    }

    private IEnumerator WaitForShowTween()
    {
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
        }
    }

    private IEnumerator WaitForCloseTween()
    {
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
        }
    }
}
