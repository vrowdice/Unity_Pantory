using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Thread 저장 데이터를 담는 Wrapper 클래스 (JSON 직렬화용)
/// </summary>
[Serializable]
public class ThreadSaveData
{
    public List<ThreadState> threads = new List<ThreadState>();
    public List<ThreadCategory> categories = new List<ThreadCategory>();
}
