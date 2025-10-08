using UnityEngine;

public class MarketPanel : BasePanel
{
    /// <summary>
    /// 패널 초기화 (BasePanel에서 호출)
    /// </summary>
    protected override void OnInitialize()
    {
        if (_dataManager == null)
        {
            return;
        }
    }

    void Update()
    {

    }
}
