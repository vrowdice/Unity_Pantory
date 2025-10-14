using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DesignPanelThreadBtn : MonoBehaviour
{
    [SerializeField] private Image _sampleImage;
    [SerializeField] private TextMeshProUGUI _productionText;
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
