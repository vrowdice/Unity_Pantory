using System.Collections.Generic;
using UnityEngine;

public class VisualManager : Singleton<VisualManager>
{
    public Color ValidColor => _validColor;
    public Color InvalidColor => _invalidColor;
    public Color ProfitColor => _profitColor;
    public Color LossColor => _lossColor;
    public Color ManagementSufficientColor => _managementSufficientColor;
    public Color ManagementInsufficientColor => _managementInsufficientColor;
    
    public Color ThreadPlacementOutlineColor => _threadPlacementOutlineColor;
    public Color ThreadRemovalHighlightColor => _threadRemovalHighlightColor;
    public Color ThreadPreviewValidColor => _threadPreviewValidColor;
    public Color ThreadPreviewInvalidColor => _threadPreviewInvalidColor;
    public float ThreadPreviewAlpha => _threadPreviewAlpha;

    [SerializeField]private Color _validColor = new Color(0, 1, 0, 0.2f);
    [SerializeField]private Color _invalidColor = new Color(1, 0, 0, 0.2f);
    [SerializeField]private Color _profitColor = Color.blue;
    [SerializeField]private Color _lossColor = Color.red;
    [SerializeField]private Color _managementSufficientColor = Color.green;
    [SerializeField]private Color _managementInsufficientColor = Color.red;
    
    [Header("Thread Colors")]
    [SerializeField]private Color _threadPlacementOutlineColor = new Color(0.2f, 0.8f, 1f, 0.8f);
    [SerializeField]private Color _threadRemovalHighlightColor = new Color(1f, 0.4f, 0.4f, 0.9f);
    [SerializeField]private Color _threadPreviewValidColor = Color.green;
    [SerializeField]private Color _threadPreviewInvalidColor = Color.red;
    [SerializeField]private float _threadPreviewAlpha = 0.6f;

    [SerializeField] private List<StringSpritePair> _panelIconList = new List<StringSpritePair>();

    private Dictionary<string, Sprite> _panelIconDict = new Dictionary<string, Sprite>();

    public void Init()
    {
        _panelIconDict.Clear();
        foreach (StringSpritePair pair in _panelIconList)
        {
            if (!_panelIconDict.ContainsKey(pair.String))
            {
                _panelIconDict.Add(pair.String, pair.Sprite);
            }
        }
    }

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

    public Color GetWealthChangeColor(float currentWealth, float previousWealth)
    {
        if (previousWealth <= 0f)
        {
            return Color.black;
        }

        float change = currentWealth - previousWealth;
        
        if (change > 0f)
        {
            return ProfitColor;
        }
        else if (change < 0f)
        {
            return LossColor;
        }
        else
        {
            return Color.black;
        }
    }

    public Color GetBudgetColor(float budget)
    {
        if (budget >= 1000f)
        {
            return ManagementSufficientColor;
        }
        if (budget <= 100f)
        {
            return ManagementInsufficientColor;
        }
        return Color.white;
    }

    public Sprite GetMainPanelIcon(string panelTypeStr)
    {
        _panelIconDict.TryGetValue(panelTypeStr, out Sprite icon);
        return icon;
    }
}
