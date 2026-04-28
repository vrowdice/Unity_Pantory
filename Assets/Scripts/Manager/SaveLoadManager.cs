using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

/// <summary>
/// 게임 데이터 저장/로드를 처리하는 매니저 클래스.
/// JSON 본문은 GameSaveData이며, DataManager.CaptureGameStateTo / ApplyGameStateFrom 에서
/// 튜토리얼 진행(PlayerDataHandler, tutorialAutoShowPending)을 포함한 전체 상태를 채우거나 복원합니다.
/// 사용자 볼륨·언어 등은 persistentDataPath 의 단일 GameUserSettings.json 으로 별도 관리합니다.
/// </summary>
public class SaveLoadManager : Singleton<SaveLoadManager>
{
    private const string SaveFileDirectory = "SaveFiles";
    private const string SaveFileExtension = ".json";
    private const string LogPrefix = "[SaveLoadManager]";

    /// <summary>게임 슬롯 세이브와 무관하게 persistentDataPath 루트에 단일 파일로 유지.</summary>
    private const string UserSettingsFileName = "GameUserSettings.json";
    private const string PlayerPrefsLocaleKey = "SelectedLocale";

    /// <summary>기존 오토세이브 파일만 삭제할 때 사용하는 파일명 접두사 (수동 세이브와 구분).</summary>
    public const string AutoSaveFileNamePrefix = "AutoSave_";

    protected override void Awake()
    {
        base.Awake();
    }

    public void Init()
    {
        if (Instance != this)
        {
            return;
        }
    }

    private static string GetUserSettingsFilePath()
    {
        return Path.Combine(Application.persistentDataPath, UserSettingsFileName);
    }

    /// <summary>
    /// 단일 사용자 설정 파일을 읽어 볼륨·언어를 적용합니다. 파일이 없으면 아무 것도 하지 않습니다.
    /// </summary>
    public void TryLoadUserSettingsAndApply()
    {
        if (!TryReadUserSettingsFile(out UserSettingsSaveData data))
        {
            return;
        }

        SoundManager soundManager = SoundManager.Instance;
        if (soundManager != null)
        {
            soundManager.ApplyVolumesFromUserSettings(data.bgmVolume, data.sfxVolume);
        }

        ApplyLocaleFromSettings(string.IsNullOrEmpty(data.localeCode) ? "en" : data.localeCode);
    }

    /// <summary>
    /// 설정 파일에서 BGM/SFX 볼륨만 읽어 SoundManager에 반영합니다. 씬 전환 시 갱신용.
    /// </summary>
    public void TryApplyUserSettingsVolumesToSound()
    {
        if (!TryReadUserSettingsFile(out UserSettingsSaveData data))
        {
            return;
        }

        SoundManager soundManager = SoundManager.Instance;
        if (soundManager != null)
        {
            soundManager.ApplyVolumesFromUserSettings(data.bgmVolume, data.sfxVolume);
        }
    }

    private bool TryReadUserSettingsFile(out UserSettingsSaveData data)
    {
        data = null;
        string filePath = GetUserSettingsFilePath();
        if (!File.Exists(filePath))
        {
            return false;
        }

        try
        {
            string json = File.ReadAllText(filePath);
            data = JsonUtility.FromJson<UserSettingsSaveData>(json);
            return data != null;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"{LogPrefix} Failed to read user settings file: {e.Message}");
            return false;
        }
    }

    /// <summary>현재 SoundManager 볼륨 및 선택 언어를 단일 설정 파일에 덮어씁니다.</summary>
    public bool SaveUserSettingsFromCurrentState()
    {
        try
        {
            UserSettingsSaveData data = new UserSettingsSaveData();
            SoundManager soundManager = SoundManager.Instance;
            if (soundManager != null)
            {
                data.bgmVolume = soundManager.GetBGMVolume();
                data.sfxVolume = soundManager.GetSFXVolume();
            }

            data.localeCode = LocalizationSettings.SelectedLocale != null
                ? LocalizationSettings.SelectedLocale.Identifier.Code
                : PlayerPrefs.GetString(PlayerPrefsLocaleKey, "en");

            string json = JsonUtility.ToJson(data, true);
            string filePath = GetUserSettingsFilePath();
            File.WriteAllText(filePath, json);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"{LogPrefix} Failed to save user settings: {e.Message}");
            return false;
        }
    }

    private static void ApplyLocaleFromSettings(string localeCode)
    {
        if (LocalizationSettings.AvailableLocales == null || LocalizationSettings.AvailableLocales.Locales == null)
        {
            return;
        }

        IList<Locale> availableLocales = LocalizationSettings.AvailableLocales.Locales;
        Locale targetLocale = null;

        foreach (Locale locale in availableLocales)
        {
            if (locale.Identifier.Code == localeCode)
            {
                targetLocale = locale;
                break;
            }
        }

        if (targetLocale != null)
        {
            LocalizationSettings.SelectedLocale = targetLocale;
            PlayerPrefs.SetString(PlayerPrefsLocaleKey, localeCode);
            PlayerPrefs.Save();
        }
    }

    public bool SaveSavefile(string fileName, DataManager dataManager)
    {
        if (!TryValidateFileOperation(fileName, dataManager, out string validationError))
        {
            Debug.LogError($"{LogPrefix} {validationError}");
            return false;
        }

        try
        {
            GameSaveData saveData = new GameSaveData();
            dataManager.CaptureGameStateTo(saveData);
            string json = JsonUtility.ToJson(saveData, true);
            string filePath = GetSaveFilePath(fileName);
            string directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(filePath, json);
            Debug.Log($"{LogPrefix} Game data saved to: {filePath}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"{LogPrefix} Failed to save game data: {e.Message}");
            return false;
        }
    }

    public bool LoadSaveFile(string fileName, DataManager dataManager)
    {
        if (!TryValidateFileOperation(fileName, dataManager, out string validationError))
        {
            Debug.LogError($"{LogPrefix} {validationError}");
            return false;
        }

        string filePath = GetSaveFilePath(fileName);
        if (!File.Exists(filePath))
        {
            Debug.LogWarning($"{LogPrefix} Save file not found: {filePath}");
            return false;
        }

        try
        {
            string json = File.ReadAllText(filePath);
            GameSaveData saveData = JsonUtility.FromJson<GameSaveData>(json);
            if (saveData == null)
            {
                Debug.LogError($"{LogPrefix} Failed to parse save data.");
                return false;
            }

            dataManager.ApplyGameStateFrom(saveData);
            Debug.Log($"{LogPrefix} Game data loaded from: {filePath}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"{LogPrefix} Failed to load game data: {e.Message}");
            return false;
        }
    }

    public List<string> GetSaveFileList()
    {
        List<string> saveFiles = new List<string>();
        string directory = GetSaveFileDirectory();
        if (!Directory.Exists(directory))
        {
            return saveFiles;
        }

        try
        {
            foreach (string file in Directory.GetFiles(directory, $"*{SaveFileExtension}"))
            {
                saveFiles.Add(Path.GetFileNameWithoutExtension(file));
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"{LogPrefix} Failed to get save file list: {e.Message}");
        }

        return saveFiles;
    }

    public bool HasSaveFile(string fileName)
    {
        return File.Exists(GetSaveFilePath(fileName));
    }

    /// <summary>
    /// 월 변경 등에서 호출: 기존 오토세이브를 모두 지운 뒤, 게임 내 시각(TimeDataHandler) 기준
    /// <c>AutoSave_Y_M_D_H</c> 이름으로 한 개만 저장합니다.
    /// </summary>
    public bool PerformAutoSave(DataManager dataManager)
    {
        if (dataManager == null)
        {
            Debug.LogError($"{LogPrefix} PerformAutoSave: DataManager is null.");
            return false;
        }

        if (dataManager.Time == null)
        {
            Debug.LogError($"{LogPrefix} PerformAutoSave: Time is null.");
            return false;
        }

        DeleteAllAutoSaveFiles();

        TimeDataHandler time = dataManager.Time;
        string fileName = $"{AutoSaveFileNamePrefix}{time.Year}_{time.Month:00}_{time.Day:00}_{time.CurrentHour:00}";
        return SaveSavefile(fileName, dataManager);
    }

    /// <summary>세이브 폴더에서 오토세이브(<see cref="AutoSaveFileNamePrefix"/>*) 파일만 삭제합니다.</summary>
    public void DeleteAllAutoSaveFiles()
    {
        string directory = GetSaveFileDirectory();
        if (!Directory.Exists(directory))
        {
            return;
        }

        try
        {
            foreach (string filePath in Directory.GetFiles(directory, $"{AutoSaveFileNamePrefix}*{SaveFileExtension}"))
            {
                File.Delete(filePath);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"{LogPrefix} Failed to delete auto-save files: {e.Message}");
        }
    }

    public bool DeleteSaveFile(string fileName)
    {
        string filePath = GetSaveFilePath(fileName);
        if (!File.Exists(filePath))
        {
            Debug.LogWarning($"{LogPrefix} Save file not found, nothing to delete: {filePath}");
            return false;
        }

        try
        {
            File.Delete(filePath);
            Debug.Log($"{LogPrefix} Save file deleted: {filePath}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"{LogPrefix} Failed to delete save file: {e.Message}");
            return false;
        }
    }

    private static bool TryValidateFileOperation(string fileName, DataManager dataManager, out string error)
    {
        if (dataManager == null)
        {
            error = "DataManager is null.";
            return false;
        }

        if (string.IsNullOrEmpty(fileName))
        {
            error = "File name is null or empty.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private string GetSaveFileDirectory()
    {
        return Path.Combine(Application.persistentDataPath, SaveFileDirectory);
    }

    private string GetSaveFilePath(string fileName)
    {
        if (!fileName.EndsWith(SaveFileExtension))
        {
            fileName += SaveFileExtension;
        }

        return Path.Combine(GetSaveFileDirectory(), fileName);
    }
}
