using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MarketTraderBtn : BtnBase
{
    [SerializeField] private Image _image = null;
    [SerializeField] private Image _backgroundImage = null;
    [SerializeField] private TextMeshProUGUI _nameText = null;
    [SerializeField] private TextMeshProUGUI _wealthText = null;
    [SerializeField] private TextMeshProUGUI _currentChangeWealthText = null;

    private MarketCanvas _marketPanel = null;
    private MarketActorEntry _actorEntry = null;
    private bool _isPlayer = false;
    private Sprite _defaultSprite = null;

    public void Init(MarketCanvas panel, MarketActorEntry actorEntry, bool isPlayer = false)
    {
        if (_defaultSprite == null && _image != null)
        {
            _defaultSprite = _image.sprite;
        }

        _marketPanel = panel;
        _actorEntry = actorEntry;
        _isPlayer = isPlayer;

        if (isPlayer) _backgroundImage.color = _marketPanel.VisualManager.DefaultPanelColor;
        else _backgroundImage.color = Color.white;
        _image.sprite = _actorEntry.data.icon != null ? _actorEntry.data.icon : _defaultSprite;
        _nameText.text = _actorEntry.data.id.Localize(LocalizationUtils.TABLE_MARKET_ACTOR);

        RefreshAllUI();
    }

    protected override void HandleClick()
    {
        if (_isPlayer)
        {
            return;
        }

        _marketPanel.OnTraderButtonClicked(_actorEntry);
    }

    public void RefreshAllUI()
    {
        MarketActorState state = _actorEntry.state;
        long change = state.currentChangeWealth;

        _wealthText.text = ReplaceUtils.FormatNumber(state.wealth);

        string sign = (change > 0) ? "+" : "";
        string changeText = change == 0 ? "0" : $"{sign}{ReplaceUtils.FormatNumberWithCommas(change)}";

        _currentChangeWealthText.text = changeText;
        _currentChangeWealthText.color = _marketPanel.VisualManager.GetDeltaColor(change);
    }
}
