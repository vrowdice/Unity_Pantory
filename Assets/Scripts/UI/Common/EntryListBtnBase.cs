/// <summary>
/// 데이터 엔트리를 표시하는 리스트 버튼 공통 베이스. Init 후 Refresh로 상태 텍스트 등을 갱신합니다.
/// </summary>
public abstract class EntryListBtnBase : BtnBase, IEntryListBtn
{
    public abstract void Refresh();
}
