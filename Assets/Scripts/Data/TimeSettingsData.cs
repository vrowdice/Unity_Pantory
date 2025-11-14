using UnityEngine;

/// <summary>
/// ScriptableObject that stores time settings for balancing.
/// Allows time flow configuration through the Inspector.
/// </summary>
[CreateAssetMenu(fileName = "TimeSettingsData", menuName = "Game Data/Time Settings Data", order = 2)]
public class TimeSettingsData : ScriptableObject
{
    [Header("Time Flow Settings")]
    [Tooltip("Real seconds it takes for one in-game HOUR to pass")]
    [Range(0.01f, 10.0f)]
    public float realSecondsPerHour = 0.1f;

    [Header("Calendar Settings")]
    [Tooltip("Number of days in a month (business days)")]
    [Range(1, 31)]
    public int daysPerMonth = 20;
    
    [Tooltip("Number of months in a year")]
    [Range(1, 24)]
    public int monthsPerYear = 12;

    [Header("Starting Date")]
    [Tooltip("Starting year")]
    [Range(0, 999)]
    public int startYear = 0;
    
    [Tooltip("Starting month")]
    [Range(0, 23)]
    public int startMonth = 0;
    
    [Tooltip("Starting day")]
    [Range(0, 30)]
    public int startDay = 0;

    /// <summary>
    /// Applies time settings to TimeService.
    /// </summary>
    /// <param name="timeService">TimeService to apply to</param>
    public void ApplyToTimeService(TimeDataHandler timeService)
    {
        if (timeService == null)
        {
            Debug.LogError("[TimeSettingsData] TimeService is null.");
            return;
        }

        timeService.SetRealSecondsPerHour(realSecondsPerHour);
        timeService.SetDaysPerMonth(daysPerMonth);
        timeService.SetMonthsPerYear(monthsPerYear);
        timeService.SetDate(startYear, startMonth, startDay);

        Debug.Log($"[TimeSettingsData] Time settings applied: " +
                  $"RealSecondsPerHour={realSecondsPerHour}, " +
                  $"DaysPerMonth={daysPerMonth}, " +
                  $"MonthsPerYear={monthsPerYear}, " +
                  $"StartDate=Y{startYear} M{startMonth} D{startDay}");
    }

    /// <summary>
    /// Validates values in the Editor.
    /// </summary>
    private void OnValidate()
    {
        // Ensure positive values
        if (realSecondsPerHour <= 0) realSecondsPerHour = 0.01f;
        if (daysPerMonth < 1) daysPerMonth = 1;
        if (monthsPerYear < 1) monthsPerYear = 1;

        // Validate starting date ranges
        if (startYear < 0) startYear = 0;
        if (startMonth < 0) startMonth = 0;
        if (startDay < 0) startDay = 0;
        
        // Clamp starting month/day to valid ranges
        if (startMonth >= monthsPerYear) startMonth = monthsPerYear - 1;
        if (startDay >= daysPerMonth) startDay = daysPerMonth - 1;
    }
}

