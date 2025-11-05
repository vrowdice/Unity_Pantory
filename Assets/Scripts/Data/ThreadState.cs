using System;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Thread(생산 라인)의 상태를 나타내는 클래스
/// </summary>
[Serializable]
public class ThreadState
{
    public string threadId = string.Empty;         // 고유 식별자 (플레이어가 저장할 때 사용)
    public string threadName = string.Empty;       // 표시 이름
    public string categoryId = string.Empty;       // 속한 카테고리 ID
    public string division = string.Empty;         // 부서/사업부
    public List<BuildingState> buildingStateList = new List<BuildingState>();
    
    public ThreadState()
    {
    }
    
    public ThreadState(string id, string name, string div = "", string catId = "")
    {
        threadId = id;
        threadName = name;
        division = div;
        categoryId = catId;
    }
}
