using DG.Tweening;
using UnityEngine;

/// <summary>
/// 요구 조건 충족·완료 시 완료 버튼(또는 표시 대상)에 재생하는 SFX·스케일 펀치 피드백.
/// </summary>
public static class RequirementCompleteFeedbackUtils
{
    public const float ButtonPunchDuration = 0.45f;
    public const float ButtonPunchScale = 0.15f;
    public const int ButtonPunchVibrato = 8;

    /// <summary>
    /// false → true 전환 시에만 피드백을 재생합니다.
    /// </summary>
    public static void NotifyBecameReady(ref bool wasReady, bool isReady, Transform animationTarget, AudioClip sfx)
    {
        if (!isReady)
        {
            wasReady = false;
            return;
        }

        if (!wasReady)
            Play(animationTarget, sfx);

        wasReady = true;
    }

    public static void Play(Transform animationTarget, AudioClip sfx)
    {
        PlayButtonAnimation(animationTarget);

        if (sfx != null && SoundManager.Instance != null)
            SoundManager.Instance.PlaySFX(sfx);
    }

    public static void PlayButtonAnimation(Transform animationTarget)
    {
        if (animationTarget == null)
            return;

        animationTarget.DOKill();
        animationTarget
            .DOPunchScale(Vector3.one * ButtonPunchScale, ButtonPunchDuration, ButtonPunchVibrato, 0.5f)
            .SetUpdate(true)
            .SetLink(animationTarget.gameObject);
    }
}
