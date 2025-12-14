using UnityEngine;

/// <summary>
/// ScriptableObject that stores time settings for balancing.
/// Allows time flow configuration through the Inspector.
/// </summary>
[CreateAssetMenu(fileName = "TimeSettingsData", menuName = "Game Data/Time Settings Data", order = 2)]
public class InitialTimeData : ScriptableObject
{
    [Range(1, 48)]
    public int hoursPerDay = 24;
    [Range(0.01f, 1.0f)]
    public float realSecondsPerHour = 0.1f;
    [Range(1, 31)]
    public int daysPerMonth = 20;
    [Range(1, 24)]
    public int monthsPerYear = 12;
    [Range(0, 999)]
    public int startYear = 0;
    [Range(0, 23)]
    public int startMonth = 0;
    [Range(0, 30)]
    public int startDay = 0;

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

