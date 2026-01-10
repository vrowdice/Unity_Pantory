using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

public partial class DesignCanvas
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
        string currentThreadId = _designRunner.CurrentThreadId;
        if (!string.IsNullOrEmpty(currentThreadId))
        {
            ThreadState existingThread = DataManager.Thread.GetThread(currentThreadId);
            if (existingThread != null && !string.IsNullOrEmpty(existingThread.threadName))
            {
                _currentThreadTitle = existingThread.threadName;
                return;
            }
        }

        Dictionary<string, ThreadState> allThreads = DataManager.Thread.GetAllThreads();
        if (allThreads != null && allThreads.Count > 0)
        {
            ThreadState firstThread = allThreads.Values.First();
            if (firstThread != null && !string.IsNullOrEmpty(firstThread.threadName))
            {
                _currentThreadTitle = firstThread.threadName;
                return;
            }
        }

        _currentThreadTitle = DefaultThreadTitle;
    }
}
