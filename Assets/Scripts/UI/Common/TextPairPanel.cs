using UnityEngine;
using TMPro;

public class TextPairPanel : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _mainText;
    [SerializeField] private TextMeshProUGUI _secondText;

    public void Init(string mainText, string secondText)
    {
        _mainText.text = mainText;
        _secondText.text = secondText;
    }

    public void Init(string mainText, string secondText, float numericValue, Color mainTextColor = default)
    {
        if (mainTextColor == default) mainTextColor = Color.white;

        _mainText.text = mainText;
        _mainText.color = mainTextColor;
        _secondText.text = secondText;

        if (VisualManager.Instance != null)
        {
            _secondText.color = VisualManager.Instance.GetDeltaColor(numericValue);
        }
    }
}
