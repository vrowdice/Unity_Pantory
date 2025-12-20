using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ResearchInfoPanel : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _tireText;
    [SerializeField] private TextMeshProUGUI _descriptionText;
    [SerializeField] private Image _iconImage;
    [SerializeField] private Transform _researchEffectScrollViewContentTransform;
    [SerializeField] private TextMeshProUGUI _costPanelText;

    private ResearchEntry _currentResearchEntry;
    private MainUiManager _mainUiManager;
    private dataManager _dataManager;

    private GameManager _gameManager;

    private bool _isSubscribed = false;

    /// <summary>
    /// �г��� �ʱ�ȭ�ϰ� �����͸� �����մϴ�.
    /// </summary>
    public void OnInitialize(ResearchEntry researchEntry, MainUiManager mainUiManager, dataManager dataManager)
    {
        _currentResearchEntry = researchEntry;
        _mainUiManager = mainUiManager;
        _dataManager = dataManager;

        _gameManager = mainUiManager.GameManager;

        SubscribeToDayChanged();
        RefreshAllUI();

        gameObject.SetActive(true);
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
        _nameText.text = _currentResearchEntry.data.displayName;
        _tireText.text = $"Tier {_currentResearchEntry.data.tier}";
        _descriptionText.text = _currentResearchEntry.data.description;
        _iconImage.sprite = _currentResearchEntry.data.icon;
        _costPanelText.text = _currentResearchEntry.data.researchPointCost.ToString();

        GameObjectUtils.ClearChildren(_researchEffectScrollViewContentTransform);
        foreach (EffectData effectState in _currentResearchEntry.data.effects)
        {
            GameObject panelObj = Instantiate(_gameManager.EffectTextPairPanelPrefab, _researchEffectScrollViewContentTransform);
            TextPairPanel panel = panelObj.GetComponent<TextPairPanel>();

            if (panel != null)
            {
                string effectDescription = effectState.displayName ?? effectState.id;
                string changeValue = _dataManager.Effect.FormatEffectValue(effectState.value, effectState.type);
                panel.OnInitialize(effectDescription, changeValue, effectState.value);
            }
        }
    }

    public void OnClickResearchBtn()
    {
        if (!_dataManager.Research.TryUnlockResearch(_currentResearchEntry.data.id))
        {
            _gameManager.ShowWarningPanel("Research cannot be unlocked");
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
