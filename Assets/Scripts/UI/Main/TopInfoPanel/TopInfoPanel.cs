using UnityEngine;

public class TopInfoPanel : MonoBehaviour
{
    [SerializeField] private EmployeeTopInfoPanel _employeeTopInfoPanel;
    [SerializeField] private DateTopInfoPanel _dateTopInfoPanel;

    public void Init()
    {
        _employeeTopInfoPanel.Init();
        _dateTopInfoPanel.Init();
    }
}
