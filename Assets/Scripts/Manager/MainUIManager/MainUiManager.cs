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
    [SerializeField] private EmployeePanel _employmentPanel;
    [SerializeField] private FinancePanel _financePanel;
    [SerializeField] private CreditInfoPanel _creditInfoPanel;

    [Header("Quick Move")]
    [SerializeField] private GameObject _quickMoveBtnPrefeb;
    [SerializeField] private Transform _quickMovePanelContent;
    [SerializeField] private PanelDoAni _quickMovePanelAni;

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
            _dataManager.OnCreditChanged += UpdateAllMainText;
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

        if (_creditInfoPanel != null)
        {
            _creditInfoPanel.OnInitialize(_dataManager);
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
    }

    private void OnDestroy()
    {
        if (_dataManager != null)
        {
            _dataManager.OnResourceChanged -= UpdateAllMainText;
            _dataManager.OnCreditChanged -= UpdateAllMainText;
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

        long resourceAmount = _dataManager.Finances.GetCredit();
        textComponent.text = ReplaceUtils.FormatNumberWithCommas(resourceAmount);
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
        
        if (deltaCredit == 0)
        {
            textComponent.text = "";
            return;
        }

        // 적자면 빨간색, 흑자면 파란색
        string sign = deltaCredit > 0 ? " +" : " ";
        textComponent.text = $"{sign}{ReplaceUtils.FormatNumberWithCommas(deltaCredit)} /day";
        
        // VisualManager에서 색상 가져오기
        VisualManager visualManager = VisualManager.Instance;
        if (visualManager != null)
        {
            textComponent.color = visualManager.GetDeltaColor(deltaCredit);
        }
        else
        {
            // VisualManager가 없을 경우 기본값 반환
            textComponent.color = Color.white;
        }
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
}
