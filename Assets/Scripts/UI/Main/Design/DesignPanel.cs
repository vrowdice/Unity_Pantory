using UnityEngine;

public class DesignPanel : BasePanel
{
    /// <summary>
    /// 초기화 (BasePanel 상속)
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
