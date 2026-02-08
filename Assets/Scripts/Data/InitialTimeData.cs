using UnityEngine;

/// <summary>
/// 밸런싱을 위한 시간 설정을 저장하는 ScriptableObject
/// Inspector를 통해 시간 흐름 구성을 조정할 수 있습니다.
/// </summary>
[CreateAssetMenu(fileName = "TimeSettingsData", menuName = "Init Game Data/Time Settings Data")]
public class InitialTimeData : ScriptableObject
{
    [Range(1, 48)]
    [Tooltip("하루당 시간 수")]
    public int hoursPerDay = 24;
    [Range(0.01f, 1.0f)]
    [Tooltip("시간당 실제 초 (게임 시간 속도 조절)")]
    public float realSecondsPerHour = 0.1f;
    [Range(1, 31)]
    [Tooltip("한 달당 일수")]
    public int daysPerMonth = 20;
    [Range(1, 24)]
    [Tooltip("한 해당 월수")]
    public int monthsPerYear = 12;
    [Range(0, 999)]
    [Tooltip("시작 연도")]
    public int startYear = 0;
    [Range(0, 23)]
    [Tooltip("시작 월 (0부터 시작)")]
    public int startMonth = 0;
    [Range(0, 30)]
    [Tooltip("시작 일 (0부터 시작)")]
    public int startDay = 0;

    /// <summary>
    /// Editor에서 값 검증
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

