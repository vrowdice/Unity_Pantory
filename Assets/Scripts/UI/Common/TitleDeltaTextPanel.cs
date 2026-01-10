using UnityEngine;
using TMPro;

public class TitleDeltaTextPanel : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _titleText = null;
    [SerializeField] private TextMeshProUGUI _deltaText = null;

    /// <summary>
    /// 제목과 델타 값을 초기화합니다. 델타 값에 따라 색상과 + - 기호가 자동으로 설정됩니다.
    /// </summary>
    /// <param name="titleText">제목 텍스트</param>
    /// <param name="deltaValue">델타 값 (양수: +, 음수: -, 0: 변화없음)</param>
    public void Init(string titleText, float deltaValue)
    {
        if (_titleText != null)
        {
            _titleText.text = titleText;
        }

        if (_deltaText != null)
        {
            // + - 기호와 값 표시 (쉼표 포맷 적용)
            long longValue = (long)deltaValue;
            string formattedValue = ReplaceUtils.FormatNumberWithCommas(longValue);
            
            string deltaText = deltaValue > 0f 
                ? $"+{formattedValue}" 
                : deltaValue < 0f 
                ? $"{formattedValue}" 
                : "0";
            
            _deltaText.text = deltaText;
            
            // VisualManager에서 색상 가져오기
            VisualManager visualManager = VisualManager.Instance;
            if (visualManager != null)
            {
                _deltaText.color = visualManager.GetDeltaColor(deltaValue);
            }
            else
            {
                _deltaText.color = Color.white;
            }
        }
    }

    /// <summary>
    /// 제목과 델타 텍스트를 직접 초기화합니다 (기존 방식 호환성)
    /// </summary>
    /// <param name="titleText">제목 텍스트</param>
    /// <param name="deltaText">델타 텍스트</param>
    public void Init(string titleText, string deltaText)
    {
        if (_titleText != null)
        {
            _titleText.text = titleText;
        }

        if (_deltaText != null)
        {
            _deltaText.text = deltaText;
            _deltaText.color = Color.white;
        }
    }
}
