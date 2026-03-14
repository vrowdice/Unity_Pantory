using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OrderRequireResourceItemPanel : MonoBehaviour
{
    [SerializeField] private Image _resourceIcon;
    [SerializeField] private TextMeshProUGUI _resourceNameText;
    [SerializeField] private TextMeshProUGUI _conditionText;

    private ResourceEntry _resourceEntry;
    private int _conditionAmount;

    public void Init(ResourceEntry resourceEntry, int conditionAmount)
    {
        _resourceEntry = resourceEntry;
        _conditionAmount = conditionAmount;

        _resourceIcon.sprite = _resourceEntry.data.icon;
        _resourceNameText.text = _resourceEntry.data.id.Localize(LocalizationUtils.TABLE_RESOURCE);
        _conditionText.text = $"{_resourceEntry.state.count} / {_conditionAmount}";
    }

    public void UpdateUI()
    {
        _resourceIcon.sprite = _resourceEntry.data.icon;
        _resourceNameText.text = _resourceEntry.data.id.Localize(LocalizationUtils.TABLE_RESOURCE);
        _conditionText.text = $"{_resourceEntry.state.count} / {_conditionAmount}";
    }
}
