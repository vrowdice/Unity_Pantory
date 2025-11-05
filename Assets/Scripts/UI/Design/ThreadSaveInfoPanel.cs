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

        // 현재 스레드 가져오기
        string currentThreadId = _gameManager.CurrentThreadId;
        ThreadState currentThread = _dataManager.GetThread(currentThreadId);

        if (currentThread == null)
        {
            Debug.LogWarning($"[ThreadSaveInfoPanel] Thread not found: {currentThreadId}");
            return;
        }

        // 스레드 이름 변경
        currentThread.threadName = threadName;

        // 카테고리에 스레드 추가
        if (!string.IsNullOrEmpty(_selectedCategoryId))
        {
            _dataManager.AddThreadToCategory(_selectedCategoryId, currentThreadId);
        }

        Debug.Log($"[ThreadSaveInfoPanel] Thread saved: {threadName} (Category: {_selectedCategoryId})");
        _gameManager.ShowWarningPanel("Saved successfully.");
    }
}
