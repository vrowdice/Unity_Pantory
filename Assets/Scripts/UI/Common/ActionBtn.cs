using UnityEngine;
using TMPro;
using System;

public class ActionBtn : MonoBehaviour
{
    [SerializeField] private GameObject _selectedImage;
    [SerializeField] private TextMeshProUGUI _text;

    private Action _onClick;

    public void Init(string label, Action onClick)
    {
        if (_text != null && !string.IsNullOrEmpty(label))
        {
            _text.SetText(label);
        }

        _onClick = onClick;
    }

    public void OnClick()
    {
        _onClick?.Invoke();
    }

    /// <summary>
    /// 버튼의 하이라이트 상태를 설정합니다.
    /// </summary>
    /// <param name="isHighlight">하이라이트 여부</param>
    public void SetHighlight(bool isHighlight)
    {
        if (_selectedImage != null)
        {
            _selectedImage.SetActive(isHighlight);
        }
    }
}
