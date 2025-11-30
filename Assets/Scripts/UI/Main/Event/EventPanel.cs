using UnityEngine;

public class EventPanel : BasePanel
{
    protected override void OnInitialize()
    {
        if (_dataManager == null)
        {
            Debug.LogWarning("[ProductionPanel] DataManager is null.");
            return;
        }
    }
}
