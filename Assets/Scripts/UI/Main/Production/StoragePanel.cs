using UnityEngine;

/// <summary>
/// 생산 관리 패널
/// </summary>
public class StoragePanel : BasePanel
{
    /// <summary>
    /// 패널 초기화 (BasePanel에서 호출)
    /// </summary>
    protected override void OnInitialize()
    {
        if (_dataManager == null)
        {
            Debug.LogWarning("[ProductionPanel] DataManager is null.");
            return;
        }
    }

    void Update()
    {
        // 업데이트 로직
    }
}
