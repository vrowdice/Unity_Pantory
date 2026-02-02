using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuickMoveBtn : MonoBehaviour
{
    [SerializeField] private Image _icon = null;
    [SerializeField] private TextMeshProUGUI _text = null;

    private MainPanelType _penalType;
    MainCanvas _mainUiManager = null;

    public void Initialize(MainCanvas argUiManager, MainPanelType argPenalType)
    {
        _mainUiManager = argUiManager;
        _penalType = argPenalType;

        _text.text = argPenalType.Localize();
        _icon.sprite = VisualManager.Instance.GetMainPanelIcon(_penalType.ToString());
    }

    public void OnClick()
    {
        _mainUiManager.OpenPanel(_penalType);
    }
}
