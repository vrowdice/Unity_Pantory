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
    private bool _isPlayer = false;
    private GameDataManager _dataManager = null;

    public void OnInitialize(MarketTraderPanel panel, MarketActorEntry actorEntry)
    {
        _traderPanel = panel;
        _actorEntry = actorEntry;
        _isPlayer = false;
        _dataManager = null;

        if (_actorEntry?.data != null)
        {
            if (_image != null)
            {
                _image.sprite = _actorEntry.data.icon;
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

    public void OnInitializePlayer(MarketTraderPanel panel, GameDataManager dataManager)
    {
        _traderPanel = panel;
        _actorEntry = null;
        _isPlayer = true;
        _dataManager = dataManager;

        if (_nameText != null)
        {
            _nameText.text = "Player";
        }

        if (_image != null)
        {
            _image.sprite = null;
            _image.enabled = false;
        }

        UpdateIndicator();
    }

    public void OnClick()
    {
        if (_traderPanel == null)
        {
            return;
        }

        if (_isPlayer)
        {
            // 플레이어는 상세 정보 표시 안 함 (또는 별도 처리)
            return;
        }

        if (_actorEntry != null)
        {
            _traderPanel.HandleTraderButtonClicked(_actorEntry);
        }
    }

    private void UpdateIndicator()
    {
        if (_changeValueText == null)
        {
            return;
        }

        string indicator = "-";
        Color color = Color.white;

        if (_isPlayer)
        {
            // 플레이어 정보 표시
            if (_dataManager != null)
            {
                long playerWealth = _dataManager.Finances.GetCredit();
                var sortedActors = _dataManager.Market.GetActorsSortedByWealth(false);
                
                // 플레이어 순위 계산
                int playerRank = 1;
                foreach (var actor in sortedActors)
                {
                    if (actor?.state != null && actor.state.GetWealth() > playerWealth)
                    {
                        playerRank++;
                    }
                    else
                    {
                        break;
                    }
                }

                indicator = $"#{playerRank} Wealth {ReplaceUtils.FormatNumber(playerWealth)} [Player]";
                color = Color.yellow; // 플레이어는 노란색으로 표시
            }
            else
            {
                indicator = "Wealth - [Player]";
                color = Color.yellow;
            }
        }
        else if (_actorEntry != null)
        {
            // NPC 트레이더 정보 표시
            float wealth = _actorEntry.state?.GetWealth() ?? 0f;
            float previousWealth = GetPreviousWealth();
            int rank = _actorEntry.state?.GetRank() ?? 0;
            string healthStatus = _actorEntry.state?.GetHealthStatus() ?? "Normal";

            if (wealth > 0f && rank > 0)
            {
                float changePercent = previousWealth > 0f 
                    ? ((wealth - previousWealth) / previousWealth) * 100f 
                    : 0f;
                string changeText = changePercent > 0f ? $"+{changePercent:F1}%" 
                    : changePercent < 0f ? $"{changePercent:F1}%" 
                    : "0%";
                
                indicator = $"#{rank} Wealth {ReplaceUtils.FormatNumber((long)wealth)} ({changeText}) [{healthStatus}]";
                color = GetWealthChangeColor(wealth, previousWealth);
            }
            else if (wealth > 0f)
            {
                indicator = $"Wealth {ReplaceUtils.FormatNumber((long)wealth)} [{healthStatus}]";
                color = GetWealthChangeColor(wealth, previousWealth);
            }
            else
            {
                indicator = $"No Wealth [{healthStatus}]";
                color = Color.gray;
            }
        }

        _changeValueText.text = indicator;
        _changeValueText.color = color;
    }

    private float GetPreviousWealth()
    {
        if (_actorEntry?.state == null)
        {
            return 0f;
        }

        float previous = 0f;
        if (_actorEntry.state.provider != null)
        {
            previous += _actorEntry.state.provider.previousWealth;
        }
        if (_actorEntry.state.consumer != null)
        {
            previous += _actorEntry.state.consumer.previousWealth;
        }
        return previous;
    }

    private Color GetWealthChangeColor(float currentWealth, float previousWealth)
    {
        VisualManager visualManager = VisualManager.Instance;
        if (visualManager != null)
        {
            return visualManager.GetWealthChangeColor(currentWealth, previousWealth);
        }
        
        // VisualManager가 없을 경우 기본값 반환
        return Color.white;
    }
}

