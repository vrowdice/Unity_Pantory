using UnityEngine;

/// <summary>
/// 메인 이벤트 모듈 초기화 SO 공통. 제목 키 하나만 두고, 본문은 키 + <c>_Desc</c> 로 <see cref="LocalizationUtils.TABLE_MAIN_EVENT"/>에서 조회합니다.
/// </summary>
public abstract class InitialMainEventModuleData : ScriptableObject
{
    [Tooltip("이벤트 종료까지 남은 일수(0 이하면 무기한). 모듈별로 해석")]
    public int eventOverDate;

    [Header("NewsPopup")]
    [Tooltip("MainEvent 테이블 제목 키. 본문은 동일 키 + _Desc.")]
    public string announcementLocalizationKey;

    [Tooltip("이벤트 공지 팝업에 표시할 아이콘")]
    public Sprite announcementIcon;
    [Tooltip("공지 팝업이 열릴 때 재생할 효과음")]
    public AudioClip openNewsAudio;
}
