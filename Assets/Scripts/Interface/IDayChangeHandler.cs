/// <summary>
/// 일자 변경 시 호출되는 핸들러. DataManager.HandleDayChanged에서 일괄 호출.
/// </summary>
public interface IDayChangeHandler
{
    void HandleDayChanged();
}
