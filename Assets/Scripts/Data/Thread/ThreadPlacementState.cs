using System;
using UnityEngine;

[Serializable]
public class ThreadPlacementState
{
    public Vector2Int GridPosition;      // 위치 정보
    public string TemplateId;            // 원본 템플릿 ID (예: "iron_mine")
    public ThreadState RuntimeState;     // [핵심] 독립적인 런타임 상태 (직원수, 진행도 등)

    public ThreadPlacementState(Vector2Int pos, string templateId, ThreadState initialState)
    {
        GridPosition = pos;
        TemplateId = templateId;
        RuntimeState = initialState; // 복사본을 받아야 함
    }

    public string ThreadId => RuntimeState?.threadId ?? string.Empty;
}
