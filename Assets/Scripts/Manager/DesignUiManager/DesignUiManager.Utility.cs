using UnityEngine;

public partial class DesignUiManager
{
    public string GetCurrentThreadTitle()
    {
        return string.IsNullOrEmpty(_currentThreadTitle) ? DefaultThreadTitle : _currentThreadTitle;
    }

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

        string currentThreadId = _buildingTileManager.CurrentThreadId;
        if (!string.IsNullOrEmpty(currentThreadId) && _dataManager != null)
        {
            ThreadState existingThread = _dataManager.GetThread(currentThreadId);
            if (existingThread != null && !string.IsNullOrEmpty(existingThread.threadName))
            {
                _currentThreadTitle = existingThread.threadName;
                return;
            }
        }

        _currentThreadTitle = DefaultThreadTitle;

        string defaultThreadId = GetThreadIdFromTitle(_currentThreadTitle);
        _buildingTileManager.SetCurrentThread(defaultThreadId);
    }
}
