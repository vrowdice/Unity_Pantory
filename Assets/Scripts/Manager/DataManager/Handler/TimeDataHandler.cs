using System;
using UnityEngine;

/// <summary>
/// 게임 내 시간을 관리하는 서비스 클래스
/// </summary>
public class TimeDataHandler : IDataHandlerEvents
{
    private const float MaxDeltaTimeSeconds = 0.1f;
    private const int MaxTimeTicksPerFrame = 48;

    private readonly DataManager _dataManager;
    private InitialTimeData _initialTimeData;

    public int Year { get; private set; }
    public int Month { get; private set; }
    public int Day { get; private set; }
    public int CurrentHour { get; private set; }

    public bool IsPaused { get; private set; } = false;
    public float TimeSpeed { get; private set; } = 1.0f;
    public float DayProgress { get; private set; } = 0f;

    private float _realSecondsPerDay = 2.0f;
    private int _daysPerMonth = 20;
    private int _monthsPerYear = 12;

    public event Action OnHourChanged;
    public event Action OnDayChanged;
    public event Action OnMonthChanged;
    public event Action OnYearChanged;

    public TimeDataHandler(DataManager gameDataManager, InitialTimeData initData)
    {
        _dataManager = gameDataManager;
        _initialTimeData = initData;

        SetRealSecondsPerHour(initData.realSecondsPerHour);
        SetDaysPerMonth(initData.daysPerMonth);
        SetMonthsPerYear(initData.monthsPerYear);
        SetDate(initData.startYear, initData.startMonth, initData.startDay);
    }

    public void Update(float deltaTime)
    {
        if (IsPaused) return;
        if (TimeSpeed <= 0f) return;

        deltaTime = Mathf.Min(deltaTime, MaxDeltaTimeSeconds);
        DayProgress += (deltaTime / _realSecondsPerDay) * TimeSpeed;

        int ticksRemaining = MaxTimeTicksPerFrame;
        while (ticksRemaining > 0 && TryAdvanceOneTimeTick())
            ticksRemaining--;
    }

    private bool TryAdvanceOneTimeTick()
    {
        int hoursPerDay = _initialTimeData.hoursPerDay;

        if (DayProgress >= 1f)
        {
            if (CurrentHour < hoursPerDay - 1)
            {
                AdvanceOneHour();
                return true;
            }

            DayProgress -= 1f;
            AdvanceOneHour();
            return true;
        }

        int targetHour = GetHourFromProgress(DayProgress);
        if (CurrentHour == targetHour)
            return false;

        AdvanceOneHour();
        return true;
    }

    private int GetHourFromProgress(float progress)
    {
        return Mathf.Clamp(
            Mathf.FloorToInt(progress * _initialTimeData.hoursPerDay),
            0,
            _initialTimeData.hoursPerDay - 1);
    }

    private void AdvanceOneHour()
    {
        int hoursPerDay = _initialTimeData.hoursPerDay;
        CurrentHour++;
        if (CurrentHour >= hoursPerDay)
        {
            CurrentHour = 0;
            AdvanceDayAfterHourRollover();
        }

        OnHourChanged?.Invoke();
    }

    private void AdvanceDayAfterHourRollover()
    {
        Day++;
        if (Day >= _daysPerMonth)
        {
            Day = 0;
            AdvanceMonth();
        }

        OnDayChanged?.Invoke();
    }

    private void AdvanceMonth()
    {
        Month++;
        if (Month >= _monthsPerYear)
        {
            Month = 0;
            AdvanceYear();
        }

        // 구독처: DataManager — 월말 재정 처리 및 SaveLoadManager 오토세이브 등
        OnMonthChanged?.Invoke();
    }

    private void AdvanceYear()
    {
        Year++;
        OnYearChanged?.Invoke();
    }

    public void PauseTime() => IsPaused = true;
    public bool IsTimePaused() => IsPaused;

    public void ResumeTime()
    {
        IsPaused = false;
        TimeSpeed = 1.0f;
    }

    public void SetTimeSpeed(float speed)
    {
        if (speed < 0) return;
        IsPaused = false;
        TimeSpeed = speed;
    }

    public void SetDate(int year, int month, int day)
    {
        Year = year;
        Month = month;
        Day = day;
        CurrentHour = 0;
        OnHourChanged?.Invoke();
    }

    public void SetRealSecondsPerDay(float seconds)
    {
        if (seconds > 0) _realSecondsPerDay = seconds;
    }

    public void SetRealSecondsPerHour(float seconds)
    {
        if (seconds > 0) _realSecondsPerDay = seconds * _initialTimeData.hoursPerDay;
    }

    public void SetDaysPerMonth(int days)
    {
        if (days > 0) _daysPerMonth = days;
    }

    public void SetMonthsPerYear(int months)
    {
        if (months > 0) _monthsPerYear = months;
    }

    public void AdvanceOneDay()
    {
        DayProgress = 0f;
        CurrentHour = 0;
        AdvanceDayAfterHourRollover();
    }

    public void AdvanceOneMonth()
    {
        int daysRemaining = _daysPerMonth - Day;
        for (int i = 0; i < daysRemaining; i++)
        {
            AdvanceOneDay();
        }
    }

    public void AdvanceOneYear()
    {
        for (int i = 0; i < _monthsPerYear; i++)
        {
            AdvanceOneMonth();
        }
    }

    /// <summary>
    /// 모든 이벤트 구독을 초기화합니다.
    /// </summary>
    public void ClearAllSubscriptions()
    {
        OnHourChanged = null;
        OnDayChanged = null;
        OnMonthChanged = null;
        OnYearChanged = null;
    }
}
