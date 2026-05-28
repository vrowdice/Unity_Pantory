using UnityEngine;
using TMPro;
using System;

public class ActionBtn : BtnBase
{
    [SerializeField] private GameObject _selectedImage;
    [SerializeField] private TextMeshProUGUI _text;

    private string _label = string.Empty;
    private Action _onClick;

    public void Init(string label, Action onClick)
    {
        _label = label ?? string.Empty;
        _onClick = onClick;

        if (_text != null)
        {
            DisableTextLocalization(_text);
            if (!string.IsNullOrEmpty(_label))
            {
                _text.SetText(_label);
            }
        }

        EnsureClickBound();
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        if (_text != null && !string.IsNullOrEmpty(_label))
        {
            _text.SetText(_label);
        }
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

    private static void DisableTextLocalization(TextMeshProUGUI text)
    {
        MonoBehaviour[] behaviours = text.GetComponents<MonoBehaviour>();
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] != null && behaviours[i].GetType().Name == "LocalizeStringEvent")
            {
                behaviours[i].enabled = false;
            }
        }
    }
}
