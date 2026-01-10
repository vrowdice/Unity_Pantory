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

    public void Init(string mainText, string secondText, float numericValue)
    {
        _mainText.text = mainText;
        _secondText.text = secondText;
        
        // VisualManager를 사용하여 색상 설정 (숫자 값 기준)
        if (VisualManager.Instance != null)
        {
            _secondText.color = VisualManager.Instance.GetDeltaColor(numericValue);
        }
    }

    public void Init(string mainText, int secondTextValue)
    {
        _mainText.text = mainText;
        _secondText.text = secondTextValue.ToString();
        
        // VisualManager를 사용하여 색상 설정
        if (VisualManager.Instance != null)
        {
            _secondText.color = VisualManager.Instance.GetDeltaColor(secondTextValue);
        }
    }

    public void Init(string mainText, float secondTextValue)
    {
        _mainText.text = mainText;
        _secondText.text = secondTextValue.ToString("F1");
        
        // VisualManager를 사용하여 색상 설정
        if (VisualManager.Instance != null)
        {
            _secondText.color = VisualManager.Instance.GetDeltaColor(secondTextValue);
        }
    }

    public void Init(string mainText, int secondTextValue, Color color)
    {
        _mainText.text = mainText;
        _secondText.text = secondTextValue.ToString();
        _secondText.color = color;
    }
}
