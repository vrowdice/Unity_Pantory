using UnityEngine;
using Evo.UI;
using TMPro;
public class PolicySetBtn : MonoBehaviour
{
    [SerializeField] private Switch _switch;
    [SerializeField] private TextMeshProUGUI _policyTitleText;
    [SerializeField] private TextMeshProUGUI _remainingMonthsText;
    [SerializeField] private TextMeshProUGUI _dailyCostText;
    [SerializeField] private Transform _effectContentTransform;
    private DataManager _dataManager;
    private PolicyEntry _policyEntry;
    public void Init(PolicyEntry policyEntry)
    {
        _dataManager = DataManager.Instance;
        _policyEntry = policyEntry;
        RefreshUI();
        DisplayEffects();
    }
    public void OnClick()
    {
        bool changed = _dataManager.Policy.TrySetPolicyActive(_policyEntry.data.id, !_policyEntry.state.isActive);
        if (!changed)
        {
            if (_policyEntry?.state != null && _policyEntry.state.remainingMonths > 0)
            {
                UIManager.Instance.ShowWarningPopup(WarningMessage.PolicyChangeLocked);
            }
            RefreshUI();
            return;
        }

        RefreshUI();
    }

    public void RefreshUI()
    {
        if (_policyEntry?.data == null || _policyEntry.state == null || _dataManager?.Policy == null)
        {
            return;
        }

        _switch.SetValue(_policyEntry.state.isActive);
        _policyTitleText.text = _policyEntry.data.id.Localize(LocalizationUtils.TABLE_POLICY);

        long scaledDailyCost = _dataManager.Policy.CalculateScaledDailyPolicyCost(_policyEntry.data.dailyCreditCost);
        _dailyCostText.text = scaledDailyCost.ToString("N0");

        if (_remainingMonthsText != null)
        {
            int remaining = _policyEntry.state.remainingMonths;
            _remainingMonthsText.text = remaining > 0 ? remaining.ToString() : string.Empty;
        }
    }
    private void DisplayEffects()
    {
        if (_effectContentTransform == null || _policyEntry?.data?.effects == null)
        {
            return;
        }
        PoolingManager pool = PoolingManager.Instance;
        UIManager uiManager = UIManager.Instance;
        pool.ClearChildrenToPool(_effectContentTransform);
        foreach (EffectData effect in _policyEntry.data.effects)
        {
            if (effect == null)
            {
                continue;
            }
            uiManager.CreateEffectTextPairPanel(_effectContentTransform, new EffectState(effect));
        }
    }
}