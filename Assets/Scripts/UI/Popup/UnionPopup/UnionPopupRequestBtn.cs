using Evo.UI;
using TMPro;
using UnityEngine;

public class UnionPopupRequestBtn : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _titleText;
    [SerializeField] private TextMeshProUGUI _descriptionText;
    [SerializeField] private TextMeshProUGUI _remainingDaysText;

    [SerializeField] private GameObject _conditionContainer;
    [SerializeField] private GameObject _creditConditionContainer;
    [SerializeField] private TextMeshProUGUI _creditCostText;
    [SerializeField] private GameObject _resourceConditionContainer;
    [SerializeField] private OrderRequireResourceItemPanel _resourceItemPanel;
    [SerializeField] private GameObject _policyConditionContainer;
    [SerializeField] private TextMeshProUGUI _policyRequirementText;
    [SerializeField] private Switch _policySwitch;

    [SerializeField] private GameObject _completeImage;
    [SerializeField] private TextMeshProUGUI _rewardCohesionText;

    private UnionRequestState _requestState;
    private UnionRequestData _requestTemplate;
    private DataManager _dataManager;
    private UnionPopup _unionPopup;

    public void Init(UnionRequestState requestState, DataManager dataManager, UnionPopup unionPopup)
    {
        _requestState = requestState;
        _dataManager = dataManager;
        _unionPopup = unionPopup;
        _requestTemplate = _dataManager?.UnionRequest?.GetUnionRequestData(requestState.id);
        RefreshUI();
    }

    public void RefreshUI()
    {
        if (_requestState == null || _dataManager == null)
        {
            return;
        }

        if (_titleText != null && !string.IsNullOrEmpty(_requestState.id))
        {
            _titleText.text = _requestState.id.Localize(LocalizationUtils.TABLE_MAIN_EVENT);
        }

        if (_descriptionText != null && !string.IsNullOrEmpty(_requestState.id))
        {
            _descriptionText.text = (_requestState.id + LocalizationUtils.KEY_SUFFIX_DESC)
                .Localize(LocalizationUtils.TABLE_MAIN_EVENT);
        }

        if (_remainingDaysText != null)
        {
            _remainingDaysText.text = _requestState.remainingDays.ToString();
        }

        RefreshConditionContainers();
        RefreshRewardUI();
        RefreshCompleteButton();
    }

    public void OnClick()
    {
        if (_dataManager?.UnionRequest == null || _requestState == null)
        {
            return;
        }

        if (!_dataManager.UnionRequest.CanFulfillUnionRequest(_requestState))
        {
            return;
        }

        if (_dataManager.UnionRequest.TryFulfillUnionRequest(_requestState))
        {
            _unionPopup?.RefreshUI();
        }
    }

    private void RefreshConditionContainers()
    {
        UnionRequestConditionType conditionType = GetConditionType();

        if (_conditionContainer != null)
        {
            _conditionContainer.SetActive(conditionType != UnionRequestConditionType.None);
        }

        if (_creditConditionContainer != null)
        {
            _creditConditionContainer.SetActive(conditionType == UnionRequestConditionType.Credit);
        }

        if (_resourceConditionContainer != null)
        {
            _resourceConditionContainer.SetActive(conditionType == UnionRequestConditionType.Resource);
        }

        if (_policyConditionContainer != null)
        {
            _policyConditionContainer.SetActive(conditionType == UnionRequestConditionType.Policy);
        }

        switch (conditionType)
        {
            case UnionRequestConditionType.Credit:
                RefreshCreditConditionUI();
                break;
            case UnionRequestConditionType.Resource:
                RefreshResourceConditionUI();
                break;
            case UnionRequestConditionType.Policy:
                RefreshPolicyConditionUI();
                break;
        }
    }

    private UnionRequestConditionType GetConditionType()
    {
        if (_requestState.requireCredit > 0)
        {
            return UnionRequestConditionType.Credit;
        }

        if (!string.IsNullOrEmpty(_requestState.requireResourceId) && _requestState.requireResourceCount > 0)
        {
            return UnionRequestConditionType.Resource;
        }

        if (!string.IsNullOrEmpty(_requestState.requiredPolicyId))
        {
            return UnionRequestConditionType.Policy;
        }

        return UnionRequestConditionType.None;
    }

    private void RefreshCreditConditionUI()
    {
        if (_creditCostText == null)
        {
            return;
        }

        _creditCostText.text = ReplaceUtils.FormatNumberWithCommas(_requestState.requireCredit);
    }

    private void RefreshResourceConditionUI()
    {
        OrderRequireResourceItemPanel resourcePanel = _resourceItemPanel;
        if (resourcePanel == null && _resourceConditionContainer != null)
        {
            resourcePanel = _resourceConditionContainer.GetComponentInChildren<OrderRequireResourceItemPanel>(true);
        }

        if (resourcePanel == null)
        {
            return;
        }

        ResourceEntry resourceEntry = _dataManager.Resource.GetResourceEntry(_requestState.requireResourceId);
        if (resourceEntry == null)
        {
            return;
        }

        resourcePanel.Init(resourceEntry, _requestState.requireResourceCount);
    }

    private void RefreshPolicyConditionUI()
    {
        PolicyEntry policyEntry = _dataManager.Policy.GetPolicyEntry(_requestState.requiredPolicyId);
        if (_policyRequirementText != null)
        {
            _policyRequirementText.text = policyEntry?.data != null
                ? policyEntry.data.id.Localize(LocalizationUtils.TABLE_POLICY)
                : _requestState.requiredPolicyId;
        }

        if (_policySwitch != null)
        {
            bool isPolicyActive = policyEntry != null && policyEntry.state.isActive;
            _policySwitch.IsOn = isPolicyActive;
            _policySwitch.interactable = false;
        }
    }

    private void RefreshRewardUI()
    {
        if (_rewardCohesionText == null)
        {
            return;
        }

        int rewardCohesion = _requestTemplate != null ? _requestTemplate.rewardCohesion : 0;
        bool hasReward = rewardCohesion > 0;
        _rewardCohesionText.gameObject.SetActive(hasReward);
        if (hasReward)
        {
            _rewardCohesionText.text = $"+{rewardCohesion}%";
        }
    }

    private void RefreshCompleteButton()
    {
        bool canFulfill = _dataManager.UnionRequest.CanFulfillUnionRequest(_requestState);

        Button button = GetComponent<Button>();
        if (button != null)
        {
            button.interactable = canFulfill;
        }

        if (_completeImage != null)
        {
            _completeImage.SetActive(canFulfill);
        }
    }
}
