using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEditor.Rendering;

public partial class MainUiManager : MonoBehaviour, IUIManager
{
    [Header("References")]
    private GameManager _gameManager;
    private DataManager _dataManager;

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

    private GameObject _productionInfoImage;
    public Transform CanvasTrans => transform;
    public GameManager GameManager => _gameManager;
    public DataManager DataManager => _dataManager;
    public GameObject ProductionInfoImage => _productionInfoImage;
    public ThreadTileManager ThreadTileManager => _threadTileManager;

    public void OnInitialize(GameManager argGameManager, DataManager argGameDataManager)
    {
        _gameManager = argGameManager;
        _dataManager = argGameDataManager;
        _productionInfoImage = argGameManager.ProductionInfoImage;

        _dataManager.Resource.OnResourceChanged -= UpdateAllMainText;
        _dataManager.Finances.OnCreditChanged -= UpdateAllMainText;
        _dataManager.Research.OnResearchPointsChanged -= UpdateAllMainText;
        _dataManager.Thread.OnThreadChanged -= OnThreadPlacementChanged;

        _dataManager.Time.OnDayChanged -= OnDayChanged;
        _dataManager.Time.OnMonthChanged -= OnMonthChanged;
        _dataManager.Time.OnYearChanged -= OnYearChanged;

        _dataManager.Resource.OnResourceChanged += UpdateAllMainText;
        _dataManager.Finances.OnCreditChanged += UpdateAllMainText;
        _dataManager.Research.OnResearchPointsChanged += UpdateAllMainText;
        _dataManager.Thread.OnThreadChanged += OnThreadPlacementChanged;

        _dataManager.Time.OnDayChanged += OnDayChanged;
        _dataManager.Time.OnMonthChanged += OnMonthChanged;
        _dataManager.Time.OnYearChanged += OnYearChanged;

        _infoDatePanel.OnInitialize(_dataManager);
        _creditInfoPanel.OnInitialize(_dataManager);
        _topInfoPanel.OnInitialize(_dataManager);

        InitializePanelDictionary();
        InitializePanels();
        CreateQuickMoveBtns();
        UpdateAllMainText();

        RefreshThreadCategories();
        RefreshThreadButtons();
        RefreshResourceScrollView();
        UpdateAllMainText();
    }

    public void UpdateAllMainText()
    {
        UpdateCreditText();
        UpdateResearchText();
    }

    private void UpdateCreditText()
    {
        long resourceAmount = _dataManager.Finances.GetCredit();
        _creditText.text = ReplaceUtils.FormatNumberWithCommas(resourceAmount);
        long deltaCredit = _dataManager.Finances.CalculateDailyCreditDelta();
        if (deltaCredit == 0)
        {
            _deltaCreditText.text = "";
            return;
        }

        string sign = deltaCredit > 0 ? " +" : " ";
        _deltaCreditText.text = $"{sign}{ReplaceUtils.FormatNumberWithCommas(deltaCredit)}";
        VisualManager visualManager = VisualManager.Instance;
        _deltaCreditText.color = visualManager.GetDeltaColor(deltaCredit);
    }

    private void UpdateResearchText()
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
        if (_creditInfoPanel != null)
        {
            _creditInfoPanel.ToggleCreditInfo();
        }
        else
        {
            Debug.LogWarning("[MainUiManager] CreditInfoPanel is not assigned!");
        }
    }

    /// <summary>
    /// FinancesDataHandler에서 크레딧 정보를 가져옵니다.
    /// </summary>
    /// <returns>크레딧 정보가 포함된 FinancesDataHandler, 없으면 null</returns>
    public FinancesDataHandler GetFinancesDataHandler()
    {
        return _dataManager?.Finances;
    }

    /// <summary>
    /// 스레드 정보 패널을 표시합니다.
    /// </summary>
    public void ShowThreadInfoPanel(ThreadState threadState)
    {
        _threadInfoPanel.OnInitialize(threadState, this, _dataManager);
    }

    public void ShowResearchInfoPanel(ResearchEntry researchEntry)
    {
        _researchInfoPanel.OnInitialize(researchEntry, this, _dataManager);
    }
}
