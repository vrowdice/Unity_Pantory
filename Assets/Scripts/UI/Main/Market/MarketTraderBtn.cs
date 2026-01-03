using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MarketTraderBtn : MonoBehaviour
{
    [SerializeField] private Image _image = null;
    [SerializeField] private TextMeshProUGUI _nameText = null;

    private MarketTraderPanel _traderPanel = null;
    private MarketActorEntry _actorEntry = null;
    private bool _isPlayer = false;

    public void OnInitialize(MarketTraderPanel panel, MarketActorEntry actorEntry)
    {
        _traderPanel = panel;
        _actorEntry = actorEntry;
        _isPlayer = false;

        if (_actorEntry?.data != null)
        {
            if (_image != null)
            {
                _image.sprite = _actorEntry.data.icon;
                _image.enabled = _actorEntry.data.icon != null;
            }

            if (_nameText != null)
            {
                _nameText.text = string.IsNullOrEmpty(_actorEntry.data.displayName)
                    ? _actorEntry.data.id
                    : _actorEntry.data.displayName;
            }
        }
        else
        {
            Debug.LogWarning("[MarketTraderBtn] ActorEntry or data is null.");
            if (_nameText != null)
            {
                _nameText.text = "Unknown";
            }
        }
    }

    public void OnInitializePlayer(MarketTraderPanel panel, DataManager dataManager, int rank = 0)
    {
        _traderPanel = panel;
        _actorEntry = null;
        _isPlayer = true;

        if (_nameText != null)
        {
            _nameText.text = "Player";
        }

        if (_image != null)
        {
            _image.sprite = null;
            _image.enabled = false;
        }
    }

    public void OnClick()
    {
        if (_traderPanel == null)
        {
            return;
        }

        if (_isPlayer)
        {
            return;
        }

        if (_actorEntry != null)
        {
            _traderPanel.HandleTraderButtonClicked(_actorEntry);
        }
    }

    /// <summary>
    /// 버튼 정보를 업데이트합니다 (외부에서 호출 가능).
    /// </summary>
    public void RefreshIndicator()
    {

    }
}

