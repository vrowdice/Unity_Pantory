using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEditor.Rendering;

public partial class MainCanvas : CanvasBase
{
    [Header("Information")]
    [SerializeField] private TextMeshProUGUI _creditText;
    [SerializeField] private TextMeshProUGUI _deltaCreditText;
    [SerializeField] private TextMeshProUGUI _researchText;
    [SerializeField] private TextMeshProUGUI _deltaResearchText;

    [SerializeField] private DateTopInfoPanel _infoDatePanel;
    [SerializeField] private TopInfoPanel _topInfoPanel;

    [Header("Info Panel")]
    [SerializeField] private ThreadInfoPanel _threadInfoPanel;
    [SerializeField] private ResearchInfoPanel _researchInfoPanel;

    public MainRunner ThreadTileManager => _threadTileManager;

    public void Init(MainRunner mainRunner)
    {
        base.Init();

        DataManager.Resource.OnResourceChanged -= UpdateAllMainText;
        DataManager.Finances.OnCreditChanged -= UpdateAllMainText;
        DataManager.Research.OnResearchPointsChanged -= UpdateAllMainText;
        DataManager.Thread.OnThreadChanged -= OnThreadPlacementChanged;

        DataManager.Time.OnDayChanged -= OnDayChanged;
        DataManager.Time.OnMonthChanged -= OnMonthChanged;
        DataManager.Time.OnYearChanged -= OnYearChanged;

        DataManager.Resource.OnResourceChanged += UpdateAllMainText;
        DataManager.Finances.OnCreditChanged += UpdateAllMainText;
        DataManager.Research.OnResearchPointsChanged += UpdateAllMainText;
        DataManager.Thread.OnThreadChanged += OnThreadPlacementChanged;

        DataManager.Time.OnDayChanged += OnDayChanged;
        DataManager.Time.OnMonthChanged += OnMonthChanged;
        DataManager.Time.OnYearChanged += OnYearChanged;

        _infoDatePanel.Init(DataManager);
        _creditInfoPanel.Init(DataManager);
        _topInfoPanel.Init(DataManager);

        InitializePanelDictionary();
        InitializePanels();
        CreateQuickMoveBtns();
        UpdateAllMainText();

        RefreshThreadCategories();
        RefreshThreadButtons();
        RefreshResourceScrollView();
        UpdateAllMainText();
    }

    override public void UpdateAllMainText()
    {
        UpdateCreditText();
        UpdateResearchText();
    }

    private void UpdateCreditText()
    {
        VisualManager visualManager = VisualManager.Instance;

        long resourceAmount = DataManager.Finances.Credit;
        _creditText.text = ReplaceUtils.FormatNumberWithCommas(resourceAmount);
        long deltaCredit = DataManager.Finances.CalculateDailyCreditDelta();
        if (deltaCredit == 0)
        {
            _deltaCreditText.text = "";
            return;
        }

        string sign = deltaCredit > 0 ? " +" : " ";
        _deltaCreditText.text = $"{sign}{ReplaceUtils.FormatNumberWithCommas(deltaCredit)}";
        _deltaCreditText.color = visualManager.GetDeltaColor(deltaCredit);
    }

    private void UpdateResearchText()
    {
        VisualManager visualManager = VisualManager.Instance;

        long researchPoints = DataManager.Research.ResearchPoint;
        _researchText.text = ReplaceUtils.FormatNumberWithCommas(researchPoints);
        long deltaResearch = DataManager.Research.CalculateDailyRPProduction();
        if (deltaResearch == 0)
        {
            _deltaResearchText.text = "";
            return;
        }

        string sign = deltaResearch > 0 ? " + " : " ";
        _deltaResearchText.text = $"{sign}{ReplaceUtils.FormatNumberWithCommas(deltaResearch)}";
        _deltaResearchText.color = visualManager.GetDeltaColor(deltaResearch);
    }

    private void OnMonthChanged()
    {
        Debug.Log("[MainUiManager] Month changed event received.");
    }

    private void OnYearChanged()
    {
        Debug.Log("[MainUiManager] Year changed event received.");
    }

    private void OnDayChanged()
    {
        RefreshResourceScrollView();
        UpdateAllMainText();
    }

    private void OnThreadPlacementChanged()
    {
        RefreshResourceScrollView();
        UpdateAllMainText();
    }

    /// <summary>
    /// 크레딧 정보 패널을 토글합니다.
    /// </summary>
    public void ToggleCreditInfo()
    {
        _creditInfoPanel.ToggleCreditInfo();
    }

    /// <summary>
    /// FinancesDataHandler에서 크레딧 정보를 가져옵니다.
    /// </summary>
    /// <returns>크레딧 정보가 포함된 FinancesDataHandler, 없으면 null</returns>
    public FinancesDataHandler GetFinancesDataHandler()
    {
        return DataManager.Finances;
    }

    /// <summary>
    /// 스레드 정보 패널을 표시합니다.
    /// </summary>
    public void ShowThreadInfoPanel(ThreadState threadState)
    {
        _threadInfoPanel.Init(threadState, this);
    }

    public void ShowResearchInfoPanel(ResearchEntry researchEntry)
    {
        _researchInfoPanel.Init(researchEntry, this);
    }

    public void ShowOptionPanel()
    {
        GameManager.ShowOptionPanel();
    }
}
