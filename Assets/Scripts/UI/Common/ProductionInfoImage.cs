using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;
public class ProductionInfoImage : MonoBehaviour
{
    [SerializeField] private Image _productionImage;
    [SerializeField] private TextMeshProUGUI _productionText;
    [SerializeField] private TextMeshProUGUI _productionCountText;

    public void OnInitialize(ResourceEntry resourceEntry)
    {
        OnInitialize(resourceEntry, -1);
    }

    public void OnInitialize(ResourceEntry resourceEntry, int amount)
    {
        _productionImage.sprite = resourceEntry.resourceData.icon;
        _productionText.text = resourceEntry.resourceData.displayName;

        if (amount >= 0)
        {
            _productionCountText.text = $"{amount}";
        }
        else
        {
            _productionCountText.text = $"-";
        }
    }
}
