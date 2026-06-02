/// <summary>
/// 엔트리/상태를 보유한 리스트 항목 버튼. Init으로 정적 UI를 연결하고 Refresh로 동적 값만 갱신합니다.
/// </summary>
public interface IEntryListBtn
{
    void Refresh();
}
