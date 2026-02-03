using System;

/// <summary>
/// 뉴스 인스턴스의 런타임 상태 (저장/표시용)
/// </summary>
[Serializable]
public class NewsState
{
    public string id;
    public int durationDays;
    public int remainingDays;

    public bool IsPermanent => durationDays <= 0;
    public bool IsExpired => remainingDays <= 0;

    public NewsState(NewsData data)
    {
        id = data.id;
        durationDays = data.durationDays;
        remainingDays = durationDays;
    }

    public bool ProcessDayPass(int date)
    {
        if (IsPermanent) return false;

        remainingDays -= date;
        if (remainingDays <= 0)
        {
            remainingDays = 0;
            return true;
        }
        return false;
    }
}
