using UnityEngine;

public class TopInfoPanel : MonoBehaviour
{
    [SerializeField] private CreditTopInfoPanel _creditTopInfoPanel;
    [SerializeField] private EmployeeTopInfoPanel _employeeTopInfoPanel;
    [SerializeField] private DateTopInfoPanel _dateTopInfoPanel;

    public void Init(DataManager dataManager)
    {
        _creditTopInfoPanel.Init(dataManager);
        _employeeTopInfoPanel.Init(dataManager);
        _dateTopInfoPanel.Init(dataManager);
    }
}
