using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public partial class MainUiManager : MonoBehaviour, IUIManager
{
    [Header("References")]
    private GameManager _gameManager;
    private GameDataManager _dataManager;

    [Header("Managers")]
    [SerializeField] private ThreadTileManager _threadTileManager;

    [Header("Information")]
    [SerializeField] private TextMeshProUGUI _creditText;
    [SerializeField] private TextMeshProUGUI _deltaCreditText;
    [SerializeField] private InfoDatePanel _infoDatePanel;

    [Header("Panels")]
    [SerializeField] private StoragePanel _storagePanel;
    [SerializeField] private MarketPanel _marketPanel;
    [SerializeField] private EmploymentPanel _employmentPanel;
    [SerializeField] private FinancePanel _financePanel;

    [Header("Quick Move")]
    [SerializeField] private GameObject _quickMoveBtnPrefeb;
    [SerializeField] private Transform _quickMovePanelContent;

    [Header("Resouce ScrollView")]
    [SerializeField] private GameObject _mainScrollViewResouceBtn;
    [SerializeField] private Transform _resouceScrollViewContent;

    [Header("Thread")]
    [SerializeField] private Image _cancelPlacementBtnImage;
    [SerializeField] private Image _removalModeBtnImage;
    [SerializeField] private GameObject _threadCategoryBtnPrefab;
    [SerializeField] private Transform _threadCategoryScrollViewContent;
    [SerializeField] private GameObject _threadBtnPrefab;
    [SerializeField] private GameObject _threadPlusBtnPrefab;
    [SerializeField] private Transform _threadScrollViewContent;

    private GameObject _productionInfoImage;
    
    // Panels
    private Dictionary<MainPanelType, BasePanel> _panelDict;
    private MainPanelType _currentOpenPanelType;
    
    // Quick Move
    private readonly List<QuickMoveBtn> _quickMoveBtns = new List<QuickMoveBtn>();
    
    // Resources
    private readonly List<MainScrollViewResouceBtn> _resourceBtns = new List<MainScrollViewResouceBtn>();
    
    // Threads
    private readonly List<ThreadCategoryBtn> _threadCategoryBtns = new List<ThreadCategoryBtn>();
    private readonly List<ThreadBtn> _threadBtns = new List<ThreadBtn>();
    private string _selectedThreadCategoryId = string.Empty;

    public Transform CanvasTrans => transform;
    public GameDataManager DataManager => _dataManager;
    public GameObject ProductionInfoImage => _productionInfoImage;
    public ThreadTileManager ThreadTileManager => _threadTileManager;

    public void Initialize(GameManager argGameManager, GameDataManager argGameDataManager)
    {
        _gameManager = argGameManager;
        _dataManager = argGameDataManager;
        _productionInfoImage = argGameManager.ProductionInfoImage;

        if (_threadTileManager == null)
        {
            Debug.LogWarning("[MainUiManager] ThreadTileManager is not assigned.");
        }

        if (_dataManager != null)
        {
            _dataManager.OnResourceChanged += UpdateAllMainText;
            _dataManager.OnThreadPlacementChanged += OnThreadPlacementChanged;

            _dataManager.Time.OnDayChanged += OnDayChanged;
            _dataManager.Time.OnMonthChanged += OnMonthChanged;
            _dataManager.Time.OnYearChanged += OnYearChanged;
        }

        if (_infoDatePanel != null)
        {
            _infoDatePanel.OnInitialize(_dataManager);
        }
        else
        {
            Debug.LogWarning("[MainUiManager] InfoDatePanel is not assigned.");
        }

        RefreshThreadCategories();
        RefreshThreadButtons();
        UpdateResourceSummary();
        
        // 초기화 시 크레딧 변화량 표시
        UpdateDeltaCreditText(_deltaCreditText);
    }

    private void Awake()
    {
        InitializePanelDictionary();
    }

    private void Start()
    {
        InitializePanels();
        CreateQuickMoveBtns();
        UpdateAllMainText();

        RefreshThreadCategories();
        RefreshThreadButtons();
        UpdateResourceSummary();
    }

    public void UpdateAllMainText()
    {
        if (_dataManager == null)
        {
            Debug.LogWarning("[MainUiManager] DataManager is null. Cannot update main text.");
            return;
        }

        UpdateCreditText(_creditText);
        UpdateDeltaCreditText(_deltaCreditText);
        UpdateResourceSummary();

        Debug.Log("[MainUiManager] All main text updated.");
    }

    private void OnDestroy()
    {
        if (_dataManager != null)
        {
            _dataManager.OnResourceChanged -= UpdateAllMainText;
            _dataManager.OnThreadPlacementChanged -= OnThreadPlacementChanged;

            _dataManager.Time.OnDayChanged -= OnDayChanged;
            _dataManager.Time.OnMonthChanged -= OnMonthChanged;
            _dataManager.Time.OnYearChanged -= OnYearChanged;
        }
    }

    private void UpdateCreditText(TextMeshProUGUI textComponent)
    {
        if (textComponent == null)
        {
            Debug.LogWarning("[MainUiManager] Text component for Credit is null.");
            return;
        }

        long resourceAmount = _dataManager.GetCredit();
        textComponent.text = FormatResourceAmount(resourceAmount);
    }

    private void UpdateDeltaCreditText(TextMeshProUGUI textComponent)
    {
        if (textComponent == null)
        {
            Debug.LogWarning("[MainUiManager] DeltaCreditText component is null.");
            return;
        }

        if (_dataManager == null)
        {
            Debug.LogWarning("[MainUiManager] DataManager is null. Cannot update delta credit text.");
            textComponent.text = "";
            return;
        }

        long deltaCredit = _dataManager.CalculateDailyCreditDelta();
        Debug.Log($"[MainUiManager] Daily credit delta calculated: {deltaCredit}");
        
        if (deltaCredit == 0)
        {
            textComponent.text = "";
            return;
        }

        // 적자면 빨간색, 흑자면 파란색
        string sign = deltaCredit > 0 ? "+" : "";
        textComponent.text = $"{sign}{FormatResourceAmount(deltaCredit)}/day";
        
        VisualManager visualManager = VisualManager.Instance;
        if (deltaCredit < 0)
        {
            textComponent.color = visualManager != null ? visualManager.LossColor : Color.red; // 적자
        }
        else
        {
            textComponent.color = visualManager != null ? visualManager.ProfitColor : Color.blue; // 흑자
        }
        
        Debug.Log($"[MainUiManager] Delta credit text updated: {textComponent.text}, color: {textComponent.color}");
    }

    private string FormatResourceAmount(long amount)
    {
        return amount.ToString("N0");
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
        UpdateResourceSummary();
        UpdateDeltaCreditText(_deltaCreditText);
    }

    private void OnThreadPlacementChanged()
    {
        UpdateResourceSummary();
        UpdateDeltaCreditText(_deltaCreditText);
    }
}
