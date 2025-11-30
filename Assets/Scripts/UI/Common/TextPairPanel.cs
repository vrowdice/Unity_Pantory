using UnityEngine;
using TMPro;

public class TextPairPanel : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _mainText;
    [SerializeField] private TextMeshProUGUI _secondText;

    public void OnInitialize(string mainText, string secondText)
    {
        _mainText.text = mainText;
        _secondText.text = secondText;
    }
}
