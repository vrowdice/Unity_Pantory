using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// 게임 데이터 저장/로드를 처리하는 매니저 클래스.
/// JSON 본문은 GameSaveData이며, DataManager.CaptureGameStateTo / ApplyGameStateFrom 에서
/// 튜토리얼 진행(PlayerDataHandler, tutorialAutoShowPending)을 포함한 전체 상태를 채우거나 복원합니다.
/// </summary>
public class SaveLoadManager : Singleton<SaveLoadManager>
{
    private const string SaveFileDirectory = "SaveFiles";
    private const string SaveFileExtension = ".json";
    private const string LogPrefix = "[SaveLoadManager]";

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
