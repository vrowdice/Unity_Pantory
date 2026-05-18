using UnityEngine;

public class PolicyCanvas : MainCanvasPanelBase
{
    [SerializeField] private GameObject _policySetBtnPrefab;
    [SerializeField] private Transform _policySetBtnScrollViewContentTransform;
    [SerializeField] private Transform _policyEffectScrollViewContentTransform;

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
        GameObjectUtils.ClearChildren(_policySetBtnScrollViewContentTransform);

        foreach (PolicyEntry policyEntry in _dataManager.Policy.GetAllPolicyEntries().Values)
        {
            GameObject policySetBtn = Instantiate(_policySetBtnPrefab, _policySetBtnScrollViewContentTransform);
            policySetBtn.GetComponent<PolicySetBtn>().Init(policyEntry);
        }
    }

    private void RefreshPolicySetBtns()
    {
        if (_policySetBtnScrollViewContentTransform == null)
        {
            return;
        }

        foreach (Transform child in _policySetBtnScrollViewContentTransform)
        {
            PolicySetBtn policySetBtn = child.GetComponent<PolicySetBtn>();
            if (policySetBtn != null)
            {
                policySetBtn.RefreshUI();
            }
        }
    }

    public void DisplayPolicyEffect()
    {
        if (_policyEffectScrollViewContentTransform == null || _dataManager?.Policy == null)
        {
            return;
        }

        PoolingManager pool = PoolingManager.Instance;
        UIManager uiManager = UIManager.Instance;
        if (pool != null)
        {
            pool.ClearChildrenToPool(_policyEffectScrollViewContentTransform);
        }
        else
        {
            GameObjectUtils.ClearChildren(_policyEffectScrollViewContentTransform);
        }

        foreach (PolicyEntry entry in _dataManager.Policy.GetAllPolicyEntries().Values)
        {
            if (entry?.data?.effects == null || entry.state == null || !entry.state.isActive)
            {
                continue;
            }

            foreach (EffectData effect in entry.data.effects)
            {
                if (effect == null) continue;
                uiManager.CreateEffectTextPairPanel(_policyEffectScrollViewContentTransform, new EffectState(effect));
            }
        }
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

    private void OnDisable()
    {
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