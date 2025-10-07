using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 메인 UI 매니저
/// 패널들을 Enum으로 관리합니다.
/// </summary>
public class MainUiManager : MonoBehaviour, IUIManager
{
    [Header("References")]
    private GameManager _gameManager;
    private GameDataManager _dataManager;

    [Header("Panels")]
    [SerializeField] private MainPanel _mainPanel;
    [SerializeField] private ProductionPanel _productionPanel;

    [Header("Quick Move")]
    [SerializeField] private GameObject _quickMoveBtnPrefeb;
    [SerializeField] private Transform _quickMovePanelContent;

    // 패널 딕셔너리
    private Dictionary<MainPanelType, BasePanel> _panelDict;

    // 현재 열려있는 패널
    private MainPanelType _currentOpenPanelType;

    // 생성된 QuickMoveBtn 리스트
    private List<QuickMoveBtn> _quickMoveBtns = new List<QuickMoveBtn>();

    // IUIManager 인터페이스 구현
    public Transform CanvasTrans => transform;
    public GameDataManager DataManager => _dataManager;

    /// <summary>
    /// UI 매니저를 초기화합니다.
    /// </summary>
    public void Initialize(GameManager argGameManager, GameDataManager argGameDataManager)
    {
        _gameManager = argGameManager;
        _dataManager = argGameDataManager;

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
            { MainPanelType.Main, _mainPanel },
            { MainPanelType.Production, _productionPanel }
        };
    }

    void Start()
    {
        // 시작 시 모든 패널 초기화
        InitializePanels();

        // QuickMoveBtn 생성
        CreateQuickMoveBtns();
    }

    /// <summary>
    /// 모든 메인 텍스트를 업데이트합니다.
    /// </summary>
    public void UpdateAllMainText()
    {
        // 여기에 모든 메인 UI 텍스트 업데이트 로직 추가
        // 예: 자원 표시, 상태 표시 등
        
        Debug.Log("[MainUiManager] All main text updated.");
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

        OpenPanel(MainPanelType.Main);
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

    void Update()
    {
        // 테스트용: 숫자 키로 패널 전환
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            OpenPanel(MainPanelType.Main);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            OpenPanel(MainPanelType.Production);
        }
    }
}

