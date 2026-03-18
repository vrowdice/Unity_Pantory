using System.Collections.Generic;
using UnityEngine;

public partial class MainCanvas
{
    [Header("Quick Move")]
    [SerializeField] private GameObject _quickMoveBtnPrefeb;
    [SerializeField] private Transform _quickMovePanelContent;

    private readonly List<QuickMoveBtn> _quickMoveBtns = new List<QuickMoveBtn>();

    private void CreateQuickMoveBtns()
    {
        if (_quickMoveBtnPrefeb == null)
        {
            Debug.LogWarning("[MainUiManager] QuickMoveBtn prefab is null.");
            return;
        }

        if (_quickMovePanelContent == null)
        {
            Debug.LogWarning("[MainUiManager] QuickMovePanel content is null.");
            return;
        }

        foreach (QuickMoveBtn btn in _quickMoveBtns)
        {
            if (btn != null)
            {
                Destroy(btn.gameObject);
            }
        }
        _quickMoveBtns.Clear();

        foreach (MainPanelType panelType in System.Enum.GetValues(typeof(MainPanelType)))
        {
            GameObject btnObj = Instantiate(_quickMoveBtnPrefeb, _quickMovePanelContent);

            QuickMoveBtn btn = btnObj.GetComponent<QuickMoveBtn>();

            if (btn != null)
            {
                btn.Initialize(this, panelType);
                _quickMoveBtns.Add(btn);
            }
            else
            {
                Debug.LogError("[MainUiManager] QuickMoveBtn component not found on prefab.");
                Destroy(btnObj);
            }
        }
    }
}
