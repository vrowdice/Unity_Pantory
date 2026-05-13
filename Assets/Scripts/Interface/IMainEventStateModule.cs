/// <summary>
/// 메인 이벤트(연합/전쟁/자동화) 타입별 일 단위 스테이트 모듈.
/// </summary>
public interface IMainEventStateModule
{
    bool IsComplete { get; }
    int ActiveTime { get; }

    void OnDayChanged();
}
