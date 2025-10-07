using UnityEngine;

/// <summary>
/// 생산 관리 패널
/// </summary>
public class ProductionPanel : BasePanel
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

        // 여기에 생산 패널 전용 UI 초기화 로직 추가
        // 예: 현재 자원 표시, 생산 가능 항목 표시 등
        
        Debug.Log($"[ProductionPanel] Current Resources - Steel: {_dataManager.Steel}, Wood: {_dataManager.Wood}");
    }

    void Update()
    {
        // 업데이트 로직
    }
}
