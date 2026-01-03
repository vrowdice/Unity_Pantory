using System;
using System.Collections.Generic;

/// <summary>
/// Thread 카테고리 정보를 담는 클래스
/// </summary>
[Serializable]
public class ThreadCategory
{
    public string categoryId = string.Empty;           // 카테고리 고유 ID
    public string categoryName = string.Empty;         // 카테고리 이름
    public List<string> threadIds = new List<string>(); // 이 카테고리에 속한 스레드 ID 목록

    public ThreadCategory()
    {
    }

    public ThreadCategory(string id, string name)
    {
        categoryId = id;
        categoryName = name;
    }

    /// <summary>
    /// 이 카테고리에 속한 스레드 개수
    /// </summary>
    public int ThreadCount => threadIds != null ? threadIds.Count : 0;
}

