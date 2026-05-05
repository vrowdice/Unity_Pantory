using UnityEngine;

public class PolicyCanvas : MainCanvasPanelBase
{
    [SerializeField] private GameObject policySetBtnPrefab;
    [SerializeField] private Transform PolicySetBtnScrollViewContentTransform;

    public override void Init(MainCanvas argUIManager)
    {
        base.Init(argUIManager);

        DisplayPolicySetBtns();
    }

    private void DisplayPolicySetBtns()
    {
        GameObjectUtils.ClearChildren(PolicySetBtnScrollViewContentTransform);

        foreach (PolicyEntry policyEntry in _dataManager.Policy.GetAllPolicyEntries().Values)
        {
            GameObject policySetBtn = Instantiate(policySetBtnPrefab, PolicySetBtnScrollViewContentTransform);
            policySetBtn.GetComponent<PolicySetBtn>().Init(policyEntry);
        }
    }
}