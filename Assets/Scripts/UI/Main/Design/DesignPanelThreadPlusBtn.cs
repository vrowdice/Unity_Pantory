using UnityEngine;

public class DesignPanelThreadPlusBtn : MonoBehaviour
{
    private DesignPanel _designPanel;

    public void Initialize(DesignPanel designPanel)
    {
        _designPanel = designPanel;
    }

    public void OnClick()
    {
        Debug.Log("OnClick");
    }
}
