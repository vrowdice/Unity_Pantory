using UnityEngine;

/// <summary>
/// 메인 이벤트 모듈 초기화 SO 공통. 제목 키 하나만 두고, 본문은 키 + <c>_Desc</c> 로 <see cref="LocalizationUtils.TABLE_MAIN_EVENT"/>에서 조회합니다.
/// </summary>
public abstract class InitialMainEventModuleData : ScriptableObject
{
    [Header("시작 알림 (NewsPopup)")]
    [Tooltip("MainEvent 테이블 제목 키. 본문은 동일 키 + _Desc.")]
    public string announcementLocalizationKey;

    public Sprite announcementIcon;

    [Tooltip("비어 있으면 MainEvent 테이블")]
    public string announcementLocalizationTable = "";

    public string ResolveAnnouncementLocalizationTable()
    {
        if (string.IsNullOrWhiteSpace(announcementLocalizationTable))
        {
            return LocalizationUtils.TABLE_MAIN_EVENT;
        }

        return announcementLocalizationTable;
    }

    public bool HasAnnouncementConfigured()
    {
        return !string.IsNullOrWhiteSpace(announcementLocalizationKey)
            || announcementIcon != null;
    }
}
