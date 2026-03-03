using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ResearchInfoPopup : PopupBase
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

    public void Init(ResearchEntry researchEntry)
    {
        base.Init();
        
        _gameManager = GameManager.Instance;
        _dataManager = DataManager.Instance;
        _currentResearchEntry = researchEntry;

        _dataManager.Time.OnDayChanged -= OnDayChanged;
        _dataManager.Time.OnDayChanged += OnDayChanged;

        RefreshAllUI();

        Show();
    }

    private void OnDisable()
    {
        if (_dataManager?.Time == null) return;
        _dataManager.Time.OnDayChanged -= OnDayChanged;
    }

    private void OnDayChanged()
    {
        if (gameObject.activeSelf) RefreshAllUI();
    }

    public void RefreshAllUI()
    {
        string researchId = _currentResearchEntry.data.id;
        _nameText.text = researchId.Localize(LocalizationUtils.TABLE_RESEARCH);
        _descriptionText.text = researchId.Localize(LocalizationUtils.TABLE_RESEARCH_DESCRIPTION);
        _iconImage.sprite = _currentResearchEntry.data.icon;
        _costPanelText.text = _currentResearchEntry.data.researchPointCost.ToString();

        PoolingManager.Instance.ClearChildrenToPool(_researchEffectScrollViewContentTransform);
        PoolingManager.Instance.ClearChildrenToPool(_researchEffectScrollViewContentTransform);
        foreach (EffectData effectData in _currentResearchEntry.data.effects)
        {
            UIManager.Instance.CreateEffectTextPairPanel(_researchEffectScrollViewContentTransform, new EffectState(effectData));
        }
    }

    public void OnClickResearchBtn()
    {
        if (!_dataManager.Research.TryUnlockResearch(_currentResearchEntry.data.id))
        {
            UIManager.Instance.ShowWarningPopup(WarningMessage.ResearchCannotUnlock);
        }
        else
        {
            Close();
        }
    }
}
