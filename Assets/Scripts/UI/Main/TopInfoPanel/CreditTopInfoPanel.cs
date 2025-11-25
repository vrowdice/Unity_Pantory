using System.Collections.Generic;
using UnityEngine;

public class CreditTopInfoPanel : MonoBehaviour
{
    [SerializeField] private GameObject _titleDeltaTextPanelPrefab;
    [SerializeField] private PanelDoAni _panelDoAni;

    private GameDataManager _dataManager;
    private readonly List<TitleDeltaTextPanel> _createdPanels = new List<TitleDeltaTextPanel>();

    /// <summary>
    /// 크레딧 정보 패널을 초기화합니다.
    /// </summary>
    public void OnInitialize(GameDataManager dataManager)
    {
        _dataManager = dataManager;
        
        if (_panelDoAni != null)
        {
            _panelDoAni.SnapToClosedPosition();
        }
        
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 크레딧 정보 패널을 표시합니다.
    /// </summary>
    public void ShowCreditInfo()
    {
        if (_dataManager == null)
        {
            Debug.LogWarning("[CreditInfoPanel] DataManager is null. Cannot show credit info.");
            return;
        }

        gameObject.SetActive(true);
        UpdateCreditInfo();
        
        if (_panelDoAni != null)
        {
            _panelDoAni.OpenPanel();
        }
    }

    /// <summary>
    /// 크레딧 정보 패널을 숨깁니다.
    /// </summary>
    public void HideCreditInfo()
    {
        if (_panelDoAni != null)
        {
            _panelDoAni.ClosePanel(() => gameObject.SetActive(false));
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 크레딧 정보 패널을 토글합니다.
    /// </summary>
    public void ToggleCreditInfo()
    {
        if (_panelDoAni == null)
        {
            // PanelDoAni가 없을 경우 기본 토글
            if (gameObject.activeSelf)
            {
                gameObject.SetActive(false);
            }
            else
            {
                gameObject.SetActive(true);
                if (_dataManager != null)
                {
                    UpdateCreditInfo();
                }
            }
            return;
        }

        // PanelDoAni의 목표 상태를 확인하여 토글
        bool willBeOpen = !_panelDoAni.IsOpen;
        
        if (willBeOpen)
        {
            // 열기: 먼저 활성화하고 정보 업데이트 후 애니메이션
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }
            
            if (_dataManager != null)
            {
                UpdateCreditInfo();
            }
            
            _panelDoAni.OpenPanel();
        }
        else
        {
            // 닫기: 애니메이션 후 비활성화
            _panelDoAni.ClosePanel(() => gameObject.SetActive(false));
        }
    }

    /// <summary>
    /// 크레딧 정보를 업데이트합니다.
    /// </summary>
    private void UpdateCreditInfo()
    {
        // 기존 패널들 제거
        ClearPanels();

        if (_dataManager?.Finances == null)
        {
            Debug.LogWarning("[CreditInfoPanel] Finances is null. Cannot update credit info.");
            return;
        }

        var finances = _dataManager.Finances;
        var reservation = finances.ReservedDailyExpenses;

        // 총 비용 섹션
        AddPanel("Total Expenses", -reservation.TotalExpenses);
        
        // 유지비 섹션
        if (reservation.maintenanceCost > 0)
        {
            AddPanel("Maintenance", -reservation.maintenanceCost);
            
            // 스레드별 유지비 상세
            var threadCosts = finances.ThreadMaintenanceCosts;
            foreach (var kvp in threadCosts)
            {
                AddPanel($"  - {kvp.Value.reason}", -kvp.Value.cost);
            }
        }

        // 직원 급여
        if (reservation.salaryCost > 0)
        {
            AddPanel("Employee Salary", -reservation.salaryCost);
        }

        // 자원 부족 비용
        if (reservation.resourceShortageCost > 0)
        {
            AddPanel("Resource Shortage Cost", -reservation.resourceShortageCost);
            
            // 자원별 부족 비용 상세
            var resourceCosts = finances.ResourceShortageCosts;
            foreach (var kvp in resourceCosts)
            {
                AddPanel($"  - {kvp.Value.reason}", -kvp.Value.cost);
            }
        }

        // 플레이어 거래 비용
        if (reservation.playerTradeCost > 0)
        {
            AddPanel("Player Trade Cost", -reservation.playerTradeCost);
        }

        // 플레이어 거래 수익
        if (reservation.playerTradeRevenue > 0)
        {
            AddPanel("Player Trade Revenue", reservation.playerTradeRevenue);
        }

        // 순 변화량
        long netDelta = reservation.NetDelta;
        AddPanel("Daily Net Change", netDelta);
    }

    /// <summary>
    /// 새로운 TitleDeltaTextPanel을 추가합니다.
    /// </summary>
    private void AddPanel(string title, long deltaValue)
    {
        if (_titleDeltaTextPanelPrefab == null)
        {
            Debug.LogWarning("[CreditInfoPanel] Prefab or ContentTransform is null.");
            return;
        }

        GameObject panelObj = Instantiate(_titleDeltaTextPanelPrefab, transform);
        TitleDeltaTextPanel panel = panelObj.GetComponent<TitleDeltaTextPanel>();
        
        if (panel != null)
        {
            panel.OnInitialize(title, deltaValue);
            _createdPanels.Add(panel);
        }
    }

    /// <summary>
    /// 생성된 모든 패널을 제거합니다.
    /// </summary>
    private void ClearPanels()
    {
        foreach (var panel in _createdPanels)
        {
            if (panel != null)
            {
                Destroy(panel.gameObject);
            }
        }
        _createdPanels.Clear();

        GameObjectUtils.ClearChildren(transform);
    }
}
