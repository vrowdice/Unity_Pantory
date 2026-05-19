using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;

public class UnionPopupEmployeeInfoContainer : MonoBehaviour
{
    [SerializeField] private Image _image;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _countText;
    [SerializeField] private TextMeshProUGUI _satisfactionText;
    [SerializeField] private Slider _satisfactionSlider;

    private EmployeeEntry _employeeEntry;

    public void Init(EmployeeEntry employeeEntry)
    {
        _employeeEntry = employeeEntry;

        RefreshUI();
    }

    public void RefreshUI()
    {
        if (_employeeEntry == null)
        {
            return;
        }

        _image.sprite = _employeeEntry.data.image;
        _nameText.text = _employeeEntry.data.type.Localize(LocalizationUtils.TABLE_EMPLOYEE);
        _countText.text = _employeeEntry.state.count.ToString();
        _satisfactionSlider.value = _employeeEntry.state.currentSatisfaction;
        _satisfactionText.text = _employeeEntry.state.currentSatisfaction.ToString();
    }
}
