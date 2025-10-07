using UnityEngine;
using TMPro;

public class QuickMoveBtn : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI _text = null;
    private MainPanelType _penalType;

    MainUiManager _mainUiManager = null;

    public void Initialize(MainUiManager argUiManager, MainPanelType argPenalType)
    {
        _mainUiManager = argUiManager;
        _penalType = argPenalType;

        _text.text = argPenalType.ToString();
    }

    public void OnClick()
    {
        _mainUiManager.OpenPanel(_penalType);
    }
}
