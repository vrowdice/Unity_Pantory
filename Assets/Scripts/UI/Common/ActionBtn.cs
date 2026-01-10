using UnityEngine;
using TMPro;
using System;

public class ActionBtn : MonoBehaviour
{
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
}
