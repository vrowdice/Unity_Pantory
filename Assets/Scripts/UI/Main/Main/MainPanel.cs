using UnityEngine;

/// <summary>
/// 메인 패널
/// </summary>
public class MainPanel : BasePanel
{
    /// <summary>
    /// 패널 초기화 (BasePanel에서 호출)
    /// </summary>
    protected override void OnInitialize()
    {
        if (_dataManager == null)
        {
            Debug.LogWarning("[MainPanel] DataManager is null.");
            return;
        }

        // 여기에 메인 패널 전용 UI 초기화 로직 추가
        
        Debug.Log($"[MainPanel] Current Resources - Steel: {_dataManager.Steel}, Wood: {_dataManager.Wood}");
    }

    void Update()
    {
        // 업데이트 로직
    }
}
