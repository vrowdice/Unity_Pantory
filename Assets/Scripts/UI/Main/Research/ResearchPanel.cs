using UnityEngine;
using TMPro;

/// <summary>
/// 연구 관리 패널
/// </summary>
public class ResearchPanel : BasePanel
{
    [SerializeField] private TextMeshProUGUI _researchText;
    [SerializeField] private TextMeshProUGUI _deltaResearchText;
    [SerializeField] private TextMeshProUGUI _researcherText;
    [SerializeField] private GameObject _researchTierPanelPrefab;

    protected override void OnInitialize()
    {
        if (_dataManager == null)
        {
            Debug.LogWarning("[ResearchPanel] DataManager is null.");
            return;
        }

        _dataManager.Research.OnResearchPointsChanged += ResearchChanged;
        _dataManager.Time.OnDayChanged += ResearchChanged;

        UpdateAllText();
    }

    public void ResearchChanged()
    {
        UpdateAllText();
    }

    private void UpdateAllText()
    {
        long researchPoints = _dataManager.Research.ResearchPoint;
        _researchText.text = ReplaceUtils.FormatNumberWithCommas(researchPoints);
        long deltaResearch = _dataManager.Research.CalculateDailyRPProduction();
        if (deltaResearch == 0)
        {
            _deltaResearchText.text = "";
            return;
        }

        string sign = deltaResearch > 0 ? " + " : " ";
        _deltaResearchText.text = $"{sign}{ReplaceUtils.FormatNumberWithCommas(deltaResearch)}";
        VisualManager visualManager = VisualManager.Instance;
        _deltaResearchText.color = visualManager.GetDeltaColor(deltaResearch);

        _researcherText.text = _dataManager.Employee.GetAvailableEmployeeCount(EmployeeType.Researcher).ToString();
    }
}