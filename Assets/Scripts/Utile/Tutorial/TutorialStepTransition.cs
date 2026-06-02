using System;
using DG.Tweening;
using TMPro;
using UnityEngine;

public static class TutorialStepTransition
{
    public static void Play(
        TextMeshProUGUI descriptionText,
        TextMeshProUGUI indexText,
        Action onContentSwap,
        float duration = 0.25f,
        Ease ease = Ease.OutCubic)
    {
        Kill(descriptionText, indexText);

        float halfDuration = duration * 0.5f;
        Sequence sequence = DOTween.Sequence();

        if (descriptionText != null)
        {
            sequence.Join(descriptionText.DOFade(0f, halfDuration).SetEase(ease));
            sequence.SetLink(descriptionText.gameObject);
        }

        if (indexText != null)
            sequence.Join(indexText.DOFade(0f, halfDuration).SetEase(ease));

        sequence.AppendCallback(() => onContentSwap?.Invoke());

        if (descriptionText != null)
            sequence.Append(descriptionText.DOFade(1f, halfDuration).SetEase(ease));

        if (indexText != null)
            sequence.Join(indexText.DOFade(1f, halfDuration).SetEase(ease));
    }

    public static void EnsureVisible(TextMeshProUGUI descriptionText, TextMeshProUGUI indexText = null)
    {
        SetAlpha(descriptionText, 1f);
        SetAlpha(indexText, 1f);
    }

    public static void Kill(TextMeshProUGUI descriptionText, TextMeshProUGUI indexText = null)
    {
        if (descriptionText != null)
            descriptionText.DOKill();

        if (indexText != null)
            indexText.DOKill();

        EnsureVisible(descriptionText, indexText);
    }

    private static void SetAlpha(TextMeshProUGUI text, float alpha)
    {
        if (text == null)
            return;

        Color color = text.color;
        color.a = alpha;
        text.color = color;
    }
}
