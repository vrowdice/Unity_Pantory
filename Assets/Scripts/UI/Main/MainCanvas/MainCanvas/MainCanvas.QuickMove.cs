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
        foreach (QuickMoveBtn btn in _quickMoveBtns)
            GameManager.PoolingManager.ReturnToPool(btn.gameObject);
        _quickMoveBtns.Clear();

        foreach (MainPanelType panelType in System.Enum.GetValues(typeof(MainPanelType)))
        {
            GameObject btnObj = GameManager.PoolingManager.GetPooledObject(_quickMoveBtnPrefeb);
            btnObj.transform.SetParent(_quickMovePanelContent, false);

            QuickMoveBtn quickBtn = btnObj.GetComponent<QuickMoveBtn>();
            quickBtn.Initialize(this, panelType);
            _quickMoveBtns.Add(quickBtn);
        }
    }
}
