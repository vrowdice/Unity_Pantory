using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// 연구 관리 패널
/// </summary>
public class ResearchPanel : BasePanel
{
    private List<ResearchTierPanel> _researchTierPanels = new List<ResearchTierPanel>();

    [SerializeField] private TextMeshProUGUI _researchText;
    [SerializeField] private TextMeshProUGUI _deltaResearchText;
    [SerializeField] private TextMeshProUGUI _researcherText;

    [SerializeField] private GameObject _researchTierPanelPrefab;
    [SerializeField] private Transform _researchTirePanelContentTransform;

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

    public void UpdateResearchScrollView()
    {
        GameObjectUtils.ClearChildren(_researchTirePanelContentTransform);

        int maxTier = 0;
        maxTier = _dataManager.InitialResearchData.maxTier;

        for (int i = 1; i <= maxTier; i++)
        {
            List<ResearchEntry> entrys = _dataManager.Research.GetResearchEntriesByTier(i);
            GameObject researchPanelObj = Instantiate(_researchTierPanelPrefab, _researchTirePanelContentTransform);
            ResearchTierPanel _researchPanel = researchPanelObj.GetComponent<ResearchTierPanel>();
            _researchPanel.OnInitialize(i, entrys);
        }
    }
}