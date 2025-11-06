using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ThreadSaveInfoPanel : MonoBehaviour
{
    [SerializeField] private GameObject _productionInfoIconPanel = null;

    [SerializeField] private TMP_InputField _threadTitleInputField = null;
    [SerializeField] private TextMeshProUGUI _threadCartegoryText = null;
    [SerializeField] private Transform _inputProductionScrollVIewContent = null;
    [SerializeField] private Transform _outputProductionScrollVIewContent = null;
    [SerializeField] private TextMeshProUGUI _totalMaintenanceText = null;

    private GameDataManager _dataManager = null;
    private GameManager _gameManager = null;
    private string _selectedCategoryId = string.Empty;

    public void OnInitialize(GameDataManager dataManager)
    {
        _dataManager = dataManager;
        _gameManager = GameManager.Instance;
    }

    /// <summary>
    /// Thread 정보를 초기화하고 표시합니다.
    /// </summary>
    public void OnInitialize(string threadTitle, List<string> inputResourceIds, List<string> outputResourceIds, Dictionary<string, int> outputResourceCounts, int totalMaintenance)
    {
        // Thread 제목 설정
        if (_threadTitleInputField != null)
        {
            _threadTitleInputField.text = threadTitle;
        }

        // 기존 아이템 제거
        GameObjectUtils.ClearChildren(_inputProductionScrollVIewContent);
        GameObjectUtils.ClearChildren(_outputProductionScrollVIewContent);

        // 입력 생산 자원 표시 (산출량 없음)
        if (_productionInfoIconPanel != null && _inputProductionScrollVIewContent != null && _dataManager != null)
        {
            foreach (var resourceId in inputResourceIds)
            {
                ResourceEntry resourceEntry = _dataManager.GetResourceEntry(resourceId);
                if (resourceEntry != null)
                {
                    GameObject panel = Instantiate(_productionInfoIconPanel, _inputProductionScrollVIewContent);
                    ProductionInfoIconPanel iconPanel = panel.GetComponent<ProductionInfoIconPanel>();
                    if (iconPanel != null)
                    {
                        iconPanel.OnInitialize(resourceEntry);
                    }
                }
            }
        }

        // 출력 생산 자원 표시 (산출량 포함)
        if (_productionInfoIconPanel != null && _outputProductionScrollVIewContent != null && _dataManager != null)
        {
            foreach (var resourceId in outputResourceIds)
            {
                ResourceEntry resourceEntry = _dataManager.GetResourceEntry(resourceId);
                if (resourceEntry != null)
                {
                    GameObject panel = Instantiate(_productionInfoIconPanel, _outputProductionScrollVIewContent);
                    ProductionInfoIconPanel iconPanel = panel.GetComponent<ProductionInfoIconPanel>();
                    if (iconPanel != null)
                    {
                        // 산출량 가져오기 (없으면 0)
                        int productionCount = outputResourceCounts != null && outputResourceCounts.ContainsKey(resourceId) 
                            ? outputResourceCounts[resourceId] 
                            : 0;
                        iconPanel.OnInitialize(resourceEntry, productionCount);
                    }
                }
            }
        }

        // 총 유지비 표시
        if (_totalMaintenanceText != null)
        {
            _totalMaintenanceText.text = $"total maintenance: {totalMaintenance:N0}/month";
        }
    }

    /// <summary>
    /// 카테고리 선택 버튼 클릭 시 호출됩니다.
    /// ManageThreadCartegoryPanel을 열어 카테고리를 선택합니다.
    /// </summary>
    public void OnClickSelectCategory()
    {
        if (_gameManager == null)
        {
            Debug.LogWarning("[ThreadSaveInfoPanel] GameManager is null.");
            return;
        }

        if (_dataManager == null)
        {
            Debug.LogWarning("[ThreadSaveInfoPanel] GameDataManager is null.");
            return;
        }

        // ManageThreadCartegoryPanel 생성
        _gameManager.ShowManageThreadCartegoryPanel(_dataManager, (selectedCategoryId) =>
        {
            // 선택된 카테고리 저장
            _selectedCategoryId = selectedCategoryId;
            
            // 카테고리 텍스트 업데이트
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
    /// 저장 버튼 클릭 시 호출됩니다.
    /// 현재 스레드 정보를 저장합니다.
    /// </summary>
    public void OnClickSave()
    {
        if (_dataManager == null)
        {
            Debug.LogWarning("[SaveInfoPanel] GameDataManager is null.");
            return;
        }

        if (_gameManager == null)
        {
            Debug.LogWarning("[SaveInfoPanel] GameManager is null.");
            return;
        }

        // 스레드 이름 가져오기
        string threadName = _threadTitleInputField != null ? _threadTitleInputField.text : string.Empty;
        
        if (string.IsNullOrEmpty(threadName))
        {
            _gameManager.ShowWarningPanel("Please enter a thread name.");
            return;
        }

        // BuildingTileManager에서 현재 편집 중인 스레드 ID 가져오기
        BuildingTileManager buildingTileManager = FindFirstObjectByType<BuildingTileManager>();
        string currentThreadId = null;
        
        if (buildingTileManager != null)
        {
            currentThreadId = buildingTileManager.GetCurrentThreadId();
        }
        
        // BuildingTileManager에서 가져오지 못했으면 GameManager에서 가져오기
        if (string.IsNullOrEmpty(currentThreadId) && _gameManager != null)
        {
            currentThreadId = _gameManager.CurrentThreadId;
        }
        
        // 그래도 없으면 스레드 이름에서 생성
        if (string.IsNullOrEmpty(currentThreadId))
        {
            currentThreadId = "thread_" + threadName.Trim().Replace(" ", "_").ToLower();
        }
        
        // 스레드가 없으면 생성 (저장 시점에 생성)
        ThreadState currentThread = _dataManager.GetThread(currentThreadId);
        if (currentThread == null)
        {
            // 새 스레드 생성
            currentThread = _dataManager.CreateThread(currentThreadId, threadName, "");
            if (currentThread == null)
            {
                Debug.LogError($"[ThreadSaveInfoPanel] Failed to create thread: {currentThreadId}");
                return;
            }
            Debug.Log($"[ThreadSaveInfoPanel] Created new thread: {currentThreadId} ({threadName})");
        }
        else
        {
            // 기존 스레드 이름 변경
            currentThread.threadName = threadName;
        }

        // 카테고리에 스레드 추가
        if (!string.IsNullOrEmpty(_selectedCategoryId))
        {
            _dataManager.AddThreadToCategory(_selectedCategoryId, currentThreadId);
        }

        // 건물 레이아웃 캡처
        if (buildingTileManager != null)
        {
            // 현재 편집 중인 스레드의 임시 데이터를 먼저 저장 (스레드 ID가 변경되기 전에)
            string oldThreadId = buildingTileManager.GetCurrentThreadId();
            if (!string.IsNullOrEmpty(oldThreadId))
            {
                // 현재 스레드의 임시 데이터를 먼저 저장 (스레드 ID가 같거나 다를 수 있음)
                buildingTileManager.ApplyTempBuildingDataToDataManager();
            }
            
            // BuildingTileManager의 현재 스레드 ID를 업데이트 (저장할 스레드로)
            // 스레드 ID가 같으면 SetCurrentThread는 임시 데이터를 다시 로드하지 않음
            if (oldThreadId != currentThreadId)
            {
                buildingTileManager.SetCurrentThread(currentThreadId);
            }
            
            // 현재 스레드의 임시 저장된 건물 데이터를 GameDataManager에 반영
            buildingTileManager.ApplyTempBuildingDataToDataManager();
            
            // 저장 후 건물을 다시 로드하여 표시
            buildingTileManager.RefreshBuildings();
            
            string imagePath = buildingTileManager.CaptureThreadLayout(currentThreadId);
            if (!string.IsNullOrEmpty(imagePath))
            {
                currentThread.previewImagePath = imagePath;
                Debug.Log($"[ThreadSaveInfoPanel] Thread layout captured: {imagePath}");
            }
        }
        else
        {
            Debug.LogWarning("[ThreadSaveInfoPanel] BuildingTileManager not found. Layout capture skipped.");
        }
        
        // GameManager의 CurrentThreadId도 업데이트
        if (_gameManager != null)
        {
            _gameManager.SetCurrentThreadId(currentThreadId);
        }

        // Thread 데이터 저장
        _dataManager.SaveThreadData();

        Debug.Log($"[ThreadSaveInfoPanel] Thread saved: {threadName} (Category: {_selectedCategoryId})");
        _gameManager.ShowWarningPanel("Saved successfully.");
    }
}
