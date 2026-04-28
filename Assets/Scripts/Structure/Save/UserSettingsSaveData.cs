using System;
using UnityEngine;

/// <summary>
/// 게임 세이브와 별도의 단일 사용자 설정 파일(JsonUtility)용 데이터.
/// </summary>
[Serializable]
public class UserSettingsSaveData
{
    public float bgmVolume = 0.5f;
    public float sfxVolume = 1f;
    public string localeCode = "en";
}
