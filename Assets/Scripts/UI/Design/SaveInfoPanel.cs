using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SaveInfoPanel : MonoBehaviour
{
    [SerializeField] private GameObject _productionInfoIconPanel = null;

    [SerializeField] private TMP_InputField _threadTitleInputField = null;
    [SerializeField] private Transform _inputProductionScrollVIewContent = null;
    [SerializeField] private Transform _outputProductionScrollVIewContent = null;
    [SerializeField] private TextMeshProUGUI _totalMaintenanceText = null;

    private GameDataManager _dataManager = null;

    public void OnInitialize(GameDataManager dataManager)
    {
        _dataManager = dataManager;
    }

    /// <summary>
    /// Thread 정보를 초기화하고 표시합니다.
    /// </summary>
    public void InitializeAndShow(string threadTitle, List<string> inputResourceIds, List<string> outputResourceIds, Dictionary<string, int> outputResourceCounts, int totalMaintenance)
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

}
