using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 스레드 정보를 표시하고 직원을 할당할 수 있는 패널
/// </summary>
public class ThreadInfoPanel : MonoBehaviour
{
    [Header("Resource References")]
    [SerializeField] private Transform _provideContentTransform;
    [SerializeField] private Transform _consumeContentTransform;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _maintenanceText;
    [SerializeField] private TextMeshProUGUI _categoryText;
    [SerializeField] private Image _image;

    [Header("Employee Assignment")]
    [SerializeField] private Transform _employeeAssignmentContentTransform;
    [SerializeField] private GameObject _employeeAssignmentButtonPrefab;

    private ThreadState _currentThreadState;
    private GameDataManager _dataManager;
    private MainUiManager _mainUiManager;

    /// <summary>
    /// 패널을 초기화합니다.
    /// </summary>
    public void OnInitialize(ThreadState threadState, MainUiManager mainUiManager, GameDataManager dataManager)
    {
        _currentThreadState = threadState;
        _mainUiManager = mainUiManager;
        _dataManager = dataManager;

        if (threadState == null)
        {
            Debug.LogWarning("[ThreadInfoPanel] ThreadState is null!");
            return;
        }

        UpdateUI();
    }

    /// <summary>
    /// UI를 업데이트합니다.
    /// </summary>
    private void UpdateUI()
    {
        if (_currentThreadState == null)
            return;

        // 기본 정보 업데이트
        if (_nameText != null)
            _nameText.text = _currentThreadState.threadName;

        if (_categoryText != null)
            _categoryText.text = _currentThreadState.categoryId;

        // 유지비는 ThreadState에 저장된 값 사용
        if (_maintenanceText != null)
        {
            _maintenanceText.text = $"Maintenance: {_currentThreadState.totalMaintenanceCost:N0}/month";
        }

        // 이미지 로딩
        if (_image != null)
        {
            LoadPreviewImage();
        }

        // 리소스 정보 업데이트
        UpdateResourceDisplay();

        // 직원 할당 정보 업데이트
        UpdateEmployeeAssignment();
    }

    /// <summary>
    /// 스레드 설명을 가져옵니다.
    /// </summary>
    private string GetThreadDescription()
    {
        if (_currentThreadState.buildingStateList == null || _currentThreadState.buildingStateList.Count == 0)
            return "No buildings in this thread.";

        // 건물들의 이름을 나열하여 설명으로 사용
        List<string> buildingNames = new List<string>();
        foreach (var buildingState in _currentThreadState.buildingStateList)
        {
            if (buildingState == null || string.IsNullOrEmpty(buildingState.buildingId))
                continue;

            if (_dataManager != null)
            {
                var buildingData = _dataManager.Building.GetBuildingData(buildingState.buildingId);
                if (buildingData != null)
                {
                    buildingNames.Add(buildingData.displayName);
                }
            }
        }

        if (buildingNames.Count > 0)
        {
            return $"Buildings: {string.Join(", ", buildingNames)}";
        }

        return "Thread description";
    }


    /// <summary>
    /// 미리보기 이미지를 로드합니다.
    /// </summary>
    private void LoadPreviewImage()
    {
        if (string.IsNullOrEmpty(_currentThreadState.previewImagePath))
        {
            _image.enabled = false;
            return;
        }

        // Resources 폴더에서 이미지 로드
        Sprite loadedSprite = Resources.Load<Sprite>(_currentThreadState.previewImagePath);
        if (loadedSprite != null)
        {
            _image.sprite = loadedSprite;
            _image.enabled = true;
        }
        else
        {
            Debug.LogWarning($"[ThreadInfoPanel] Failed to load image from path: {_currentThreadState.previewImagePath}");
            _image.enabled = false;
        }
    }

    /// <summary>
    /// 입력/출력 자원 정보를 표시합니다.
    /// </summary>
    private void UpdateResourceDisplay()
    {
        if (_currentThreadState == null || _dataManager == null || _mainUiManager == null)
            return;

        // 기존 내용 지우기
        if (_provideContentTransform != null)
            GameObjectUtils.ClearChildren(_provideContentTransform);
        if (_consumeContentTransform != null)
            GameObjectUtils.ClearChildren(_consumeContentTransform);

        // ThreadState에서 집계된 자원 정보 가져오기
        if (_currentThreadState.TryGetAggregatedResourceCounts(
            out Dictionary<string, int> consumptionCounts,
            out Dictionary<string, int> productionCounts))
        {
            // 입력 자원 표시
            if (consumptionCounts != null && consumptionCounts.Count > 0 && _provideContentTransform != null)
            {
                foreach (var kvp in consumptionCounts)
                {
                    ResourceEntry resourceEntry = _dataManager.Resource.GetResourceEntry(kvp.Key);
                    if (resourceEntry != null && _mainUiManager.ProductionInfoImage != null)
                    {
                        Instantiate(_mainUiManager.ProductionInfoImage, _provideContentTransform)
                            .GetComponent<ProductionInfoImage>().OnInitialize(resourceEntry, kvp.Value);
                    }
                }
            }

            // 출력 자원 표시
            if (productionCounts != null && productionCounts.Count > 0 && _consumeContentTransform != null)
            {
                foreach (var kvp in productionCounts)
                {
                    ResourceEntry resourceEntry = _dataManager.Resource.GetResourceEntry(kvp.Key);
                    if (resourceEntry != null && _mainUiManager.ProductionInfoImage != null)
                    {
                        Instantiate(_mainUiManager.ProductionInfoImage, _consumeContentTransform)
                            .GetComponent<ProductionInfoImage>().OnInitialize(resourceEntry, kvp.Value);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 직원 할당 UI를 업데이트합니다.
    /// TODO: 직원 할당 기능 구현 필요
    /// </summary>
    private void UpdateEmployeeAssignment()
    {
        // TODO: 직원 할당 기능 구현
        // 1. ThreadState에 직원 할당 정보가 있는지 확인
        // 2. 사용 가능한 직원 목록 가져오기 (GameDataManager에서)
        // 3. 현재 할당된 직원 표시
        // 4. 직원 할당/해제 버튼 생성
        // 5. 직원 할당 시 ThreadState 업데이트
        
        if (_employeeAssignmentContentTransform != null && _employeeAssignmentButtonPrefab != null)
        {
            GameObjectUtils.ClearChildren(_employeeAssignmentContentTransform);
            
            // TODO: 직원 할당 UI 구현
            // 예시:
            // var availableEmployees = _dataManager.GetAvailableEmployees();
            // foreach (var employee in availableEmployees)
            // {
            //     var button = Instantiate(_employeeAssignmentButtonPrefab, _employeeAssignmentContentTransform);
            //     // 버튼 설정 및 이벤트 연결
            // }
        }
    }

    /// <summary>
    /// 직원을 스레드에 할당합니다.
    /// TODO: 직원 할당 로직 구현 필요
    /// </summary>
    public void AssignEmployee(string employeeId, int count)
    {
        if (_currentThreadState == null || _dataManager == null)
            return;

        // TODO: 직원 할당 로직 구현
        // 1. 직원이 사용 가능한지 확인
        // 2. ThreadState에 직원 할당 정보 추가
        // 3. UI 업데이트
        Debug.Log($"[ThreadInfoPanel] AssignEmployee called: {employeeId}, count: {count}");
    }

    /// <summary>
    /// 스레드에서 직원 할당을 해제합니다.
    /// TODO: 직원 할당 해제 로직 구현 필요
    /// </summary>
    public void UnassignEmployee(string employeeId, int count)
    {
        if (_currentThreadState == null || _dataManager == null)
            return;

        // TODO: 직원 할당 해제 로직 구현
        // 1. ThreadState에서 직원 할당 정보 제거
        // 2. UI 업데이트
        Debug.Log($"[ThreadInfoPanel] UnassignEmployee called: {employeeId}, count: {count}");
    }

    /// <summary>
    /// 패널을 숨깁니다.
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }
}