using UnityEngine;

public class TopInfoPanel : MonoBehaviour
{
    [SerializeField] private CreditTopInfoPanel _creditTopInfoPanel;
    [SerializeField] private EmployeeTopInfoPanel _employeeTopInfoPanel;
    [SerializeField] private DateTopInfoPanel _dateTopInfoPanel;

    public void OnInitialize(DataManager dataManager)
    {
        _creditTopInfoPanel.OnInitialize(dataManager);
        _employeeTopInfoPanel.OnInitialize(dataManager);
        _dateTopInfoPanel.OnInitialize(dataManager);
    }
}
