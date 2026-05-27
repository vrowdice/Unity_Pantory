using UnityEngine;
using TMPro;

public class MainCanvasUnionContainer : BtnBase
{
    [SerializeField] private TextMeshProUGUI _unionCohesionProgressText;
    [SerializeField] private TextMeshProUGUI _remainDateText;

    private DataManager _dataManager;

    public void Init(MainCanvas mainCanvas)
    {
        _dataManager = mainCanvas.DataManager;
        RefreshUI();
    }

    public void RefreshUI()
    {
        UnionStateModule module = _dataManager?.MainEvent?.UnionModule;
        if (module == null)
        {
            gameObject.SetActive(false);
            return;
        }
        
        float cohesionProgress = Mathf.Clamp(module.UnionCohesionProgress, 0f, 100f);
        _unionCohesionProgressText.text = $"{Mathf.RoundToInt(cohesionProgress)}%";
        _remainDateText.text = $"{module.RemainingDays}";
    }

    protected override void HandleClick()
    {
        UIManager.Instance?.ShowUnionPopup();
    }
}
