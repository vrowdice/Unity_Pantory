using UnityEngine;
using UnityEngine.Events;

public abstract class BtnBase : UITweenEffectBase
{
    [SerializeField] private Evo.UI.Button _button;

    protected override bool PlayEffectOnEnable => true;

    protected override void OnEnable()
    {
        base.OnEnable();
        EnsureClickBound();
    }

    protected Evo.UI.Button ResolveButton()
    {
        if (_button != null)
        {
            return _button;
        }

        Evo.UI.Button button = GetComponent<Evo.UI.Button>();
        if (button != null)
        {
            return button;
        }

        button = GetComponentInParent<Evo.UI.Button>();
        if (button != null)
        {
            return button;
        }

        return GetComponentInChildren<Evo.UI.Button>(true);
    }

    protected void EnsureClickBound()
    {
        Evo.UI.Button button = ResolveButton();
        if (button == null)
        {
            return;
        }

        BindClick(button, OnClick);
    }

    protected void BindClick(Evo.UI.Button button, UnityAction handler)
    {
        if (button == null || handler == null)
        {
            return;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(handler);
    }

    public void OnClick()
    {
        HandleClick();
    }

    protected abstract void HandleClick();
}
