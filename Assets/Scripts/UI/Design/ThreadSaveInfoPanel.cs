using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Image, Button, InputField 등을 위해 사용
using TMPro; // TMP_InputField, TextMeshProUGUI를 위해 필요

/// <summary>
/// Thread 저장 정보를 표시하고 최종 저장(Save) 명령을 처리하는 UI 패널입니다.
/// </summary>
public class ThreadSaveInfoPanel : MonoBehaviour
{
    #region Inspector Fields

    [Header("UI Prefabs & Contents")]
    [SerializeField] private GameObject _productionInfoIconPanel = null;
    [SerializeField] private TMP_InputField _threadTitleInputField = null;
    [SerializeField] private TextMeshProUGUI _threadCartegoryText = null;
    [SerializeField] private Transform _inputProductionScrollVIewContent = null;
    [SerializeField] private Transform _outputProductionScrollVIewContent = null;
    [SerializeField] private TextMeshProUGUI _totalMaintenanceText = null;

    #endregion

    #region Private State & Managers

    private GameDataManager _dataManager = null;
    private GameManager _gameManager = null;
    private DesignUiManager _designUiManager = null;
    private string _selectedCategoryId = string.Empty; // 사용자가 선택한 카테고리 ID

    #endregion

    //---------------------------------------------------------

    #region 초기화 및 표시

    /// <summary>
    /// SaveLoadHandler에서 사용할 DataManager를 초기 설정합니다. (Awake/Initialize 단계)
    /// </summary>
    public void OnInitialize(GameDataManager dataManager)
    {
        _dataManager = dataManager;
        _gameManager = GameManager.Instance;
    }

    /// <summary>
    /// Thread 정보를 계산된 데이터로 초기화하고 패널을 표시합니다.
    /// 이 메서드는 DesignUiManager.OnClickSaveBtn()에서 호출됩니다.
    /// </summary>
    public void OnShow(string threadTitle, List<string> inputResourceIds, Dictionary<string, int> inputResourceCounts, List<string> outputResourceIds, Dictionary<string, int> outputResourceCounts, int totalMaintenance, DesignUiManager designUiManager)
    {
        _designUiManager = designUiManager;

        // 1. Thread 제목 설정
        if (_threadTitleInputField != null)
        {
            _threadTitleInputField.text = threadTitle ?? string.Empty;
        }

        // 2. 현재 편집 중인 Thread의 기존 카테고리 ID를 가져와 설정
        string currentThreadId = _designUiManager.BuildingTileManager.CurrentThreadId;
        _selectedCategoryId = string.Empty;

        if (!string.IsNullOrEmpty(currentThreadId) && _dataManager != null)
        {
            ThreadState existingThread = _dataManager.GetThread(currentThreadId);
            if (existingThread != null && !string.IsNullOrEmpty(existingThread.categoryId))
            {
                _selectedCategoryId = existingThread.categoryId;
            }
        }

        // 3. UI 갱신
        UpdateCategoryText();
        GameObjectUtils.ClearChildren(_inputProductionScrollVIewContent);
        GameObjectUtils.ClearChildren(_outputProductionScrollVIewContent);

        // 4. 입력/출력 자원 목록 표시
        inputResourceIds ??= new List<string>();
        inputResourceCounts ??= new Dictionary<string, int>();
        outputResourceIds ??= new List<string>();
        outputResourceCounts ??= new Dictionary<string, int>();

        DisplayProductionIcons(inputResourceIds, _inputProductionScrollVIewContent, inputResourceCounts, isOutput: false);
        DisplayProductionIcons(outputResourceIds, _outputProductionScrollVIewContent, outputResourceCounts, isOutput: true);

        // 5. 총 유지비 표시
        if (_totalMaintenanceText != null)
        {
            _totalMaintenanceText.text = $"total maintenance: {totalMaintenance:N0}/month";
        }

        gameObject.SetActive(true);
    }

    /// <summary>
    /// 자원 아이콘과 산출량을 스크롤 뷰에 표시합니다.
    /// </summary>
    private void DisplayProductionIcons(List<string> resourceIds, Transform content, Dictionary<string, int> counts, bool isOutput)
    {
        if (_productionInfoIconPanel == null || content == null || _dataManager == null)
            return;

        foreach (var resourceId in resourceIds)
        {
            ResourceEntry resourceEntry = _dataManager.GetResourceEntry(resourceId);
            if (resourceEntry != null)
            {
                GameObject panel = Instantiate(_productionInfoIconPanel, content);
                ProductionInfoIconPanel iconPanel = panel.GetComponent<ProductionInfoIconPanel>();

                if (iconPanel == null)
                    continue;

                int amount = 0;
                if (counts != null && counts.TryGetValue(resourceId, out int value))
                {
                    amount = value;
                }
                else
                {
                    amount = -1;
                }

                iconPanel.OnInitialize(resourceEntry, amount);
            }
        }
    }

    #endregion

    //---------------------------------------------------------

    #region UI 상호작용

    /// <summary>
    /// 카테고리 선택 버튼 클릭 시 호출됩니다.
    /// ManageThreadCartegoryPanel을 열어 카테고리를 선택합니다.
    /// </summary>
    public void OnClickSelectCategory()
    {
        if (_gameManager == null || _dataManager == null)
        {
            Debug.LogWarning("[ThreadSaveInfoPanel] Managers are null.");
            return;
        }

        // ManageThreadCartegoryPanel 생성
        _gameManager.ShowManageThreadCartegoryPanel(_dataManager, (selectedCategoryId) =>
        {
            // 콜백: 선택된 카테고리 저장 및 UI 업데이트
            _selectedCategoryId = selectedCategoryId;
            UpdateCategoryText();
        });
    }

    /// <summary>
    /// 카테고리 텍스트를 업데이트합니다.
    /// </summary>
    private void UpdateCategoryText()
    {
        if (_threadCartegoryText == null || _dataManager == null)
            return;

        if (string.IsNullOrEmpty(_selectedCategoryId))
        {
            _threadCartegoryText.text = "No Category";
            return;
        }

        ThreadCategory category = _dataManager.GetCategory(_selectedCategoryId);
        if (category != null)
        {
            _threadCartegoryText.text = category.categoryName;
        }
        else
        {
            _threadCartegoryText.text = "No Category";
        }
    }

    /// <summary>
    /// 저장 확인 버튼 클릭 시 호출됩니다. (최종 저장 명령)
    /// </summary>
    public void OnClickSave()
    {
        if (_designUiManager == null || _gameManager == null)
        {
            Debug.LogError("[SaveInfoPanel] DesignUiManager or GameManager reference is missing.");
            return;
        }

        // 1. 스레드 이름 유효성 검사
        string threadName = _threadTitleInputField != null ? _threadTitleInputField.text : string.Empty;

        if (string.IsNullOrEmpty(threadName))
        {
            _gameManager.ShowWarningPanel("Please enter a thread name.");
            return;
        }

        // 2. 최종 저장 명령을 DesignUiManager에 위임
        // DesignUiManager는 이 정보를 가지고 BuildingTileManager의 임시 데이터를 DataManager에 반영하고, 최종 파일 저장을 트리거합니다.
        _designUiManager.SaveThreadChanges(threadName, _selectedCategoryId);

        Debug.Log($"[ThreadSaveInfoPanel] Save request delegated for: {threadName} (Category: {_selectedCategoryId})");

        // 3. 저장 완료 알림 및 패널 숨김
        gameObject.SetActive(false);
    }

    #endregion
}