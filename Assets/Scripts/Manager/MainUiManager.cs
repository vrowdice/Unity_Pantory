using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// 메인 UI 매니저
/// 패널들을 Enum으로 관리합니다.
/// </summary>
public class MainUiManager : MonoBehaviour, IUIManager
{
    [Header("References")]
    private GameManager _gameManager;
    private GameDataManager _dataManager;

    [Header("Information")]
    [SerializeField] private TextMeshProUGUI _creditText;
    [SerializeField] private InfoDatePanel _infoDatePanel;

    [Header("Panels")]
    [SerializeField] private StoragePanel _storagePanel;
    [SerializeField] private DesignPanel _designPanel;
    [SerializeField] private MarketPanel _marketPanel;
    [SerializeField] private EmploymentPanel _employmentPanel;


    [Header("Quick Move")]
    [SerializeField] private GameObject _quickMoveBtnPrefeb;
    [SerializeField] private Transform _quickMovePanelContent;

    // 패널 딕셔너리
    private Dictionary<MainPanelType, BasePanel> _panelDict;

    // 현재 열려있는 패널
    private MainPanelType _currentOpenPanelType;

    // 생성된 QuickMoveBtn 리스트
    private List<QuickMoveBtn> _quickMoveBtns = new List<QuickMoveBtn>();
    private GameObject _productionInfoImage = null;

    // IUIManager 인터페이스 구현
    public Transform CanvasTrans => transform;
    public GameDataManager DataManager => _dataManager;
    public GameObject ProductionInfoImage => _productionInfoImage;
    /// <summary>
    /// UI 매니저를 초기화합니다.
    /// </summary>
    public void Initialize(GameManager argGameManager, GameDataManager argGameDataManager)
    {
        _gameManager = argGameManager;
        _dataManager = argGameDataManager;
        _productionInfoImage = argGameManager.ProductionInfoImage;
        
        // 자원 변경 이벤트 구독
        if (_dataManager != null)
        {
            _dataManager.OnResourceChanged += UpdateAllMainText;
            
            // 시간 이벤트 구독
            _dataManager.Time.OnMonthChanged += OnMonthChanged;
            _dataManager.Time.OnYearChanged += OnYearChanged;
        }

        // InfoDatePanel 초기화
        if (_infoDatePanel != null)
        {
            _infoDatePanel.OnInitialize(_dataManager);
        }
        else
        {
            Debug.LogWarning("[MainUiManager] InfoDatePanel is not assigned.");
        }

        Debug.Log("[MainUiManager] Initialized.");
    }

    void Awake()
    {
        // 패널 딕셔너리 초기화
        InitializePanelDictionary();
    }

    /// <summary>
    /// 패널 딕셔너리를 초기화합니다.
    /// </summary>
    private void InitializePanelDictionary()
    {
        _panelDict = new Dictionary<MainPanelType, BasePanel>
        {
            { MainPanelType.Storage, _storagePanel },
            { MainPanelType.Design, _designPanel },
            { MainPanelType.Market, _marketPanel },
            { MainPanelType.Employment, _employmentPanel }
        };
    }

    void Start()
    {
        // 시작 시 모든 패널 초기화
        InitializePanels();

        // QuickMoveBtn 생성
        CreateQuickMoveBtns();

        // 자원 텍스트 초기 업데이트
        UpdateAllMainText();
    }

    /// <summary>
    /// 모든 메인 텍스트를 업데이트합니다.
    /// </summary>
    public void UpdateAllMainText()
    {
        // DataManager가 null이면 업데이트하지 않음
        if (_dataManager == null)
        {
            Debug.LogWarning("[MainUiManager] DataManager is null. Cannot update main text.");
            return;
        }
        
        UpdateCreditText(_creditText);

        Debug.Log("[MainUiManager] All main text updated.");
    }

    /// <summary>
    /// 특정 자원 텍스트를 업데이트합니다.
    /// </summary>
    private void UpdateCreditText(TextMeshProUGUI textComponent)
    {
        if (textComponent == null)
        {
            Debug.LogWarning($"[MainUiManager] Text component for Credit is null.");
            return;
        }

        long resourceAmount = _dataManager.GetCredit();
        textComponent.text = FormatResourceAmount(resourceAmount);
    }

    /// <summary>
    /// 자원 양을 포맷팅합니다 (예: 1000 -> 1,000).
    /// </summary>
    private string FormatResourceAmount(long amount)
    {
        return amount.ToString("N0");
    }

    /// <summary>
    /// 한 달이 지났을 때 호출됩니다.
    /// </summary>
    private void OnMonthChanged()
    {
        Debug.Log("[MainUiManager] Month changed event received.");
        // 추가적인 UI 업데이트나 효과 처리
    }

    /// <summary>
    /// 한 해가 지났을 때 호출됩니다.
    /// </summary>
    private void OnYearChanged()
    {
        Debug.Log("[MainUiManager] Year changed event received.");
        // 추가적인 UI 업데이트나 효과 처리
    }

    /// <summary>
    /// 모든 패널을 초기화합니다.
    /// </summary>
    private void InitializePanels()
    {
        // 모든 패널을 닫은 상태로 초기화
        foreach (var kvp in _panelDict)
        {
            if (kvp.Value != null)
            {
                kvp.Value.OnClose();
            }
        }
        
        Debug.Log("[MainUiManager] All panels initialized.");
    }

    /// <summary>
    /// 패널을 엽니다. 이전에 열린 패널은 자동으로 닫힙니다.
    /// </summary>
    public void OpenPanel(MainPanelType panelType)
    {
        if (!_panelDict.ContainsKey(panelType))
        {
            Debug.LogWarning($"[MainUiManager] Panel type {panelType} not found in dictionary.");
            return;
        }

        BasePanel panel = _panelDict[panelType];
        
        if (panel == null)
        {
            Debug.LogWarning($"[MainUiManager] Panel {panelType} is null.");
            return;
        }

        // 이전에 열려있던 패널 닫기 (같은 패널이어도 무조건 닫고 다시 엶)
        if (_panelDict.ContainsKey(_currentOpenPanelType))
        {
            ClosePanelInternal(_currentOpenPanelType);
        }

        // 새 패널 열기 (DataManager가 null이어도 패널은 열림)
        panel.OnOpen(_dataManager, this);
        _currentOpenPanelType = panelType;
        Debug.Log($"[MainUiManager] Panel {panelType} opened.");
    }

    /// <summary>
    /// 내부적으로 패널을 닫습니다 (OpenPanel에서만 사용).
    /// </summary>
    private void ClosePanelInternal(MainPanelType panelType)
    {
        if (!_panelDict.ContainsKey(panelType))
        {
            return;
        }

        BasePanel panel = _panelDict[panelType];
        
        if (panel == null)
        {
            return;
        }

        panel.OnClose();
        Debug.Log($"[MainUiManager] Panel {panelType} closed.");
    }

    public void CloseAllPanels()
    {
        foreach (var kvp in _panelDict)
        {
            ClosePanelInternal(kvp.Key);
        }
    }

    /// <summary>
    /// QuickMoveBtn을 생성합니다.
    /// </summary>
    private void CreateQuickMoveBtns()
    {
        if (_quickMoveBtnPrefeb == null)
        {
            Debug.LogWarning("[MainUiManager] QuickMoveBtn prefab is null.");
            return;
        }

        if (_quickMovePanelContent == null)
        {
            Debug.LogWarning("[MainUiManager] QuickMovePanel content is null.");
            return;
        }

        // 기존 버튼들 제거
        foreach (var btn in _quickMoveBtns)
        {
            if (btn != null)
            {
                Destroy(btn.gameObject);
            }
        }
        _quickMoveBtns.Clear();

        // 모든 MainPanelType에 대해 버튼 생성
        foreach (MainPanelType panelType in System.Enum.GetValues(typeof(MainPanelType)))
        {
            // 프리팹 인스턴스화
            GameObject btnObj = Instantiate(_quickMoveBtnPrefeb, _quickMovePanelContent);
            
            // QuickMoveBtn 컴포넌트 가져오기
            QuickMoveBtn btn = btnObj.GetComponent<QuickMoveBtn>();
            
            if (btn != null)
            {
                // 버튼 초기화
                btn.Initialize(this, panelType);
                _quickMoveBtns.Add(btn);
                
                Debug.Log($"[MainUiManager] QuickMoveBtn created for {panelType}");
            }
            else
            {
                Debug.LogError("[MainUiManager] QuickMoveBtn component not found on prefab.");
                Destroy(btnObj);
            }
        }

        Debug.Log($"[MainUiManager] Created {_quickMoveBtns.Count} QuickMoveBtn(s).");
    }

    /// <summary>
    /// 현재 열려있는 패널 타입을 반환합니다.
    /// </summary>
    public MainPanelType GetCurrentOpenPanelType()
    {
        return _currentOpenPanelType;
    }

    /// <summary>
    /// 특정 패널이 열려있는지 확인합니다.
    /// </summary>
    public bool IsPanelOpen(MainPanelType panelType)
    {
        return _currentOpenPanelType == panelType;
    }
/*
    void Update()
    {
        // 테스트용: 숫자 키로 패널 전환
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            OpenPanel(MainPanelType.Storage);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            OpenPanel(MainPanelType.Design);
        }
    }*/

    void OnDestroy()
    {
        // 자원 변경 이벤트 구독 해제
        if (_dataManager != null)
        {
            _dataManager.OnResourceChanged -= UpdateAllMainText;
            
            // 시간 이벤트 구독 해제
            _dataManager.Time.OnMonthChanged -= OnMonthChanged;
            _dataManager.Time.OnYearChanged -= OnYearChanged;
        }
    }
}

