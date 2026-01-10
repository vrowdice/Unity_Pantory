using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MarketTraderBtn : MonoBehaviour
{
    [SerializeField] private Image _image = null;
    [SerializeField] private TextMeshProUGUI _nameText = null;
    [SerializeField] private TextMeshProUGUI _wealthText = null;
    [SerializeField] private TextMeshProUGUI _currentChangeWealthText = null;

    private MarketPanel _marketPanel = null;
    private MarketActorEntry _actorEntry = null;
    private bool _isPlayer = false;

    public void Init(MarketPanel panel, MarketActorEntry actorEntry)
    {
        _marketPanel = panel;
        _actorEntry = actorEntry;
        _isPlayer = false;

        _image.sprite = _actorEntry.data.icon;
        _nameText.text = _actorEntry.data.displayName;

        RefreshAllUI();
    }

    public void InitPlayer(MarketPanel panel, int rank = 0)
    {
        _marketPanel = panel;
        _actorEntry = null;
        _isPlayer = true;

        _nameText.text = "Player";
        _image.sprite = null;
        _image.enabled = false;
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
        _currentChangeWealthText.color = VisualManager.Instance.GetDeltaColor(change);
    }
}

