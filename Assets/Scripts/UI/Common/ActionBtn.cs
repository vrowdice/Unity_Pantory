using UnityEngine;
using TMPro;
using System;

public class ActionBtn : BtnBase
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
        EnsureClickBound();
    }

    protected override void HandleClick()
    {
        _onClick?.Invoke();
    }

    public void SetHighlight(bool isHighlight)
    {
        if (_selectedImage != null)
        {
            _selectedImage.SetActive(isHighlight);
        }
    }
}
