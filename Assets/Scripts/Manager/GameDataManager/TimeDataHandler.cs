using System;
using UnityEngine;

/// <summary>
/// 게임 내 시간을 관리하는 서비스 클래스
/// </summary>
public class TimeDataHandler
{
    private readonly GameDataManager _gameDataManager;
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

    public TimeDataHandler(GameDataManager gameDataManager, InitialTimeData initData)
    {
        _gameDataManager = gameDataManager;
        _initialTimeData = initData;

        SetRealSecondsPerHour(initData.realSecondsPerHour);
        SetDaysPerMonth(initData.daysPerMonth);
        SetMonthsPerYear(initData.monthsPerYear);
        SetDate(initData.startYear, initData.startMonth, initData.startDay);
    }

    public void Update(float deltaTime)
    {
        if (IsPaused) return;

        DayProgress += (deltaTime / _realSecondsPerDay) * TimeSpeed;
        UpdateHourLogic();

        if (DayProgress >= 1.0f)
        {
            DayProgress -= 1.0f;
            AdvanceDay();
        }
    }

    private void UpdateHourLogic(bool forceNotify = false)
    {
        int newHour = Mathf.Clamp(Mathf.FloorToInt(DayProgress * _initialTimeData.hoursPerDay), 0, _initialTimeData.hoursPerDay - 1);

        if (newHour != CurrentHour || forceNotify)
        {
            CurrentHour = newHour;
            OnHourChanged?.Invoke();
        }
    }

    private void AdvanceDay()
    {
        Day++;
        CurrentHour = 0;
        UpdateHourLogic(true);

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
        Year = year; Month = month; Day = day;
        CurrentHour = 0;
        UpdateHourLogic(true);
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
}