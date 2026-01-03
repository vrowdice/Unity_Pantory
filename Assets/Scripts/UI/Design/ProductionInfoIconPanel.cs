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
        _image.sprite = resourceEntry.data.icon;
        _titleText.text = resourceEntry.data.displayName;

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
