using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// ���� ���� �г�
/// </summary>
public class EmployeePanel : BasePanel
{
    [SerializeField] private Image _employeeImage;
    [SerializeField] private TextMeshProUGUI _titleText;
    [SerializeField] private TextMeshProUGUI _descriptionText;

    [SerializeField] private TextMeshProUGUI _employeeCountText;
    [SerializeField] private TextMeshProUGUI _totalSalatyText;

    [SerializeField] private Slider _efficiencySlider;
    [SerializeField] private Slider _satisfactionSlider;
    [SerializeField] private TextMeshProUGUI _efficiencyValueText;
    [SerializeField] private TextMeshProUGUI _satisfactionValueText;

    private EmployeeEntry _employeeEntry;

    /// <summary>
    /// (BasePanel)
    /// </summary>
    protected override void OnInitialize()
    {
        if (_dataManager == null)
        {
            Debug.LogWarning("[ProductionPanel] DataManager is null.");
            return;
        }
    }

    void Update()
    {

    }
}