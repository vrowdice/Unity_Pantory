using UnityEngine;
using TMPro;

public class QuickMoveBtn : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI _text = null;
    private MainPanelType _penalType;

    MainCanvas _mainUiManager = null;

    public void Initialize(MainCanvas argUiManager, MainPanelType argPenalType)
    {
        _mainUiManager = argUiManager;
        _penalType = argPenalType;

        _text.text = argPenalType.Localize();
    }

    public void OnClick()
    {
        _mainUiManager.OpenPanel(_penalType);
    }
}
