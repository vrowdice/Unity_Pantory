using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 적자(파산) 상태일 때 남은 개월 수를 원형 슬라이더로 표시합니다.
/// </summary>
public class GameOverTimer : MonoBehaviour
{
    private const float ShowDuration = 0.45f;
    private const float TickDuration = 0.35f;
    private const float NeedleAngleOffset = 180f;

    [Header("Breathing")]
    [SerializeField] private float _breathingScaleMultiplier = 1.06f;
    [SerializeField] private float _breathingHalfDuration = 1.1f;

    [SerializeField] private Slider _slider;
    [SerializeField] private TextMeshProUGUI _countdownText;
    [SerializeField] private RectTransform _needle;

    private bool _isShowing;
    private int _lastMonthsRemaining = -1;
    private float _currentNeedleAngle;
    private float _currentFillRatio;
    private Vector3 _defaultScale;
    private Sequence _activeSequence;
    private Tween _breathingTween;

    private void Awake()
    {
        _defaultScale = transform.localScale;
    }

    public void ApplyCountdown(int monthsRemaining, int graceMonths, bool playShowAnimation = true)
    {
        if (graceMonths <= 0)
        {
            return;
        }

        float fillRatio = Mathf.Clamp01((float)monthsRemaining / graceMonths);
        float targetAngle = GetNeedleAngle(fillRatio);

        if (!_isShowing)
        {
            if (playShowAnimation)
            {
                PlayShowAnimation(monthsRemaining, fillRatio, targetAngle);
            }
            else
            {
                ShowImmediate(monthsRemaining, fillRatio, targetAngle);
            }

            return;
        }

        if (_lastMonthsRemaining == monthsRemaining)
        {
            ApplyImmediate(monthsRemaining, fillRatio, targetAngle);
            return;
        }

        PlayMonthTickAnimation(monthsRemaining, fillRatio, targetAngle);
    }

    public void Hide()
    {
        KillAllTween();

        if (_needle != null)
        {
            _needle.gameObject.SetActive(false);
        }

        transform.localScale = _defaultScale;
        gameObject.SetActive(false);
        _isShowing = false;
        _lastMonthsRemaining = -1;
    }

    private void OnDestroy()
    {
        KillAllTween();
    }

    private void OnDisable()
    {
        KillAllTween();
    }

    private float GetNeedleAngle(float fillRatio)
    {
        return NeedleAngleOffset - 360f * fillRatio;
    }

    private void ShowImmediate(int monthsRemaining, float fillRatio, float targetAngle)
    {
        KillActiveTween();
        gameObject.SetActive(true);
        transform.localScale = _defaultScale;

        if (_needle != null)
        {
            _needle.gameObject.SetActive(true);
        }

        ApplyImmediate(monthsRemaining, fillRatio, targetAngle);
        _isShowing = true;
        StartBreathing();
    }

    private void PlayShowAnimation(int monthsRemaining, float fillRatio, float targetAngle)
    {
        KillActiveTween();
        StopBreathing();
        gameObject.SetActive(true);
        transform.localScale = Vector3.zero;

        if (_needle != null)
        {
            _needle.gameObject.SetActive(true);
        }

        UpdateCountdownText(monthsRemaining);
        SetSliderValue(fillRatio);
        SetNeedleAngle(targetAngle);

        _activeSequence = DOTween.Sequence();
        _activeSequence.Append(
            transform.DOScale(_defaultScale, ShowDuration)
                .SetEase(Ease.OutBack));
        _activeSequence.OnComplete(StartBreathing);
        _activeSequence.SetLink(gameObject);

        _isShowing = true;
        _lastMonthsRemaining = monthsRemaining;
        _currentFillRatio = fillRatio;
        _currentNeedleAngle = targetAngle;
    }

    private void PlayMonthTickAnimation(int monthsRemaining, float fillRatio, float targetAngle)
    {
        KillActiveTween();
        StopBreathing();
        transform.localScale = _defaultScale;
        UpdateCountdownText(monthsRemaining);

        _activeSequence = DOTween.Sequence();

        if (_slider != null)
        {
            _activeSequence.Join(
                DOTween.To(
                    () => _currentFillRatio,
                    value =>
                    {
                        _currentFillRatio = value;
                        SetSliderValue(value);
                    },
                    fillRatio,
                    TickDuration)
                    .SetEase(Ease.OutCubic));
        }

        if (_needle != null)
        {
            _activeSequence.Join(
                DOTween.To(
                    () => _currentNeedleAngle,
                    value =>
                    {
                        _currentNeedleAngle = value;
                        SetNeedleAngle(value);
                    },
                    targetAngle,
                    TickDuration)
                    .SetEase(Ease.OutCubic));
        }

        _activeSequence.Join(
            transform.DOPunchScale(Vector3.one * 0.12f, TickDuration, 6, 0.6f));

        _activeSequence.OnComplete(StartBreathing);
        _activeSequence.SetLink(gameObject);

        _lastMonthsRemaining = monthsRemaining;
    }

    private void ApplyImmediate(int monthsRemaining, float fillRatio, float targetAngle)
    {
        UpdateCountdownText(monthsRemaining);
        SetSliderValue(fillRatio);
        SetNeedleAngle(targetAngle);

        _lastMonthsRemaining = monthsRemaining;
        _currentFillRatio = fillRatio;
        _currentNeedleAngle = targetAngle;
    }

    private void StartBreathing()
    {
        StopBreathing();
        transform.localScale = _defaultScale;

        Vector3 expandedScale = _defaultScale * _breathingScaleMultiplier;
        _breathingTween = transform
            .DOScale(expandedScale, _breathingHalfDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetLink(gameObject);
    }

    private void StopBreathing()
    {
        if (_breathingTween != null && _breathingTween.IsActive())
        {
            _breathingTween.Kill();
        }

        _breathingTween = null;
    }

    private void UpdateCountdownText(int monthsRemaining)
    {
        if (_countdownText != null)
        {
            _countdownText.text = monthsRemaining.ToString();
        }
    }

    private void SetSliderValue(float fillRatio)
    {
        if (_slider == null)
        {
            return;
        }

        _slider.minValue = 0f;
        _slider.maxValue = 1f;
        _slider.value = fillRatio;
    }

    private void SetNeedleAngle(float angleZ)
    {
        if (_needle == null)
        {
            return;
        }

        _needle.localRotation = Quaternion.Euler(0f, 0f, angleZ);
    }

    private void KillActiveTween()
    {
        if (_activeSequence != null && _activeSequence.IsActive())
        {
            _activeSequence.Kill();
        }

        _activeSequence = null;
    }

    private void KillAllTween()
    {
        KillActiveTween();
        StopBreathing();
    }
}
