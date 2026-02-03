/// <summary>
/// 이벤트 구독을 가진 핸들러. 씬 전환 시 ClearAllSubscriptions로 구독 해제.
/// </summary>
public interface IDataHandlerEvents
{
    void ClearAllSubscriptions();
}
