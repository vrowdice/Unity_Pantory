using UnityEngine;

/// <summary>
/// 주문 관리 패널
/// </summary>
public class OrderPanel : BasePanel
{
    protected override void OnInitialize()
    {
        if (_dataManager == null)
        {
            Debug.LogWarning("[OrderPanel] DataManager is null.");
            return;
        }
    }
}

