using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 시장의 리소스 상세 정보 및 거래 입력을 담당하는 패널 클래스입니다.
/// </summary>
public class MarketResurcePanel : MonoBehaviour
{
    private DataManager _dataManager;
    private MarketPanel _marketPanel;

    [Header("Components")]
    [SerializeField] private WindowGraph _windowGraph;
    [SerializeField] private TMP_InputField _resouceTradeInputField;

    [Header("Info Panel UI")]
    [SerializeField] private Image _resouceImage;
    [SerializeField] private TextMeshProUGUI _resourceNameText;
    [SerializeField] private TextMeshProUGUI _resourceStorageText;
    [SerializeField] private TextMeshProUGUI _resourcePriceText;

    private ResourceEntry _selectedResourceEntry;
    private string _selectedResourceId = string.Empty;
    public string GetSelectedResourceId() => _selectedResourceId;

    /// <summary>
    /// 패널을 초기화하고 날짜 변경 이벤트를 구독합니다.
    /// </summary>
    /// <param name="dataManager">데이터 관리자 참조</param>
    /// <param name="marketPanel">부모 마켓 패널 참조</param>
    public void Init(DataManager dataManager, MarketPanel marketPanel)
    {
        _dataManager = dataManager;
        _marketPanel = marketPanel;

        _dataManager.Time.OnDayChanged -= HandleDayChanged;
        _dataManager.Time.OnDayChanged += HandleDayChanged;

        if (_selectedResourceEntry == null)
        {
            _selectedResourceEntry = _dataManager.Resource.GetResourceEntry("iron_ore");
        }

        _windowGraph.Init();
        RefreshUI();
    }

    /// <summary>
    /// 리소스 목록에서 버튼 클릭 시 호출되어 상세 정보를 갱신합니다.
    /// </summary>
    /// <param name="entry">선택된 리소스 엔트리</param>
    public void HandleResourceButtonClicked(ResourceEntry entry)
    {
        if (entry == null) return;

        _selectedResourceEntry = entry;
        _selectedResourceId = entry.data.id;

        _resouceTradeInputField.text = entry.state.marketDeltaCount.ToString();

        RefreshUI();
    }

    /// <summary>
    /// 거래량 입력 필드의 값이 변경될 때 호출됩니다. (UI Event)
    /// </summary>
    public void ResouceTradeInputFieldChanged()
    {
        if (_selectedResourceEntry == null) return;

        string inputText = _resouceTradeInputField.text;

        if (int.TryParse(inputText, out int tradeAmount))
        {
            _selectedResourceEntry.state.marketDeltaCount = tradeAmount;
            _marketPanel.RefreshResourceButtons();
        }
    }

    public void ChangeResourceTradeInputFieldValue(int value)
    {
        _resouceTradeInputField.text = (int.Parse(_resouceTradeInputField.text) + value).ToString();
        ResouceTradeInputFieldChanged();
    }

    /// <summary>
    /// 날짜가 변경되었을 때 그래프와 상세 정보를 새로고침합니다.
    /// </summary>
    private void HandleDayChanged()
    {
        RefreshUI();
    }

    /// <summary>
    /// 그래프와 텍스트 정보를 포함한 모든 UI 요소를 최신화합니다.
    /// </summary>
    private void RefreshUI()
    {
        if (_selectedResourceEntry == null) return;

        ResourceData data = _selectedResourceEntry.data;
        ResourceState state = _selectedResourceEntry.state;

        _resouceImage.sprite = data.icon;
        _resourceNameText.text = data.displayName;
        _resourceStorageText.text = state.count.ToString("N0");

        string priceText = $"{state.currentValue:N0}";
        if (state.currentChangeValue != 0f)
        {
            string deltaSymbol = state.currentChangeValue > 0 ? "+" : "";
            priceText += $" ({deltaSymbol}{state.currentChangeValue:F2})";
        }
        _resourcePriceText.text = priceText;

        _windowGraph.ShowGraph(_selectedResourceEntry.state.PriceHistory);
        _resouceTradeInputField.text = _selectedResourceEntry.state.marketDeltaCount.ToString();
    }
}