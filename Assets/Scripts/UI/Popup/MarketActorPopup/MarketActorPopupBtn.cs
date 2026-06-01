using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MarketActorPopupBtn : EntryListBtnBase
{
    [SerializeField] private Image _marketActorImage;
    [SerializeField] private TextMeshProUGUI _marketActorNameText;
    [SerializeField] private TextMeshProUGUI _trustText;

    private MarketActorEntry _marketActorEntry;

    public void Init(MarketActorEntry marketActorEntry)
    {
        _marketActorEntry = marketActorEntry;

        if (_marketActorImage != null)
            _marketActorImage.sprite = _marketActorEntry.data.icon;
        if (_marketActorNameText != null)
            _marketActorNameText.text = _marketActorEntry.data.id.Localize(LocalizationUtils.TABLE_MARKET_ACTOR);

        Refresh();
    }

    public override void Refresh()
    {
        if (_marketActorEntry == null)
            return;

        if (_trustText != null)
            _trustText.text = _marketActorEntry.state.trust.ToString();
    }

    protected override void HandleClick()
    {
        if (_marketActorEntry == null) return;
        UIManager.Instance.ShowMarketActorInfoPopup(_marketActorEntry);
    }
}
