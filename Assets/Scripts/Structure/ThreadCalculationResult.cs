using System.Collections.Generic;

/// <summary>
/// 건물 계산 결과를 담는 데이터 구조체입니다.
/// </summary>
public class ThreadCalculationResult
{
    public int TotalBuildCost { get; set; }
    public int TotalMaintenanceCost { get; set; }
    public int TotalRequiredEmployees { get; set; }
    public int RequiredTechnicians { get; set; }
    public Dictionary<string, int> InputResourceCounts { get; } = new Dictionary<string, int>();
    public Dictionary<string, int> OutputResourceCounts { get; } = new Dictionary<string, int>();
}
