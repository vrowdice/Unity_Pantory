using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MarketTraderBtn : MonoBehaviour
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

    private void Awake()
    {
        if (_image != null)
        {
            _defaultSprite = _image.sprite;
        }
    }

    public void Init(MarketCanvas panel, MarketActorEntry actorEntry, bool isPlayer = false)
    {
        _marketPanel = panel;
        _actorEntry = actorEntry;
        _isPlayer = isPlayer;

        if (isPlayer) _backgroundImage.color = _marketPanel.Host.VisualManager.DefaultPanelColor;
        else _backgroundImage.color = Color.white;
        _image.sprite = _actorEntry.data.icon != null ? _actorEntry.data.icon : _defaultSprite;
        _nameText.text = _actorEntry.data.id.Localize(LocalizationUtils.TABLE_MARKET_ACTOR);

        RefreshAllUI();
    }

    public void OnClick()
    {
        if (_isPlayer)
        {
            return;
        }

        _marketPanel.OnTraderButtonClicked(_actorEntry);
    }

    /// <summary>
    /// 버튼 정보를 업데이트합니다
    /// </summary>
    public void RefreshAllUI()
    {
        MarketActorState state = _actorEntry.state;
        long change = state.currentChangeWealth;

        _wealthText.text = ReplaceUtils.FormatNumber(state.wealth);

        string sign = (change > 0) ? "+" : "";
        string changeText = change == 0 ? "0" : $"{sign}{ReplaceUtils.FormatNumberWithCommas(change)}";

        _currentChangeWealthText.text = changeText;
        _currentChangeWealthText.color = _marketPanel.Host.VisualManager.GetDeltaColor(change);
    }
}

