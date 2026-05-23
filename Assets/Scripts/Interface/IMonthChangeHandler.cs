/// <summary>
/// 월 변경 시 호출되는 핸들러. DataManager.HandleMonthChanged에서 일괄 호출.
/// </summary>
public interface IMonthChangeHandler
{
    void HandleMonthChanged();
}
