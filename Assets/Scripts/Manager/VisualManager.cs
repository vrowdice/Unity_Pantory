using UnityEngine;

public class VisualManager : MonoBehaviour
{
    public static VisualManager Instance { get; private set; }

    public Color ValidColor => _validColor;
    public Color InvalidColor => _invalidColor;
    public Color ProfitColor => _profitColor;  // 흑자 색상 (양수, 증가)
    public Color LossColor => _lossColor;     // 적자 색상 (음수, 감소)

    [SerializeField]private Color _validColor = new Color(0, 1, 0, 0.2f);
    [SerializeField]private Color _invalidColor = new Color(1, 0, 0, 0.2f);
    [SerializeField]private Color _profitColor = Color.blue;   // 흑자: 파란색
    [SerializeField]private Color _lossColor = Color.red;       // 적자: 빨간색

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;

            DontDestroyOnLoad(gameObject);
        }
    }

    /// <summary>
    /// 델타 값에 따른 색상을 반환합니다 (양수: 흑자, 음수: 적자, 0: 흰색)
    /// </summary>
    /// <param name="delta">변화량</param>
    /// <returns>색상</returns>
    public Color GetDeltaColor(float delta)
    {
        if (delta > 0f)
        {
            return ProfitColor;
        }

        if (delta < 0f)
        {
            return LossColor;
        }

        return Color.white;
    }

    /// <summary>
    /// 재산 변화에 따른 색상을 반환합니다 (증가: 흑자, 감소: 적자, 변화없음/데이터없음: 흰색)
    /// </summary>
    /// <param name="currentWealth">현재 재산</param>
    /// <param name="previousWealth">이전 재산</param>
    /// <returns>색상</returns>
    public Color GetWealthChangeColor(float currentWealth, float previousWealth)
    {
        // 전일 데이터가 없으면 흰색
        if (previousWealth <= 0f)
        {
            return Color.white;
        }

        float change = currentWealth - previousWealth;
        
        if (change > 0f)
        {
            // 증가 → 흑자 색상
            return ProfitColor;
        }
        else if (change < 0f)
        {
            // 감소 → 적자 색상
            return LossColor;
        }
        else
        {
            // 변화 없음 → 흰색
            return Color.white;
        }
    }
}
