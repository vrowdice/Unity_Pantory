using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugPopup : PopupBase
{
    [SerializeField] private Transform _debugBtnContentTransform;
    [SerializeField] private GameObject _debugBtnPrefab;

    private Coroutine _buildButtonsCoroutine;

    public override void Init()
    {
        base.Init();
        StaggeredSpawnUtils.Restart(this, ref _buildButtonsCoroutine, BuildDebugButtonsRoutine());
        Show();
    }

    public override void Close()
    {
        StaggeredSpawnUtils.Stop(this, ref _buildButtonsCoroutine);
        base.Close();
    }

    private IEnumerator BuildDebugButtonsRoutine()
    {
        if (_debugBtnContentTransform == null || _debugBtnPrefab == null)
        {
            Debug.LogError("[DebugPopup] Button content or prefab is missing.");
            yield break;
        }

        for (int i = _debugBtnContentTransform.childCount - 1; i >= 0; i--)
            Destroy(_debugBtnContentTransform.GetChild(i).gameObject);

        List<(string label, Action action)> buttonDefs = new List<(string label, Action action)>
        {
            ("Credit +1,000,000", () => DataManager.Instance.Finances.ModifyCredit(1_000_000)),
            ("All Resource +500", AddAllResourceCounts),
            ("RP +10,000", () => DataManager.Instance.Research.AddResearchPoints(10_000)),
            ("Unlock All Research", UnlockAllResearch),
            ("All Employee +10", AddAllEmployees),
            ("All Employee Satisfaction 100", SetAllEmployeeSatisfactionMax),
            ("All Employee Salary Level 4", SetAllEmployeeSalaryLevelMax),
            ("All Market Actor Trust +10", BoostAllMarketTrust),
            ("Complete All Active Orders", CompleteAllActiveOrders),
            ("Time x10", () => DataManager.Instance.Time.SetTimeSpeed(10f)),
            ("Pause Time", () => DataManager.Instance.Time.PauseTime()),
            ("Resume Time", () => DataManager.Instance.Time.ResumeTime()),
        };

        yield return StaggeredSpawnUtils.ForEachFrame(buttonDefs.Count, i =>
        {
            (string label, Action action) def = buttonDefs[i];
            AddButton(def.label, def.action);
        });
    }

    private void AddButton(string label, Action onClickAction)
    {
        GameObject buttonObj = Instantiate(_debugBtnPrefab, _debugBtnContentTransform);
        DebugPopupBtn debugButton = buttonObj.GetComponent<DebugPopupBtn>();
        if (debugButton == null)
        {
            Debug.LogWarning("[DebugPopup] DebugPopupBtn component not found on prefab.");
            return;
        }

        debugButton.Init(label, onClickAction);
    }

    private void AddAllResourceCounts()
    {
        Dictionary<string, ResourceEntry> resources = DataManager.Instance.Resource.GetAllResources();
        foreach (KeyValuePair<string, ResourceEntry> pair in resources)
            DataManager.Instance.Resource.ModifyResourceCount(pair.Key, 500);
    }

    private void UnlockAllResearch()
    {
        List<ResearchEntry> entries = DataManager.Instance.Research.GetAllResearchEntries();
        if (entries.Count == 0) return;

        DataManager.Instance.Research.AddResearchPoints(10_000_000);
        foreach (ResearchEntry entry in entries)
        {
            if (entry == null || entry.data == null || entry.state == null) continue;
            if (entry.state.isCompleted) continue;
            if (!entry.state.isUnlocked) continue;
            DataManager.Instance.Research.TryUnlockResearch(entry.data.id);
        }
    }

    private void AddAllEmployees()
    {
        foreach (EmployeeType employeeType in Enum.GetValues(typeof(EmployeeType)))
        {
            EmployeeEntry entry = DataManager.Instance.Employee.GetEmployeeEntry(employeeType);
            if (entry == null || entry.state == null) continue;
            DataManager.Instance.Employee.SetEmployeeCount(employeeType, entry.state.count + 10);
        }
    }

    private void SetAllEmployeeSatisfactionMax()
    {
        foreach (EmployeeType employeeType in Enum.GetValues(typeof(EmployeeType)))
            DataManager.Instance.Employee.SetEmployeeSatisfaction(employeeType, 100f);
    }

    private void SetAllEmployeeSalaryLevelMax()
    {
        foreach (EmployeeType employeeType in Enum.GetValues(typeof(EmployeeType)))
            DataManager.Instance.Employee.SetEmployeeSalaryLevel(employeeType, 4);
    }

    private void BoostAllMarketTrust()
    {
        Dictionary<string, MarketActorEntry> actors = DataManager.Instance.MarketActor.GetAllMarketActors();
        foreach (KeyValuePair<string, MarketActorEntry> pair in actors)
            DataManager.Instance.MarketActor.ModifyMarketActorTrust(pair.Key, 10);
    }

    private void CompleteAllActiveOrders()
    {
        List<OrderState> orders = DataManager.Instance.Order.GetActiveOrderList();
        foreach (OrderState order in orders)
        {
            if (order == null) continue;

            DataManager.Instance.Order.AcceptAndCompleteOrder(order);
            if (order.isAccepted)
                DataManager.Instance.Order.AcceptAndCompleteOrder(order);
        }
    }
}
