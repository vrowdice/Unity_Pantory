using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MarketTraderBtn : MonoBehaviour
{
    [SerializeField] private Image _image = null;
    [SerializeField] private TextMeshProUGUI _nameText = null;
    [SerializeField] private TextMeshProUGUI _changeValueText = null;

    private MarketTraderPanel _traderPanel = null;
    private MarketActorEntry _actorEntry = null;

    public void OnInitialize(MarketTraderPanel panel, MarketActorEntry actorEntry)
    {
        _traderPanel = panel;
        _actorEntry = actorEntry;

        if (_actorEntry?.data != null)
        {
            if (_image != null)
            {
                _image.sprite = _actorEntry.data.portrait;
            }

            if (_nameText != null)
            {
                _nameText.text = string.IsNullOrEmpty(_actorEntry.data.displayName)
                    ? _actorEntry.data.id
                    : _actorEntry.data.displayName;
            }
        }

        UpdateIndicator();
    }

    public void OnClick()
    {
        if (_traderPanel == null || _actorEntry == null)
        {
            return;
        }

        _traderPanel.HandleTraderButtonClicked(_actorEntry);
    }

    private void UpdateIndicator()
    {
        if (_changeValueText == null || _actorEntry == null)
        {
            return;
        }

        // Provider 중심: priceDelta, Consumer 중심: currentBudget
        string indicator = "-";
        Color color = Color.white;

        bool isProvider = _actorEntry.data.roles.HasFlag(MarketRoleFlags.Provider) && _actorEntry.state?.provider != null;
        bool isConsumer = _actorEntry.data.roles.HasFlag(MarketRoleFlags.Consumer) && _actorEntry.state?.consumer != null;

        if (isProvider)
        {
            float delta = _actorEntry.state.provider.priceDelta;
            indicator = $"Tendency {delta:+0.##;-0.##;0}";
            color = GetDeltaColor(delta);
        }
        else if (isConsumer)
        {
            float budget = _actorEntry.state.consumer.currentBudget;
            indicator = $"Budget {budget:N0}";
            color = GetBudgetColor(budget);
        }

        _changeValueText.text = indicator;
        _changeValueText.color = color;
    }

    private Color GetDeltaColor(float delta)
    {
        if (delta > 0f)
        {
            return Color.green;
        }

        if (delta < 0f)
        {
            return Color.red;
        }

        return Color.white;
    }

    private Color GetBudgetColor(float budget)
    {
        // 간단한 구간 색
        if (budget >= 1000f) return Color.green;
        if (budget <= 100f) return Color.red;
        return Color.white;
    }
}
