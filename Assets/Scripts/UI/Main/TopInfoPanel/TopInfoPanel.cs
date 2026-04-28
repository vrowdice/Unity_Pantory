using UnityEngine;

public class TopInfoPanel : MonoBehaviour
{
    [SerializeField] private EmployeeTopInfoPanel _employeeTopInfoPanel;
    [SerializeField] private DateTopInfoPanel _dateTopInfoPanel;

    public void Init(DataManager dataManager, VisualManager visualManager)
    {
        _employeeTopInfoPanel.Init(dataManager, visualManager);
        _dateTopInfoPanel.Init(dataManager);
    }
}
