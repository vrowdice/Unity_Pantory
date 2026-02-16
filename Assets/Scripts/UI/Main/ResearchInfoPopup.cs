using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ResearchInfoPopup : BasePopup
{
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _tireText;
    [SerializeField] private TextMeshProUGUI _descriptionText;
    [SerializeField] private Image _iconImage;
    [SerializeField] private Transform _researchEffectScrollViewContentTransform;
    [SerializeField] private TextMeshProUGUI _costPanelText;

    private ResearchEntry _currentResearchEntry;
    private MainCanvas _mainUiManager;
    private DataManager _dataManager;

    private GameManager _gameManager;

    private bool _isSubscribed = false;

    public void Init(ResearchEntry researchEntry, MainCanvas mainUiManager)
    {
        base.Init();
        
        _currentResearchEntry = researchEntry;
        _mainUiManager = mainUiManager;
        _dataManager = DataManager.Instance;

        _gameManager = mainUiManager.GameManager;

        SubscribeToDayChanged();
        RefreshAllUI();

        Show();
    }

    private void OnEnable() => SubscribeToDayChanged();
    private void OnDisable() => UnsubscribeFromDayChanged();
    private void OnDestroy() => UnsubscribeFromDayChanged();

    private void SubscribeToDayChanged()
    {
        if (_isSubscribed || _dataManager?.Time == null) return;

        _dataManager.Time.OnDayChanged += OnDayChanged;
        _isSubscribed = true;
    }

    private void UnsubscribeFromDayChanged()
    {
        if (!_isSubscribed || _dataManager?.Time == null) return;

        _dataManager.Time.OnDayChanged -= OnDayChanged;
        _isSubscribed = false;
    }

    private void OnDayChanged()
    {
        if (gameObject.activeSelf) RefreshAllUI();
    }

    public void RefreshAllUI()
    {
        string researchId = _currentResearchEntry.data.id;
        _nameText.text = researchId.Localize(LocalizationUtils.TABLE_RESEARCH);
        _tireText.text = $"Tier {_currentResearchEntry.data.tier}";
        _descriptionText.text = researchId.Localize(LocalizationUtils.TABLE_RESEARCH_DESCRIPTION);
        _iconImage.sprite = _currentResearchEntry.data.icon;
        _costPanelText.text = _currentResearchEntry.data.researchPointCost.ToString();

        PoolingManager.Instance.ClearChildrenToPool(_researchEffectScrollViewContentTransform);
        PoolingManager.Instance.ClearChildrenToPool(_researchEffectScrollViewContentTransform);
        foreach (EffectData effectData in _currentResearchEntry.data.effects)
        {
            _gameManager.CreateEffectTextPairPanel(_researchEffectScrollViewContentTransform, new EffectState(effectData));
        }
    }

    public void OnClickResearchBtn()
    {
        if (!_dataManager.Research.TryUnlockResearch(_currentResearchEntry.data.id))
        {
            _gameManager.ShowWarningPopup(WarningMessage.ResearchCannotUnlock);
        }
        else
        {
            Close();
        }
    }
}
