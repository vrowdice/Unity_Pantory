using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;

public class UniopPopupEmployeeInfoContainer : MonoBehaviour
{
    [SerializeField] private Image _image;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _countText;
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

        _image.sprite = _employeeEntry.data.icon;
        _nameText.text = _employeeEntry.data.displayName;
        _countText.text = _employeeEntry.state.count.ToString();
        _satisfactionSlider.value = _employeeEntry.state.currentSatisfaction;
    }
}
