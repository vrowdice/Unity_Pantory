using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;
public class ProductionInfoImage : MonoBehaviour
{
    [SerializeField] private Image _productionImage;
    [SerializeField] private TextMeshProUGUI _productionText;
    [SerializeField] private TextMeshProUGUI _productionValueText;

    public void OnInitialize(ResourceEntry resourceEntry)
    {
        _productionImage.sprite = resourceEntry.resourceData.icon;
        _productionText.text = resourceEntry.resourceData.description;
        _productionValueText.text = resourceEntry.resourceState.count.ToString();
    }
}
