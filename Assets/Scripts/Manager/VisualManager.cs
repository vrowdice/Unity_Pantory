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


}
