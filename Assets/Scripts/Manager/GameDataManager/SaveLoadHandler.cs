using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// 게임 데이터 저장/로드를 처리하는 클래스
/// Thread 데이터는 별도 파일로 관리됩니다.
/// </summary>
public class SaveLoadHandler
{
    private const string THREAD_SAVE_FILE = "ThreadData.json";
    
    /// <summary>
    /// SaveLoadHandler의 기본 생성자.
    /// NOTE: MonoBehaviour 클래스는 일반적으로 new로 생성되지 않고 AddComponent를 사용합니다.
    /// </summary>
    public SaveLoadHandler(GameDataManager gameDataManager)
    {

    }

    /// <summary>
    /// 저장 파일의 전체 경로를 반환합니다.
    /// </summary>
    private string GetSaveFilePath()
    {
        return Path.Combine(Application.persistentDataPath, THREAD_SAVE_FILE);
    }

    #region 데이터 저장 및 로드

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
            // 저장할 데이터를 ThreadSaveData 객체에 담기
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
    /// Thread 데이터를 로드합니다. 로드 성공 시 ThreadDataHandler에 반영됩니다.
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

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveLoadHandler] Failed to load thread data: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// ThreadDataHandler 내부의 모든 스레드 및 카테고리 데이터를 초기화합니다.
    /// </summary>
    /// <param name="threadService">ThreadDataHandler 인스턴스</param>
    public void ClearAllData(ThreadDataHandler threadService)
    {
        if (threadService == null)
        {
            Debug.LogError("[SaveLoadHandler] ThreadDataHandler is null, cannot clear internal data.");
            return;
        }

        // ThreadDataHandler에 구현된 초기화 메서드를 호출
        threadService.ClearAllThreads();

        Debug.Log("[SaveLoadHandler] All thread and category data cleared internally.");
    }

    #endregion

    #region 파일 관리 유틸리티

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
            Debug.LogWarning($"[SaveLoadHandler] Thread save file not found, nothing to delete: {filePath}");
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

    #endregion

    #region 직렬화 Wrapper 클래스

    /// <summary>
    /// Thread 저장 데이터를 담는 Wrapper 클래스 (JSON 직렬화용)
    /// </summary>
    [Serializable]
    private class ThreadSaveData
    {
        public List<ThreadState> threads = new List<ThreadState>();
        public List<ThreadCategory> categories = new List<ThreadCategory>();
    }

    /// <summary>
    /// Vector2Int를 직렬화하기 위한 Wrapper 클래스 (JSON 직렬화용)
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

    #endregion
}