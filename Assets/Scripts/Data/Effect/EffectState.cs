/// <summary>
/// 실제 적용 중인 효과의 런타임 인스턴스입니다.
/// 남은 시간을 추적합니다.
/// </summary>
public class EffectState
{
    public EffectData Data { get; private set; }
    public float RemainingDays { get; set; } // 남은 지속 시간 (일 단위)
    public bool IsPermanent => Data.durationDays <= 0;

    public EffectState(EffectData data)
    {
        Data = data;
        RemainingDays = data.durationDays;
    }
}

