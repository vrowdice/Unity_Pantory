using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MarketActorPopupBtn : MonoBehaviour
{
    [SerializeField] private Image _marketActorImage;
    [SerializeField] private TextMeshProUGUI _marketActorNameText;
    [SerializeField] private TextMeshProUGUI _trustText;

    private MarketActorEntry _marketActorEntry;

    public void Init(MarketActorEntry marketActorEntry)
    {
        _marketActorEntry = marketActorEntry;
    }

    public void OnClick()
    {

    }
}
