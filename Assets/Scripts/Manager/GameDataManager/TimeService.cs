using System;
using UnityEngine;

/// <summary>
/// 게임 내 시간을 관리하는 서비스 클래스
/// 년, 월, 일을 관리하고 시간 흐름을 제어합니다.
/// </summary>
public class TimeService
{
    // ----------------- 시간 데이터 -----------------
    
    private int _year;
    private int _month;
    private int _day;

    public int Year => _year;
    public int Month => _month;
    public int Day => _day;

    // ----------------- 시간 흐름 설정 -----------------
    
    private bool _isTimePaused = false;
    private float _timeSpeed = 1.0f;
    
    // 하루가 지나가는 실제 시간 (초 단위)
    private float _realSecondsPerDay = 2.0f;
    
    // 현재 경과 시간
    private float _currentDayProgress = 0f;

    // ----------------- 게임 설정 -----------------
    
    // 한 달의 일 수 (영업일 기준)
    private int _daysPerMonth = 20;  // 영업일 기준 약 20일
    
    // 한 해의 월 수
    private int _monthsPerYear = 12;

    // ----------------- 이벤트 -----------------
    
    /// <summary>
    /// 하루가 지날 때 발생하는 이벤트
    /// </summary>
    public event Action OnDayChanged;
    
    /// <summary>
    /// 한 달이 지날 때 발생하는 이벤트
    /// </summary>
    public event Action OnMonthChanged;
    
    /// <summary>
    /// 한 해가 지날 때 발생하는 이벤트
    /// </summary>
    public event Action OnYearChanged;

    // ----------------- 생성자 -----------------

    /// <summary>
    /// TimeService 생성자
    /// 기본값: 0년 0월 0일부터 시작
    /// </summary>
    public TimeService()
    {
        _year = 0;
        _month = 0;
        _day = 0;
        _isTimePaused = false;
        _timeSpeed = 1.0f;
        
        Debug.Log("[TimeService] Initialized. Start date: Y0 M0 D0");
    }

    /// <summary>
    /// 시작 날짜를 지정하는 생성자
    /// </summary>
    public TimeService(int year, int month, int day)
    {
        _year = year;
        _month = month;
        _day = day;
        _isTimePaused = false;
        _timeSpeed = 1.0f;
        
        Debug.Log($"[TimeService] Initialized. Start date: Y{year} M{month} D{day}");
    }

    // ----------------- Public Methods -----------------

    /// <summary>
    /// 시간을 업데이트합니다 (매 프레임 호출).
    /// </summary>
    /// <param name="deltaTime">프레임 간 경과 시간</param>
    public void Update(float deltaTime)
    {
        // 시간이 정지 상태면 업데이트하지 않음
        if (_isTimePaused)
        {
            return;
        }

        // 시간 경과 (배속 적용)
        _currentDayProgress += (deltaTime / _realSecondsPerDay) * _timeSpeed;

        // 하루가 지났는지 확인
        if (_currentDayProgress >= 1.0f)
        {
            _currentDayProgress -= 1.0f;
            AdvanceDay();
        }
    }

    /// <summary>
    /// 시간을 정지합니다.
    /// </summary>
    public void PauseTime()
    {
        _isTimePaused = true;
        Debug.Log("[TimeService] Time paused.");
    }

    /// <summary>
    /// 시간을 재생합니다 (정상 속도).
    /// </summary>
    public void ResumeTime()
    {
        _isTimePaused = false;
        _timeSpeed = 1.0f;
        Debug.Log("[TimeService] Time resumed at normal speed (1x).");
    }

    /// <summary>
    /// 시간을 배속으로 재생합니다.
    /// </summary>
    /// <param name="speed">배속 (예: 2.0f = 2배속, 5.0f = 5배속)</param>
    public void SetTimeSpeed(float speed)
    {
        if (speed < 0)
        {
            Debug.LogWarning($"[TimeService] Time speed cannot be negative. Input: {speed}");
            return;
        }

        _isTimePaused = false;
        _timeSpeed = speed;
        Debug.Log($"[TimeService] Time speed set to {speed}x.");
    }

    /// <summary>
    /// 현재 시간 흐름 상태를 반환합니다.
    /// </summary>
    public bool IsTimePaused() => _isTimePaused;

    /// <summary>
    /// 현재 시간 배속을 반환합니다.
    /// </summary>
    public float GetTimeSpeed() => _timeSpeed;

    /// <summary>
    /// 하루의 진행도를 반환합니다 (0.0 ~ 1.0).
    /// </summary>
    public float GetDayProgress() => _currentDayProgress;

    /// <summary>
    /// 현재 날짜를 문자열로 반환합니다.
    /// </summary>
    public string GetDateString()
    {
        return $"Y{_year} M{_month} D{_day}";
    }

    /// <summary>
    /// 현재 날짜를 포맷팅된 문자열로 반환합니다.
    /// </summary>
    public string GetFormattedDateString()
    {
        return $"{_year:D4}년 {_month:D2}월 {_day:D2}일";
    }

    // ----------------- Private Methods -----------------

    /// <summary>
    /// 하루를 진행합니다.
    /// </summary>
    private void AdvanceDay()
    {
        _day++;

        // 한 달이 지났는지 확인
        if (_day >= _daysPerMonth)
        {
            _day = 0;
            AdvanceMonth();
        }

        OnDayChanged?.Invoke();
    }

    /// <summary>
    /// 한 달을 진행합니다.
    /// </summary>
    private void AdvanceMonth()
    {
        _month++;

        // 한 해가 지났는지 확인
        if (_month >= _monthsPerYear)
        {
            _month = 0;
            AdvanceYear();
        }

        Debug.Log($"[TimeService] Month changed: {GetDateString()}");
        OnMonthChanged?.Invoke();
    }

    /// <summary>
    /// 한 해를 진행합니다.
    /// </summary>
    private void AdvanceYear()
    {
        _year++;

        Debug.Log($"[TimeService] Year changed: {GetDateString()}");
        OnYearChanged?.Invoke();
    }

    /// <summary>
    /// 날짜를 직접 설정합니다 (치트 또는 세이브/로드용).
    /// </summary>
    public void SetDate(int year, int month, int day)
    {
        _year = year;
        _month = month;
        _day = day;

        Debug.Log($"[TimeService] Date set to: {GetDateString()}");
    }

    /// <summary>
    /// 하루가 지나는 실제 시간을 설정합니다 (초 단위).
    /// </summary>
    public void SetRealSecondsPerDay(float seconds)
    {
        if (seconds <= 0)
        {
            Debug.LogWarning($"[TimeService] Real seconds per day must be positive. Input: {seconds}");
            return;
        }

        _realSecondsPerDay = seconds;
        Debug.Log($"[TimeService] Real seconds per day set to {seconds}s.");
    }

    /// <summary>
    /// 현재 설정된 하루 당 실제 시간을 반환합니다.
    /// </summary>
    public float GetRealSecondsPerDay() => _realSecondsPerDay;

    /// <summary>
    /// 한 달의 일 수를 설정합니다.
    /// </summary>
    public void SetDaysPerMonth(int days)
    {
        if (days <= 0)
        {
            Debug.LogWarning($"[TimeService] Days per month must be positive. Input: {days}");
            return;
        }

        _daysPerMonth = days;
    }

    /// <summary>
    /// 한 해의 월 수를 설정합니다.
    /// </summary>
    public void SetMonthsPerYear(int months)
    {
        if (months <= 0)
        {
            Debug.LogWarning($"[TimeService] Months per year must be positive. Input: {months}");
            return;
        }

        _monthsPerYear = months;
    }

    /// <summary>
    /// 현재 한 달의 일 수를 반환합니다.
    /// </summary>
    public int GetDaysPerMonth() => _daysPerMonth;

    /// <summary>
    /// 현재 한 해의 월 수를 반환합니다.
    /// </summary>
    public int GetMonthsPerYear() => _monthsPerYear;
}

