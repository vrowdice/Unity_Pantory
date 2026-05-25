/// <summary>
/// 다른 핸들러 이벤트를 구독하는 핸들러. 씬 전환 후 SubscribeCrossHandlerEvents로 재구독.
/// </summary>
public interface ICrossHandlerEvents
{
    void SubscribeCrossHandlerEvents();
}
