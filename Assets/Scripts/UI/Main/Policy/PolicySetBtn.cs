using UnityEngine;
using Evo.UI;
using TMPro;
public class PolicySetBtn : MonoBehaviour
{
    [SerializeField] private Switch _switch;
    [SerializeField] private TextMeshProUGUI _policyTitleText;
    [SerializeField] private TextMeshProUGUI _dailyCostText;
    [SerializeField] private Transform _effectContentTransform;
    private DataManager _dataManager;
    private PolicyEntry _policyEntry;
    public void Init(PolicyEntry policyEntry)
    {
        _dataManager = DataManager.Instance;
        _policyEntry = policyEntry;
        _switch.SetValue(_policyEntry.state.isActive);
        _policyTitleText.text = _policyEntry.data.id.Localize(LocalizationUtils.TABLE_POLICY);
        _dailyCostText.text = _policyEntry.data.dailyCreditCost.ToString("N0");
        DisplayEffects();
    }
    public void OnClick()
    {
        _dataManager.Policy.TrySetPolicyActive(_policyEntry.data.id, !_policyEntry.state.isActive);
        _switch.SetValue(_policyEntry.state.isActive);
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