using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PolicyCanvas : MainCanvasPanelBase
{
    [SerializeField] private GameObject _policySetBtnPrefab;
    [SerializeField] private Transform _policySetBtnScrollViewContentTransform;
    [SerializeField] private Transform _policyEffectScrollViewContentTransform;

    private Coroutine _policySetCoroutine;
    private Coroutine _policyEffectCoroutine;

    public override void Init(MainCanvas argUIManager)
    {
        base.Init(argUIManager);

        if (_dataManager?.Policy != null)
        {
            _dataManager.Policy.OnPolicyChanged -= HandlePolicyChanged;
            _dataManager.Policy.OnPolicyChanged += HandlePolicyChanged;
        }

        if (_dataManager?.Time != null)
        {
            _dataManager.Time.OnMonthChanged -= HandleMonthChanged;
            _dataManager.Time.OnMonthChanged += HandleMonthChanged;
        }

        DisplayPolicySetBtns();
        DisplayPolicyEffect();
    }

    private void DisplayPolicySetBtns()
    {
        StaggeredSpawnUtils.Restart(this, ref _policySetCoroutine, DisplayPolicySetBtnsRoutine());
    }

    private IEnumerator DisplayPolicySetBtnsRoutine()
    {
        GameObjectUtils.ClearChildren(_policySetBtnScrollViewContentTransform);

        List<PolicyEntry> policyEntries = new List<PolicyEntry>(_dataManager.Policy.GetAllPolicyEntries().Values);

        yield return StaggeredSpawnUtils.ForEachFrame(policyEntries.Count, i =>
        {
            PolicyEntry policyEntry = policyEntries[i];
            GameObject policySetBtn = Instantiate(_policySetBtnPrefab, _policySetBtnScrollViewContentTransform);
            policySetBtn.GetComponent<PolicySetBtn>().Init(policyEntry);
        });
    }

    private void RefreshPolicySetBtns()
    {
        EntryListPanelUtils.RefreshAllChildren(_policySetBtnScrollViewContentTransform);
    }

    public void DisplayPolicyEffect()
    {
        StaggeredSpawnUtils.Restart(this, ref _policyEffectCoroutine, DisplayPolicyEffectRoutine());
    }

    private IEnumerator DisplayPolicyEffectRoutine()
    {
        if (_policyEffectScrollViewContentTransform == null || _dataManager?.Policy == null)
            yield break;

        PoolingManager pool = PoolingManager.Instance;
        UIManager uiManager = UIManager.Instance;
        if (pool != null)
            pool.ClearChildrenToPool(_policyEffectScrollViewContentTransform);
        else
            GameObjectUtils.ClearChildren(_policyEffectScrollViewContentTransform);

        List<EffectState> activeEffects = new List<EffectState>();
        foreach (PolicyEntry entry in _dataManager.Policy.GetAllPolicyEntries().Values)
        {
            if (entry?.data?.effects == null || entry.state == null || !entry.state.isActive)
                continue;

            foreach (EffectData effect in entry.data.effects)
            {
                if (effect == null)
                    continue;

                activeEffects.Add(new EffectState(effect));
            }
        }

        yield return StaggeredSpawnUtils.ForEachFrame(activeEffects.Count, i =>
        {
            uiManager.CreateEffectTextPairPanel(_policyEffectScrollViewContentTransform, activeEffects[i]);
        });
    }

    private void HandlePolicyChanged()
    {
        RefreshPolicySetBtns();
        DisplayPolicyEffect();
    }

    private void HandleMonthChanged()
    {
        RefreshPolicySetBtns();
    }

    protected override void OnDisable()
    {
        StaggeredSpawnUtils.Stop(this, ref _policySetCoroutine);
        StaggeredSpawnUtils.Stop(this, ref _policyEffectCoroutine);
        base.OnDisable();

        if (_dataManager?.Policy != null)
        {
            _dataManager.Policy.OnPolicyChanged -= HandlePolicyChanged;
        }

        if (_dataManager?.Time != null)
        {
            _dataManager.Time.OnMonthChanged -= HandleMonthChanged;
        }
    }
}