using System;
using UnityEngine;

[Serializable]
public class ThreadPlacementState
{
    public Vector2Int GridPosition;      // 위치 정보
    public string TemplateThreadId;            // 원본 템플릿 ID (예: "iron_mine")
    public ThreadState RuntimeState;     // 독립적인 런타임 상태 (직원수, 진행도 등)

    public ThreadPlacementState(Vector2Int pos, string templateId, ThreadState initialState)
    {
        GridPosition = pos;
        TemplateThreadId = templateId;
        RuntimeState = initialState;
    }
}
