using System.Collections.Generic;
using TMPro;
using UnityEngine;

public partial class MainUiManager : MonoBehaviour, IUIManager
{
    [Header("References")]
    private GameManager _gameManager;
    private GameDataManager _dataManager;

    [Header("Managers")]
    [SerializeField] private ThreadTileManager _threadTileManager;

    [Header("Information")]
    [SerializeField] private TextMeshProUGUI _creditText;
    [SerializeField] private InfoDatePanel _infoDatePanel;

    private GameObject _productionInfoImage;

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
            _dataManager.OnThreadPlacementChanged += UpdateResourceSummary;

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
        UpdateResourceSummary();

        Debug.Log("[MainUiManager] All main text updated.");
    }

    private void OnDestroy()
    {
        if (_dataManager != null)
        {
            _dataManager.OnResourceChanged -= UpdateAllMainText;
            _dataManager.OnThreadPlacementChanged -= UpdateResourceSummary;

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
}
