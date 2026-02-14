using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using System;

public static class LocalizationUtils
{
    public const string TABLE_EMPLOYEE_TYPE = "EmployeeType";
    public const string TABLE_EMPLOYEE_DESCRIPTION = "EmployeeDescription";
    public const string TABLE_COMMON = "Common";
    public const string TABLE_MAIN_PANEL_TYPE = "MainPanelType";
    public const string TABLE_MARKET_PANEL_TYPE = "MarketPanelType";
    public const string TABLE_RESOURCE_DISPLAY_NAME = "ResourceDisplayName";
    public const string TABLE_RESOURCE_TYPE = "ResourceType";
    public const string TABLE_RESEARCH = "Research";
    public const string TABLE_RESEARCH_DESCRIPTION = "ResearchDescription";
    public const string TABLE_MARKET_ACTOR = "MarketActor";
    public const string TABLE_MARKET_ACTOR_DESCRIPTION = "MarketActorDescription";
    public const string TABLE_MARKET_ACTOR_TYPE = "MarketActorType";
    public const string TABLE_BUILDING = "Building";
    public const string TABLE_BUILDING_DESCRIPTION = "BuildingDescription";
    public const string TABLE_BUILDING_TYPE = "BuildingType";
    public const string TABLE_WARNING_MESSAGE = "WarningMessage";
    public const string TABLE_EFFECT = "Effect";
    public const string TABLE_NEWS = "News";
    public const string TABLE_NEWS_DESCRIPTION = "NewsDescription";

    private const string DEFAULT_TABLE = TABLE_COMMON;

    /// <summary>
    /// [확장 메서드] 키(string)를 가지고 번역된 스트링을 반환
    /// 사용법: "KEY_NAME".Localize("TableName");
    /// </summary>
    public static string Localize(this string key, string tableName = DEFAULT_TABLE)
    {
        LocalizedString localizedString = new LocalizedString(tableName, key);
        return localizedString.GetLocalizedString();
    }

    /// <summary>
    /// [확장 메서드] 포맷팅이 필요한 번역 (예: "공격력: {0}")
    /// 사용법: "KEY_ATTACK".LocalizeFormat("TableName", 50);
    /// </summary>
    public static string LocalizeFormat(this string key, string tableName, params object[] args)
    {
        LocalizedString localizedString = new LocalizedString(tableName, key);
        return string.Format(localizedString.GetLocalizedString(), args);
    }

    /// <summary>
    /// [확장 메서드] Enum을 키로 변환하여 번역
    /// 사용법: EmployeeType.Worker.Localize(); -> "Worker" 키를 찾음 (테이블명은 타입명과 동일하다고 가정)
    /// </summary>
    public static string Localize(this Enum enumValue, string tableName = null)
    {
        if (string.IsNullOrEmpty(tableName))
        {
            tableName = enumValue.GetType().Name;
        }
        string key = enumValue.ToString();
        return key.Localize(tableName);
    }
}
