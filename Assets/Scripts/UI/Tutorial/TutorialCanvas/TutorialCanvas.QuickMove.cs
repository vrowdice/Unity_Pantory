using System.Collections.Generic;
using UnityEngine;

public partial class TutorialCanvas
{
    private static readonly MainPanelType[] TutorialQuickMovePanels =
    {
        MainPanelType.Market
    };

    [Header("Quick Move")]
    [SerializeField] private GameObject _quickMoveBtnPrefeb;
    [SerializeField] private Transform _quickMovePanelContent;

    private readonly List<QuickMoveBtn> _quickMoveBtns = new List<QuickMoveBtn>();

    private void CreateQuickMoveBtns()
    {
        foreach (QuickMoveBtn btn in _quickMoveBtns)
            GameManager.PoolingManager.ReturnToPool(btn.gameObject);
        _quickMoveBtns.Clear();

        for (int i = 0; i < TutorialQuickMovePanels.Length; i++)
        {
            MainPanelType panelType = TutorialQuickMovePanels[i];
            GameObject btnObj = GameManager.PoolingManager.GetPooledObject(_quickMoveBtnPrefeb);
            btnObj.transform.SetParent(_quickMovePanelContent, false);

            QuickMoveBtn quickBtn = btnObj.GetComponent<QuickMoveBtn>();
            quickBtn.Init(this, panelType);
            _quickMoveBtns.Add(quickBtn);
        }
    }

    public QuickMoveBtn FindQuickMoveButton(MainPanelType panelType)
    {
        for (int i = 0; i < _quickMoveBtns.Count; i++)
        {
            if (_quickMoveBtns[i].PanelType == panelType)
                return _quickMoveBtns[i];
        }

        return null;
    }
}
