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
    private dataManager _dataManager = null;
    private int _rank = 0; // 순위 정보 저장

    public void OnInitialize(MarketTraderPanel panel, MarketActorEntry actorEntry)
    {
        _traderPanel = panel;
        _actorEntry = actorEntry;
        _isPlayer = false;
        _dataManager = null;
        _rank = 0; // NPC는 MarketActorState.rank 사용

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

        UpdateIndicator();
    }

    public void OnInitializePlayer(MarketTraderPanel panel, dataManager dataManager, int rank = 0)
    {
        _traderPanel = panel;
        _actorEntry = null;
        _isPlayer = true;
        _dataManager = dataManager;
        _rank = rank;

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

    /// <summary>
    /// 버튼 정보를 업데이트합니다 (외부에서 호출 가능).
    /// </summary>
    public void RefreshIndicator()
    {
        UpdateIndicator();
    }

    private void UpdateIndicator()
    {
        if (_changeValueText == null)
        {
            Debug.LogWarning("[MarketTraderBtn] ChangeValueText is null, cannot update indicator.");
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
                
                // 전달받은 순위 사용 (없으면 계산)
                int playerRank = _rank > 0 ? _rank : CalculatePlayerRank(playerWealth);

                // 플레이어는 노란색으로 표시
                indicator = $"#{playerRank} Wealth {ReplaceUtils.FormatNumber(playerWealth)} [Player]";
                color = Color.yellow;
            }
            else
            {
                indicator = "Wealth - [Player]";
                color = Color.yellow;
            }
        }
        else if (_actorEntry != null)
        {
            // NPC 트레이더 정보 표시 (자산 기준)
            var state = _actorEntry.state;
            if (state == null)
            {
                indicator = "No Data";
                color = Color.gray;
            }
            else
            {
                float wealth = state.GetWealth();
                float previousWealth = GetPreviousWealth();
                // MarketActorState.rank 사용 (이미 자산 기준으로 계산됨)
                int rank = state.GetRank();
                float economicPower = state.CalculateEconomicPower();
                string healthStatus = state.GetHealthStatus();
                float dailyTradeVolume = state.GetDailyTradeVolume();
                float dailyNetProfit = state.GetDailyNetProfit();

                // 자산 변화율 계산
                float changePercent = previousWealth > 0f 
                    ? ((wealth - previousWealth) / previousWealth) * 100f 
                    : 0f;
                string changeText = changePercent > 0f ? $"+{changePercent:F1}%" 
                    : changePercent < 0f ? $"{changePercent:F1}%" 
                    : "0%";

                if (rank > 0 && wealth > 0f)
                {
                    // 순위, 자산, 거래액 표시 (자산 기준 순위)
                    indicator = $"#{rank} Wealth {ReplaceUtils.FormatNumber((long)wealth)} ({changeText}) | " +
                        $"Trade {ReplaceUtils.FormatNumber((long)dailyTradeVolume)}";
                    color = GetWealthChangeColor(wealth, previousWealth);
                }
                else if (wealth > 0f)
                {
                    indicator = $"Wealth {ReplaceUtils.FormatNumber((long)wealth)} ({changeText}) | " +
                        $"Trade {ReplaceUtils.FormatNumber((long)dailyTradeVolume)} [{healthStatus}]";
                    color = GetWealthChangeColor(wealth, previousWealth);
                }
                else
                {
                    indicator = $"No Wealth | Trade {ReplaceUtils.FormatNumber((long)dailyTradeVolume)} [{healthStatus}]";
                    color = Color.gray;
                }
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

        return _actorEntry.state.previousWealth;
    }

    private Color GetWealthChangeColor(float currentWealth, float previousWealth)
    {
        VisualManager visualManager = VisualManager.Instance;
        if (visualManager != null)
        {
            return visualManager.GetWealthChangeColor(currentWealth, previousWealth);
        }
        
        // VisualManager가 없을 경우 기본값 반환
        return Color.black;
    }

    /// <summary>
    /// 플레이어 순위를 계산합니다 (순위가 전달되지 않은 경우 사용).
    /// </summary>
    private int CalculatePlayerRank(long playerWealth)
    {
        if (_dataManager?.Market == null)
        {
            return 1;
        }

        var sortedActors = _dataManager.Market.GetActorsSortedByWealth(false);
        int rank = 1;
        foreach (var actor in sortedActors)
        {
            if (actor?.state != null && actor.state.GetWealth() > playerWealth)
            {
                rank++;
            }
            else
            {
                break;
            }
        }
        return rank;
    }
}

