using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MarketActorPopupBtn : MonoBehaviour
{
    [SerializeField] private Image _marketActorImage;
    [SerializeField] private TextMeshProUGUI _marketActorNameText;
    [SerializeField] private TextMeshProUGUI _trustText;

    private MarketActorEntry _marketActorEntry;
    private MainCanvas _mainCanvas;

    public void Init(MarketActorEntry marketActorEntry, MainCanvas mainCanvas)
    {
        _marketActorEntry = marketActorEntry;
        _mainCanvas = mainCanvas;

        UpdateUI();
    }

    private void UpdateUI()
    {
        if (_marketActorEntry == null) return;

        if (_marketActorImage != null) _marketActorImage.sprite = _marketActorEntry.data.icon;
        if (_marketActorNameText != null) _marketActorNameText.text = _marketActorEntry.data.id.Localize(LocalizationUtils.TABLE_MARKET_ACTOR);
        if (_trustText != null) _trustText.text = _marketActorEntry.state.trust.ToString();
    }

    public void OnClick()
    {
        if (_marketActorEntry == null || _mainCanvas == null) return;

        _mainCanvas.ShowMarketActorInfoPopup(_marketActorEntry);
    }
}
