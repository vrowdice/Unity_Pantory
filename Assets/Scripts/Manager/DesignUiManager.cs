using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Image, Button, InputField 등을 위해 사용
using TMPro; // TMP_InputField, TextMeshProUGUI를 위해 필요

/// <summary>
/// 디자인 모드의 UI를 관리하는 매니저
/// </summary>
public class DesignUiManager : MonoBehaviour, IUIManager
{
    // DesignUiManager는 Thread 제목을 직접 관리하는 InputField를 가져야 하므로, 
    // 여기에 TMP_InputField를 추가합니다. (원래 코드에는 없었지만, SaveInfoPanel에서 사용했기 때문에 필요함)
    [Header("Thread Title Input")]
    [SerializeField] private TMP_InputField _threadTitleInputField = null;

    #region Inspector Fields

    [Header("UI Prefabs & Contents")]
    [SerializeField] private GameObject _buildingTypeBtnPrefab = null;
    [SerializeField] private Transform _buildingTypeBtnContent = null;
    [SerializeField] private GameObject _buildingBtnPrefab = null;
    [SerializeField] private Transform _buildingBtnContent = null;

    [Header("Mode & Panel References")]
    [SerializeField] private Image _deselectBuildingBtnImage = null;
    [SerializeField] private Image _removalModeBtnImage = null;
    [SerializeField] private BuildingInfoPanel _buildingInfoPanel = null;
    [SerializeField] private ThreadSaveInfoPanel _saveInfoPanel = null;

    #endregion

    #region Private State & Managers

    private GameManager _gameManager = null;
    private GameDataManager _dataManager = null;
    private BuildingTileManager _buildingTileManager = null;
    private List<BuildingData> _buildingDataList = null;

    private BuildingData _selectedBuilding = null; // 현재 선택된 건물
    private bool _isRemovalMode = false;         // 현재 제거 모드 활성화 여부
    private GameObject _productionInfoImage = null; // ProductionInfoImage는 GameManager에서 설정됨

    #endregion

    #region Public Properties

    public Transform CanvasTrans => transform;
    public BuildingTileManager BuildingTileManager => _buildingTileManager;
    public GameManager GameManager => _gameManager;
    public GameDataManager GameDataManager => _dataManager;
    public BuildingData SelectedBuilding => _selectedBuilding;
    public bool IsRemovalMode => _isRemovalMode;
    public GameObject ProductionInfoImage => _productionInfoImage;

    #endregion

    //---------------------------------------------------------

    #region 초기화

    public void Initialize(GameManager argGameManager, GameDataManager argGameDataManager)
    {
        _gameManager = argGameManager;
        _dataManager = argGameDataManager;

        // 씬에서 BuildingTileManager를 찾아 참조
        _buildingTileManager = FindFirstObjectByType<BuildingTileManager>();
        _productionInfoImage = argGameManager.ProductionInfoImage; // GameManager로부터 이미지 오브젝트 참조 획득

        // BuildingType 버튼 생성
        EnumUtils.GetAllEnumValues<BuildingType>().ForEach(buildingType =>
        {
            var btn = Instantiate(_buildingTypeBtnPrefab, _buildingTypeBtnContent);
            btn.GetComponent<BuildingTypeBtn>().Initialize(this, buildingType);
        });

        // 기본 타입 선택 및 버튼 생성
        SelectBuildingType(BuildingType.Distribution);

        // SaveInfoPanel 초기화
        if (_saveInfoPanel != null)
        {
            _saveInfoPanel.OnInitialize(_dataManager);
        }

        // Thread 제목 변경 이벤트 리스너 추가
        if (_threadTitleInputField != null)
        {
            _threadTitleInputField.onEndEdit.AddListener(OnThreadTitleChanged);

            // 초기 Thread ID 설정을 위해 한 번 호출
            if (string.IsNullOrEmpty(_threadTitleInputField.text))
            {
                _threadTitleInputField.text = "Main Line";
            }
            OnThreadTitleChanged(_threadTitleInputField.text);
        }
    }

    public void UpdateAllMainText()
    {
        Debug.Log("[DesignUiManager] UpdateAllMainText called (placeholder)");
    }

    /// <summary>
    /// 배치/제거 모드 버튼의 시각적 상태를 업데이트합니다.
    /// </summary>
    public void UpdateModeBtnImages(bool isPlacementMode, bool isRemovalMode)
    {
        _deselectBuildingBtnImage.color = isPlacementMode ? VisualManager.Instance.ValidColor : Color.white;
        _removalModeBtnImage.color = isRemovalMode ? VisualManager.Instance.InvalidColor : Color.white;
    }

    #endregion

    //---------------------------------------------------------

    #region 건물 목록 관리 (Building Type Selection)

    /// <summary>
    /// 건물 타입을 선택하고 해당 타입의 건물 버튼들을 생성합니다.
    /// </summary>
    public void SelectBuildingType(BuildingType buildingType)
    {
        if (_dataManager == null) return;

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

            if (buildingBtn != null)
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

    #endregion

    //---------------------------------------------------------

    #region 모드 제어 (Placement / Removal)

    /// <summary> 건물을 선택하고 배치 모드로 진입합니다. </summary>
    public void SelectBuilding(BuildingData buildingData)
    {
        _selectedBuilding = buildingData;
        _isRemovalMode = false;

        if (_buildingTileManager != null)
        {
            if (_buildingTileManager.IsRemovalMode) _buildingTileManager.CancelRemovalMode();
            _buildingTileManager.StartPlacementMode(buildingData);
        }

        UpdateModeBtnImages(true, false);
        Debug.Log($"[DesignUiManager] Building selected: {buildingData.displayName}");
    }

    /// <summary> 건물 선택을 취소하고 배치 모드에서 벗어납니다. </summary>
    public void DeselectBuilding()
    {
        _selectedBuilding = null;
        _isRemovalMode = false;

        _buildingTileManager?.CancelPlacementMode();

        UpdateModeBtnImages(false, false);
        Debug.Log("[DesignUiManager] Building deselected");
    }

    /// <summary> 건물 제거 모드를 시작합니다. </summary>
    public void StartRemovalMode()
    {
        _isRemovalMode = true;
        _selectedBuilding = null;

        if (_buildingTileManager != null)
        {
            if (_buildingTileManager.IsPlacementMode) _buildingTileManager.CancelPlacementMode();
            _buildingTileManager.StartRemovalMode();
        }

        UpdateModeBtnImages(false, true);
        Debug.Log("[DesignUiManager] Removal mode started");
    }

    /// <summary> 건물 제거 모드를 취소합니다. </summary>
    public void CancelRemovalMode()
    {
        _isRemovalMode = false;

        _buildingTileManager?.CancelRemovalMode();

        UpdateModeBtnImages(false, false);
        Debug.Log("[DesignUiManager] Removal mode cancelled");
    }

    /// <summary> 건물 제거 모드를 토글합니다. </summary>
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

    #endregion

    //---------------------------------------------------------

    #region Thread 관리 및 저장/로드

    /// <summary>
    /// Thread 제목이 변경되었을 때 호출됩니다.
    /// Thread ID를 생성하고 BuildingTileManager에 설정합니다.
    /// </summary>
    private void OnThreadTitleChanged(string threadTitle)
    {
        if (string.IsNullOrWhiteSpace(threadTitle))
        {
            Debug.LogWarning("[DesignUiManager] Thread title cannot be empty");
            return;
        }

        // BuildingTileManager의 유틸리티 메서드를 사용하여 ID 생성
        string threadId = GetThreadIdFromTitle(threadTitle);

        Debug.Log($"[DesignUiManager] Thread title changed to: {threadTitle} (ID: {threadId})");

        // BuildingTileManager에 현재 Thread 설정 (임시 데이터 로드/초기화)
        _buildingTileManager?.SetCurrentThread(threadId);
    }

    /// <summary>
    /// '저장' 버튼 클릭 시, 저장 정보를 계산하고 패널을 표시합니다.
    /// </summary>
    public void OnClickSaveBtn()
    {
        if (_saveInfoPanel == null || _buildingTileManager == null || _dataManager == null)
        {
            Debug.LogWarning("[DesignUiManager] Cannot show save info: Required components are null.");
            return;
        }

        // 1. 현재 편집 중인 Thread 정보 가져오기
        string threadId = _buildingTileManager.CurrentThreadId;
        string threadTitle = GetCurrentThreadTitle();

        if (string.IsNullOrEmpty(threadId))
        {
            // ID가 없으면 제목으로 임시 ID 생성 (계산용)
            threadId = GetThreadIdFromTitle(threadTitle);
        }

        // 2. 계산 핸들러를 통해 필요한 정보 계산 (BuildingTileManager 중개)
        List<string> inputResourceIds;
        Dictionary<string, int> inputResourceCounts;
        List<string> outputResourceIds;
        Dictionary<string, int> outputResourceCounts;

        _buildingTileManager.CalculateProductionChain(threadId, out inputResourceIds, out inputResourceCounts, out outputResourceIds, out outputResourceCounts);
        int totalMaintenance = _buildingTileManager.CalculateTotalMaintenanceCost(threadId);

        // 3. SaveInfoPanel 초기화 및 표시
        _saveInfoPanel.OnShow(threadTitle, inputResourceIds, inputResourceCounts, outputResourceIds, outputResourceCounts, totalMaintenance, this);
        Debug.Log($"[DesignUiManager] Save info panel shown. Thread ID used for calculation: {threadId}");
    }

    /// <summary>
    /// [ThreadSaveInfoPanel]에서 최종 저장 확인 시 호출되어, 
    /// BuildingTileManager에게 저장 명령을 위임합니다.
    /// </summary>
    public void SaveThreadChanges(string threadName, string categoryId)
    {
        if (_buildingTileManager == null)
        {
            Debug.LogError("[DesignUiManager] Cannot save changes: BuildingTileManager is null.");
            return;
        }

        // BuildingTileManager에게 모든 저장/갱신 작업을 위임합니다.
        _buildingTileManager.SaveThreadChanges(threadName, categoryId);

        // UI 상태 정리 및 알림
        DeselectBuilding();
        _gameManager?.ShowWarningPanel("Saved successfully.");
    }


    /// <summary>
    /// '로드' 버튼 클릭 시, 스레드 관리 패널을 표시합니다.
    /// </summary>
    public void OnClickLoadBtn()
    {
        if (_gameManager == null || _dataManager == null) return;

        // ManageThreadPanel 열기
        _gameManager.ShowManageThreadPanel((selectedThreadId) =>
        {
            LoadThread(selectedThreadId);
        });
    }

    /// <summary>
    /// 선택된 스레드를 로드하고 적용합니다.
    /// </summary>
    private void LoadThread(string threadId)
    {
        if (string.IsNullOrEmpty(threadId) || _dataManager == null) return;

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

        // BuildingTileManager에 현재 Thread 설정 (임시 데이터 로드 및 렌더링)
        _buildingTileManager?.SetCurrentThread(threadId);

        Debug.Log($"[DesignUiManager] Thread loaded: {threadId} ({thread.threadName})");
    }

    #endregion

    //---------------------------------------------------------

    #region Info Panel and Rotation

    /// <summary> 건물 정보 패널을 표시합니다. </summary>
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

    /// <summary> 건물 정보 패널을 숨깁니다. </summary>
    public void HideBuildingInfo()
    {
        _buildingInfoPanel?.gameObject.SetActive(false);
    }

    /// <summary> 건물을 왼쪽으로 회전합니다. </summary>
    public void RotateBuildingLeft()
    {
        if (_buildingTileManager == null) return;
        _buildingTileManager.RotateBuildingLeft();
    }

    /// <summary> 건물을 오른쪽으로 회전합니다. </summary>
    public void RotateBuildingRight()
    {
        if (_buildingTileManager == null) return;
        _buildingTileManager.RotateBuildingRight();
    }

    #endregion

    //---------------------------------------------------------

    #region 유틸리티: Thread ID/Title

    /// <summary> 현재 Thread 제목을 반환합니다. </summary>
    public string GetCurrentThreadTitle()
    {
        return _threadTitleInputField != null ? _threadTitleInputField.text : "Main Line";
    }

    /// <summary> 제목을 기반으로 Thread ID 문자열을 생성합니다. </summary>
    public string GetThreadIdFromTitle(string threadTitle)
    {
        // "thread_" 접두사 + (공백 제거 및 소문자 변환)
        return "thread_" + threadTitle.Trim().Replace(" ", "_").ToLower();
    }

    #endregion
}