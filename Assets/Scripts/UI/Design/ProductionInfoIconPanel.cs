using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ProductionInfoIconPanel : MonoBehaviour
{
    [SerializeField] private Image _image;
    [SerializeField] private TextMeshProUGUI _titleText;
    [SerializeField] private TextMeshProUGUI _countText;

    public void OnInitialize(ResourceEntry resourceEntry, int productionCount = -1)
    {
        _image.sprite = resourceEntry.resourceData.icon;
        _titleText.text = resourceEntry.resourceData.displayName;

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
