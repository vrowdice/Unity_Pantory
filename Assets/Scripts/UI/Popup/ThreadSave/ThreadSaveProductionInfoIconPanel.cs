using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ThreadSaveProductionInfoIconPanel : MonoBehaviour
{
    [SerializeField] private Image _image;
    [SerializeField] private TextMeshProUGUI _titleText;
    [SerializeField] private TextMeshProUGUI _countText;

    public void Init(ResourceEntry resourceEntry, int productionCount = -1)
    {
        _image.sprite = resourceEntry.data.icon;
        _titleText.text = resourceEntry.data.id.Localize(LocalizationUtils.TABLE_RESOURCE);

        if (productionCount >= 0)
        {
            _countText.text = $"{productionCount}";
        }
        else
        {
            _countText.text = $"-";
        }
    }
}
