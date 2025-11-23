using UnityEngine;

/// <summary>
/// 연구 관리 패널
/// </summary>
public class ResearchPanel : BasePanel
{
    protected override void OnInitialize()
    {
        if (_dataManager == null)
        {
            Debug.LogWarning("[ResearchPanel] DataManager is null.");
            return;
        }
    }
}

