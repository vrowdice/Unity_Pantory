using UnityEngine;
using System.Linq;

public partial class DesignUiManager
{
    public string GetThreadIdFromTitle(string threadTitle)
    {
        if (string.IsNullOrWhiteSpace(threadTitle))
        {
            threadTitle = DefaultThreadTitle;
        }

        return "thread_" + threadTitle.Trim().Replace(" ", "_").ToLower();
    }

    private void InitializeThreadTitle()
    {
        if (_buildingTileManager == null)
        {
            _currentThreadTitle = DefaultThreadTitle;
            return;
        }

        if (_dataManager == null || _dataManager.Thread == null)
        {
            _currentThreadTitle = DefaultThreadTitle;
            return;
        }

        string currentThreadId = _buildingTileManager.CurrentThreadId;
        if (!string.IsNullOrEmpty(currentThreadId))
        {
            ThreadState existingThread = _dataManager.Thread.GetThread(currentThreadId);
            if (existingThread != null && !string.IsNullOrEmpty(existingThread.threadName))
            {
                _currentThreadTitle = existingThread.threadName;
                return;
            }
        }

        // 스레드가 없으면 기본 스레드 생성 또는 첫 번째 스레드 사용
        if (_dataManager?.Thread == null)
        {
            _currentThreadTitle = DefaultThreadTitle;
            return;
        }

        var allThreads = _dataManager.Thread.GetAllThreads();
        if (allThreads != null && allThreads.Count > 0)
        {
            // 존재하는 첫 번째 스레드 사용
            var firstThread = allThreads.Values.First();
            if (firstThread != null && !string.IsNullOrEmpty(firstThread.threadName))
            {
                _currentThreadTitle = firstThread.threadName;
                if (_buildingTileManager != null && _dataManager != null && _dataManager.Thread != null)
                {
                    _buildingTileManager.SetCurrentThread(firstThread.threadId);
                }
                return;
            }
        }

        // 스레드가 전혀 없으면 기본 스레드 제목 사용
        _currentThreadTitle = DefaultThreadTitle;
    }
}
