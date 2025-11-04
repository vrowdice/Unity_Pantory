using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ProductionInfoIconPanel : MonoBehaviour
{
    [SerializeField] private Image _productionImage;
    [SerializeField] private TextMeshProUGUI _productionText;
    [SerializeField] private TextMeshProUGUI _productionCountText;

    public void OnInitialize(ResourceEntry resourceEntry, int productionCount = -1)
    {
        _productionImage.sprite = resourceEntry.resourceData.icon;
        _productionText.text = resourceEntry.resourceData.displayName;

        if (productionCount == -1)
        {
            _productionCountText.text = "Count: " + resourceEntry.resourceState.count.ToString();
        }
        else
        {
            _productionCountText.text = "Production: " + productionCount.ToString();
        }
    }
}
