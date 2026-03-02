using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// 연구 관리 패널
/// </summary>
public class ResearchCanvas : MainCanvasPanelBase
{
    [SerializeField] private TextMeshProUGUI _researchText;
    [SerializeField] private TextMeshProUGUI _deltaResearchText;
    [SerializeField] private TextMeshProUGUI _researcherText;

    [SerializeField] private GameObject _researchBtnContainerPrefab;
    [SerializeField] private Transform _researchBtnContainerContentTransform;

    public override void Init(MainCanvas argUIManager)
    {
        base.Init(argUIManager);

        _dataManager.Research.OnResearchPointsChanged -= ResearchChanged;
        _dataManager.Research.OnResearchPointsChanged += ResearchChanged;

        _dataManager.Time.OnDayChanged -= ResearchChanged;
        _dataManager.Time.OnDayChanged += ResearchChanged;

        UpdateAllText();
        UpdateResearchScrollView();
    }

    public void ResearchChanged()
    {
        UpdateAllText();
        UpdateResearchScrollView();
    }

    private void UpdateAllText()
    {
        long researchPoints = _dataManager.Research.ResearchPoint;
        _researchText.text = ReplaceUtils.FormatNumberWithCommas(researchPoints);
        long deltaResearch = _dataManager.Research.CalculateDailyRPProduction();
        string sign = deltaResearch > 0 ? " + " : " ";
        _deltaResearchText.text = $"{sign}{ReplaceUtils.FormatNumberWithCommas(deltaResearch)}";
        VisualManager visualManager = VisualManager.Instance;
        _deltaResearchText.color = visualManager.GetDeltaColor(deltaResearch);
        _researcherText.text = _dataManager.Employee.GetAvailableEmployeeCount(EmployeeType.Researcher).ToString();
    }

    public void UpdateResearchScrollView()
    {
        GameObjectUtils.ClearChildren(_researchBtnContainerContentTransform);

        GameObject researchBtnContainer = Instantiate(_researchBtnContainerPrefab, _researchBtnContainerContentTransform);
    }
}