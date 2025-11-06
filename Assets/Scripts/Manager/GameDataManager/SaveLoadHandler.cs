using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// 게임 데이터 저장/로드를 처리하는 클래스
/// Thread 데이터는 별도 파일로 관리됩니다.
/// </summary>
public class SaveLoadHandler : MonoBehaviour
{
    private const string THREAD_SAVE_FILE = "ThreadData.json";
    
    private string GetSaveFilePath()
    {
        return Path.Combine(Application.persistentDataPath, THREAD_SAVE_FILE);
    }

    /// <summary>
    /// Thread 데이터를 저장합니다.
    /// </summary>
    /// <param name="threadService">ThreadDataHandler 인스턴스</param>
    /// <returns>성공 시 true</returns>
    public bool SaveThreadData(ThreadDataHandler threadService)
    {
        if (threadService == null)
        {
            Debug.LogError("[SaveLoadHandler] ThreadDataHandler is null.");
            return false;
        }

        try
        {
            var saveData = new ThreadSaveData
            {
                threads = threadService.GetThreadListForSave(),
                categories = threadService.GetCategoryListForSave()
            };

            string json = JsonUtility.ToJson(saveData, true);
            string filePath = GetSaveFilePath();
            
            // 디렉토리가 없으면 생성
            string directory = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            File.WriteAllText(filePath, json);
            Debug.Log($"[SaveLoadHandler] Thread data saved to: {filePath}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveLoadHandler] Failed to save thread data: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Thread 데이터를 로드합니다.
    /// </summary>
    /// <param name="threadService">ThreadDataHandler 인스턴스</param>
    /// <returns>성공 시 true</returns>
    public bool LoadThreadData(ThreadDataHandler threadService)
    {
        if (threadService == null)
        {
            Debug.LogError("[SaveLoadHandler] ThreadDataHandler is null.");
            return false;
        }

        string filePath = GetSaveFilePath();
        
        if (!File.Exists(filePath))
        {
            Debug.LogWarning($"[SaveLoadHandler] Thread save file not found: {filePath}");
            return false;
        }

        try
        {
            string json = File.ReadAllText(filePath);
            ThreadSaveData saveData = JsonUtility.FromJson<ThreadSaveData>(json);
            
            if (saveData == null)
            {
                Debug.LogError("[SaveLoadHandler] Failed to parse save data.");
                return false;
            }

            // 기존 Thread 데이터 초기화
            threadService.ClearAllThreads();
            
            // 카테고리 먼저 로드
            if (saveData.categories != null)
            {
                threadService.LoadCategories(saveData.categories);
            }

            // Thread 로드
            if (saveData.threads != null)
            {
                threadService.LoadThreads(saveData.threads);
            }

            Debug.Log($"[SaveLoadHandler] Thread data loaded from: {filePath} (Threads: {saveData.threads?.Count ?? 0}, Categories: {saveData.categories?.Count ?? 0})");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveLoadHandler] Failed to load thread data: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Thread 저장 파일이 존재하는지 확인합니다.
    /// </summary>
    /// <returns>파일이 존재하면 true</returns>
    public bool HasThreadSaveFile()
    {
        return File.Exists(GetSaveFilePath());
    }

    /// <summary>
    /// Thread 저장 파일을 삭제합니다.
    /// </summary>
    /// <returns>성공 시 true</returns>
    public bool DeleteThreadSaveFile()
    {
        string filePath = GetSaveFilePath();
        
        if (!File.Exists(filePath))
        {
            return false;
        }

        try
        {
            File.Delete(filePath);
            Debug.Log($"[SaveLoadHandler] Thread save file deleted: {filePath}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveLoadHandler] Failed to delete thread save file: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Thread 저장 데이터를 담는 Wrapper 클래스
    /// </summary>
    [Serializable]
    private class ThreadSaveData
    {
        public List<ThreadState> threads = new List<ThreadState>();
        public List<ThreadCategory> categories = new List<ThreadCategory>();
    }

    /// <summary>
    /// Vector2Int를 직렬화하기 위한 Wrapper 클래스
    /// </summary>
    [Serializable]
    public class SerializableVector2Int
    {
        public int x;
        public int y;

        public SerializableVector2Int() { }
        
        public SerializableVector2Int(Vector2Int v)
        {
            x = v.x;
            y = v.y;
        }

        public Vector2Int ToVector2Int()
        {
            return new Vector2Int(x, y);
        }
    }
}