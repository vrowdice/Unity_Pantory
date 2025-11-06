using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DesignUiManager : MonoBehaviour, IUIManager
{
    [SerializeField] private GameObject _buildingTypeBtnPrefab = null;
    [SerializeField] private Transform _buildingTypeBtnContent = null;
    [SerializeField] private GameObject _buildingBtnPrefab = null;
    [SerializeField] private Transform _buildingBtnContent = null;
    [SerializeField] private TMP_InputField _threadTitleInputField = null;

    [SerializeField] private Image _deselectBuildingBtnImage = null;
    [SerializeField] private Image _removalModeBtnImage = null;
    [SerializeField] private BuildingInfoPanel _buildingInfoPanel = null;
    [SerializeField] private ThreadSaveInfoPanel _saveInfoPanel = null;

    private GameManager _gameManager = null;
    private GameDataManager _dataManager = null;
    private List<BuildingData> _buildingDataList = null;
    private BuildingData _selectedBuilding = null;  // 현재 선택된 건물
    private BuildingTileManager _buildingTileManager = null;
    private bool _isRemovalMode = false;  // 현재 제거 모드 활성화 여부
    private GameObject _productionInfoImage = null;

    public Transform CanvasTrans => transform;

    public GameManager GameManager => _gameManager;
    public GameDataManager GameDataManager => _dataManager;
    public BuildingData SelectedBuilding => _selectedBuilding;
    public bool IsRemovalMode => _isRemovalMode;
    public GameObject ProductionInfoImage => _productionInfoImage;

    public void Initialize(GameManager argGameManager, GameDataManager argGameDataManager)
    {
        _gameManager = argGameManager;
        _dataManager = argGameDataManager;
        _buildingTileManager = FindFirstObjectByType<BuildingTileManager>();
        _productionInfoImage = argGameManager.ProductionInfoImage;

        // BuildingType 버튼 생성
        EnumUtils.GetAllEnumValues<BuildingType>().ForEach(buildingType =>
        {
            var btn = Instantiate(_buildingTypeBtnPrefab, _buildingTypeBtnContent);
            btn.GetComponent<BuildingTypeBtn>().Initialize(this, buildingType);
        });

        // 기본 타입 선택
        SelectBuildingType(BuildingType.Distribution);
        
        // InputField 이벤트 등록
        if (_threadTitleInputField != null)
        {
            _threadTitleInputField.onEndEdit.AddListener(OnThreadTitleChanged);
            
            // 기본값이 비어있으면 설정
            if (string.IsNullOrEmpty(_threadTitleInputField.text))
            {
                _threadTitleInputField.text = "Main Line";
            }
            
            // 초기 Thread 생성
            OnThreadTitleChanged(_threadTitleInputField.text);
        }

        // SaveInfoPanel 초기화
        if (_saveInfoPanel != null)
        {
            _saveInfoPanel.OnInitialize(_dataManager);
        }
    }

    public void UpdateAllMainText()
    {
        Debug.Log("UpdateAllMainText");
    }

    public void UpdateModeBtnImages(bool isPlacementMode, bool isRemovalMode)
    {
        _deselectBuildingBtnImage.color = isPlacementMode ? VisualManager.Instance.ValidColor : Color.white;
        _removalModeBtnImage.color = isRemovalMode ? VisualManager.Instance.InvalidColor : Color.white;
    }

    /// <summary>
    /// 건물 타입을 선택하고 해당 타입의 건물 버튼들을 생성합니다.
    /// </summary>
    /// <param name="buildingType">선택할 건물 타입</param>
    public void SelectBuildingType(BuildingType buildingType)
    {
        if (_dataManager == null)
        {
            Debug.LogWarning("[DesignUiManager] DataManager is null. Cannot select building type.");
            return;
        }

        // 해당 타입의 건물 리스트 가져오기
        _buildingDataList = _dataManager.GetBuildingDataList(buildingType);

        // 기존 건물 버튼 제거
        var existingButtons = _buildingBtnContent.GetComponentsInChildren<BuildingBtn>();
        foreach (var btn in existingButtons)
        {
            Destroy(btn.gameObject);
        }

        // 새 건물 버튼 생성
        foreach (var buildingData in _buildingDataList)
        {
            var btn = Instantiate(_buildingBtnPrefab, _buildingBtnContent);
            var buildingBtn = btn.GetComponent<BuildingBtn>();
            
            if (buildingBtn != null )
            {
                buildingBtn.Initialize(this, buildingData);
            }
            else
            {
                Debug.LogError("[DesignUiManager] BuildingBtn component not found on prefab.");
                Destroy(btn);
            }
        }
    }

    /// <summary>
    /// 건물을 선택합니다. (배치 모드로 진입)
    /// </summary>
    public void SelectBuilding(BuildingData buildingData)
    {
        _selectedBuilding = buildingData;
        _isRemovalMode = false;  // 배치 모드로 전환
        Debug.Log($"[DesignUiManager] Building selected: {buildingData.displayName}");
        
        // 제거 모드 취소
        if (_buildingTileManager != null && _buildingTileManager.IsRemovalMode)
        {
            _buildingTileManager.CancelRemovalMode();
        }
        
        // BuildingTileManager에 선택된 건물 전달
        if (_buildingTileManager != null)
        {
            _buildingTileManager.StartPlacementMode(buildingData);
        }
    }

    /// <summary>
    /// 건물 선택을 취소합니다.
    /// </summary>
    public void DeselectBuilding()
    {
        _selectedBuilding = null;
        _isRemovalMode = false;
        Debug.Log("[DesignUiManager] Building deselected");
        
        if (_buildingTileManager != null)
        {
            _buildingTileManager.CancelPlacementMode();
        }
    }

    /// <summary>
    /// 건물 제거 모드를 시작합니다.
    /// </summary>
    public void StartRemovalMode()
    {
        _isRemovalMode = true;
        _selectedBuilding = null;
        
        // 기존 배치 모드 취소
        if (_buildingTileManager != null && _buildingTileManager.IsPlacementMode)
        {
            _buildingTileManager.CancelPlacementMode();
        }
        
        if (_buildingTileManager != null)
        {
            _buildingTileManager.StartRemovalMode();
        }
    }

    /// <summary>
    /// 건물 제거 모드를 취소합니다.
    /// </summary>
    public void CancelRemovalMode()
    {
        _isRemovalMode = false;
        
        if (_buildingTileManager != null)
        {
            _buildingTileManager.CancelRemovalMode();
        }
    }

    /// <summary>
    /// 건물 제거 모드를 토글합니다.
    /// </summary>
    public void RemovalModeToggle()
    {
        if (_isRemovalMode)
        {
            CancelRemovalMode();
            Debug.Log("[DesignUiManager] Removal mode toggled OFF");
        }
        else
        {
            StartRemovalMode();
            Debug.Log("[DesignUiManager] Removal mode toggled ON");
        }
    }

    public void OnClickSaveBtn()
    {
        if (_buildingTileManager == null || _dataManager == null || _saveInfoPanel == null)
        {
            Debug.LogWarning("[DesignUiManager] Cannot show save info: Required components are null");
            return;
        }

        string currentThreadId = GetCurrentThreadId();
        if (string.IsNullOrEmpty(currentThreadId))
        {
            Debug.LogWarning("[DesignUiManager] Cannot show save info: Thread ID is empty");
            return;
        }

        // BuildingTileManager를 통해 BuildingCalculateHandler의 계산 메서드 호출
        // 생산 체인 추적을 통한 입력/출력 자원 자동 계산
        int totalMaintenance = _buildingTileManager.CalculateTotalMaintenanceCost(currentThreadId);
        
        // 생산 체인을 추적하여 하역소→생산건물→상역소 연결된 자원 계산
        List<string> inputResourceIds;
        List<string> outputResourceIds;
        Dictionary<string, int> outputResourceCounts;
        _buildingTileManager.CalculateProductionChain(currentThreadId, out inputResourceIds, out outputResourceIds, out outputResourceCounts);
        
        // 산출량 계산 (기존 메서드 사용)
        int threadOutputs = _buildingTileManager.CalculateCurrentThreadOutputs();
        
        // Thread 제목 가져오기
        string threadTitle = GetCurrentThreadTitle();
        
        // SaveInfoPanel 초기화 및 표시
        if (_saveInfoPanel != null)
        {
            _saveInfoPanel.OnInitialize(threadTitle, inputResourceIds, outputResourceIds, outputResourceCounts, totalMaintenance);
            _saveInfoPanel.gameObject.SetActive(true);
            Debug.Log($"[DesignUiManager] Save info panel shown. Outputs: {threadOutputs}, Maintenance: {totalMaintenance}, Input Resources: {inputResourceIds.Count}, Output Resources: {outputResourceIds.Count}");
        }
    }

    public void OnClickLoadBtn()
    {
        if (_gameManager == null || _dataManager == null)
        {
            Debug.LogWarning("[DesignUiManager] Cannot load thread: GameManager or DataManager is null");
            return;
        }

        // ManageThreadPanel 열기
        _gameManager.ShowManageThreadPanel((selectedThreadId) =>
        {
            // 선택된 스레드 정보 로드 및 적용
            LoadThread(selectedThreadId);
        });
    }

    /// <summary>
    /// 선택된 스레드를 로드하고 적용합니다.
    /// </summary>
    private void LoadThread(string threadId)
    {
        if (string.IsNullOrEmpty(threadId) || _dataManager == null)
        {
            Debug.LogWarning("[DesignUiManager] Cannot load thread: Thread ID is empty or DataManager is null");
            return;
        }

        // 스레드 정보 가져오기
        ThreadState thread = _dataManager.GetThread(threadId);
        if (thread == null)
        {
            Debug.LogWarning($"[DesignUiManager] Thread not found: {threadId}");
            return;
        }

        // Thread 제목을 InputField에 설정
        string threadTitle = string.IsNullOrEmpty(thread.threadName) ? threadId : thread.threadName;
        if (_threadTitleInputField != null)
        {
            _threadTitleInputField.text = threadTitle;
        }

        // BuildingTileManager에 현재 Thread 설정 (실제 threadId 사용)
        if (_buildingTileManager != null)
        {
            _buildingTileManager.SetCurrentThread(threadId);
            // 건물들 새로고침 (Thread에 저장된 건물들이 로드됨)
            _buildingTileManager.RefreshBuildings();
        }

        Debug.Log($"[DesignUiManager] Thread loaded: {threadId} ({thread.threadName})");
    }

    /// <summary>
    /// Thread 제목이 변경되었을 때 호출됩니다.
    /// </summary>
    private void OnThreadTitleChanged(string threadTitle)
    {
        if (string.IsNullOrWhiteSpace(threadTitle))
        {
            Debug.LogWarning("[DesignUiManager] Thread title cannot be empty");
            return;
        }

        // Thread ID 생성 (공백 제거하고 소문자로 변환)
        string threadId = "thread_" + threadTitle.Trim().Replace(" ", "_").ToLower();
        
        Debug.Log($"[DesignUiManager] Thread title changed to: {threadTitle} (ID: {threadId})");

        // Thread는 저장 시 생성되므로 여기서는 생성하지 않음
        // BuildingTileManager에 현재 Thread 설정
        if (_buildingTileManager != null)
        {
            _buildingTileManager.SetCurrentThread(threadId);
        }
    }

    /// <summary>
    /// 현재 Thread 제목을 반환합니다.
    /// </summary>
    public string GetCurrentThreadTitle()
    {
        return _threadTitleInputField != null ? _threadTitleInputField.text : "Main Line";
    }

    /// <summary>
    /// 현재 Thread ID를 반환합니다.
    /// </summary>
    public string GetCurrentThreadId()
    {
        string title = GetCurrentThreadTitle();
        return "thread_" + title.Trim().Replace(" ", "_").ToLower();
    }

    /// <summary>
    /// 건물 정보 패널을 표시합니다.
    /// </summary>
    public void ShowBuildingInfo(BuildingData buildingData, BuildingState buildingState)
    {
        if (_buildingInfoPanel != null)
        {
            _buildingInfoPanel.gameObject.SetActive(true);
            _buildingInfoPanel.ShowBuildingInfo(buildingData, buildingState, this);
            Debug.Log($"[DesignUiManager] Showing building info for: {buildingData.displayName}");
        }
        else
        {
            Debug.LogWarning("[DesignUiManager] BuildingInfoPanel is not assigned!");
        }
    }

    /// <summary>
    /// 건물 정보 패널을 숨깁니다.
    /// </summary>
    public void HideBuildingInfo()
    {
        if (_buildingInfoPanel != null)
        {
            _buildingInfoPanel.gameObject.SetActive(false);
        }
    }

    // ================== 건물 회전 제어 ==================
    
    /// <summary>
    /// 건물을 왼쪽으로 회전합니다 (반시계방향, -90도).
    /// 배치 모드가 활성화되어 있을 때만 작동합니다.
    /// </summary>
    public void RotateBuildingLeft()
    {
        if (_buildingTileManager == null)
        {
            Debug.LogWarning("[DesignUiManager] BuildingTileManager is null. Cannot rotate building.");
            return;
        }
        
        _buildingTileManager.RotateBuildingLeft();
    }

    /// <summary>
    /// 건물을 오른쪽으로 회전합니다 (시계방향, +90도).
    /// 배치 모드가 활성화되어 있을 때만 작동합니다.
    /// </summary>
    public void RotateBuildingRight()
    {
        if (_buildingTileManager == null)
        {
            Debug.LogWarning("[DesignUiManager] BuildingTileManager is null. Cannot rotate building.");
            return;
        }
        
        _buildingTileManager.RotateBuildingRight();
    }
}
