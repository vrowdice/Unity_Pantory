using UnityEngine;

public class MainCanvasMainEventBtns : MonoBehaviour
{
    [SerializeField] private MainCanvasUnionContainer _unionContainer;
    [SerializeField] private MainCanvasWarContainer _warContainer;
    [SerializeField] private MainCanvasAutomationContainer _automationContainer;

    public void SetMainEventContainer(MainEventType mainEventType, MainCanvas mainCanvas)
    {
        switch (mainEventType)
        {
            case MainEventType.Union:
                _unionContainer.gameObject.SetActive(true);
                _unionContainer.Init(mainCanvas);

                _warContainer.gameObject.SetActive(false);
                _automationContainer.gameObject.SetActive(false);
                break;
            case MainEventType.War:
                _warContainer.gameObject.SetActive(true);
                _warContainer.Init(mainCanvas);

                _unionContainer.gameObject.SetActive(false);
                _automationContainer.gameObject.SetActive(false);
                break;
            case MainEventType.Automation:
                _automationContainer.gameObject.SetActive(true);
                _automationContainer.Init(mainCanvas);

                _unionContainer.gameObject.SetActive(false);
                _warContainer.gameObject.SetActive(false);
                break;
            default:
                _unionContainer.gameObject.SetActive(false);
                _warContainer.gameObject.SetActive(false);
                _automationContainer.gameObject.SetActive(false);
                break;
        }
    }
}
