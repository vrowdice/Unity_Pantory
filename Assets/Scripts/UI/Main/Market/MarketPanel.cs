using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 시장 시스템의 메인 컨트롤러로, 리소스와 거래자 뷰 사이의 전환 및 데이터 갱신을 관리합니다.
/// </summary>
public class MarketPanel : BasePanel
{
    [Header("Action Buttons")]
    [SerializeField] private Transform _marketActionBtnContentTransform;

    [Header("Sub Panels")]
    [SerializeField] private MarketResourcePanel _resourcePanel;
    [SerializeField] private MarketTraderPanel _traderPanel;

    [Header("Scroll Content & Prefabs")]
    [SerializeField] private Transform _marketScrollViewContent;
    [SerializeField] private GameObject _marketResourceBtnPrefab;
    [SerializeField] private GameObject _marketTraderBtnPrefab;

    private bool _isResourceView = true;
    private List<ActionBtn> _actionButtons = new List<ActionBtn>();

    /// <summary>
    /// 마켓 패널 초기화 및 이벤트 구독을 수행합니다.
    /// </summary>
    public override void Init(MainCanvas argUIManager)
    {
        base.Init(argUIManager);

        SetupActionButtons();

        _dataManager.Time.OnDayChanged -= HandleDayChanged;
        _dataManager.Time.OnDayChanged += HandleDayChanged;

        if (_isResourceView)
        {
            ShowResourceView();
        }
        else
        {
            ShowTraderView();
        }
    }

    /// <summary>
    /// 상단 탭 버튼(Resources, Traders)을 생성하고 초기화합니다.
    /// </summary>
    private void SetupActionButtons()
    {
        if (_gameManager?.ActionBtnPrefab == null || _marketActionBtnContentTransform == null)
        {
            return;
        }

        int targetCount = EnumUtils.GetAllEnumValues<MarketPanelType>().Count;
        if (_marketActionBtnContentTransform.childCount == targetCount)
        {
            _actionButtons.Clear();
            foreach (Transform child in _marketActionBtnContentTransform)
            {
                ActionBtn btn = child.GetComponent<ActionBtn>();
                if (btn != null)
                {
                    _actionButtons.Add(btn);
                }
            }
            UpdateActionButtonHighlight();
            return;
        }

        GameObjectUtils.ClearChildren(_marketActionBtnContentTransform);
        _actionButtons.Clear();

        List<MarketPanelType> panelTypes = EnumUtils.GetAllEnumValues<MarketPanelType>();
        foreach (MarketPanelType panelType in panelTypes)
        {
            GameObject btnObj = Instantiate(_gameManager.ActionBtnPrefab, _marketActionBtnContentTransform);
            ActionBtn btn = btnObj.GetComponent<ActionBtn>();
            if (btn != null)
            {
                MarketPanelType capturedType = panelType;
                string localizedName = capturedType.Localize();
                btn.Init(localizedName, () => {
                    OnMarketPanelTypeClick(capturedType);
                    UpdateActionButtonHighlight();
                });
                _actionButtons.Add(btn);
            }
        }
        
        UpdateActionButtonHighlight();
    }

    /// <summary>
    /// 액션 버튼 하이라이트 업데이트
    /// </summary>
    private void UpdateActionButtonHighlight()
    {
        if (_actionButtons.Count == 0) return;

        List<MarketPanelType> panelTypes = EnumUtils.GetAllEnumValues<MarketPanelType>();
        for (int i = 0; i < panelTypes.Count && i < _actionButtons.Count; i++)
        {
            bool isHighlight = false;
            if (panelTypes[i] == MarketPanelType.Resources)
            {
                isHighlight = _isResourceView;
            }
            else if (panelTypes[i] == MarketPanelType.Traders)
            {
                isHighlight = !_isResourceView;
            }
            _actionButtons[i].SetHighlight(isHighlight);
        }
    }

    /// <summary>
    /// 마켓 패널 타입 버튼 클릭 핸들러
    /// </summary>
    private void OnMarketPanelTypeClick(MarketPanelType panelType)
    {
        switch (panelType)
        {
            case MarketPanelType.Resources:
                ShowResourceView();
                break;
            case MarketPanelType.Traders:
                ShowTraderView();
                break;
        }
    }

    /// <summary>
    /// 리소스 목록 중 하나가 클릭되었을 때 상세 정보 패널에 전달합니다.
    /// </summary>
    /// <param name="entry">선택된 리소스 데이터</param>
    public void OnResourceButtonClicked(ResourceEntry entry)
    {
        _resourcePanel.ChangeResource(entry);
    }

    public void OnTraderButtonClicked(MarketActorEntry entry)
    {
        _traderPanel.ChangeActor(entry);
    }

    /// <summary>
    /// 화면을 리소스 목록 뷰로 전환합니다.
    /// </summary>
    private void ShowResourceView()
    {
        _isResourceView = true;
        TogglePanels(true);

        _resourcePanel.Init(this);
        RefreshResourceList();
        UpdateActionButtonHighlight();
    }

    /// <summary>
    /// 화면을 거래자 목록 뷰로 전환합니다.
    /// </summary>
    private void ShowTraderView()
    {
        _isResourceView = false;
        TogglePanels(false);

        _traderPanel.Init(this);
        RefreshTraderList();
        UpdateActionButtonHighlight();
    }

    /// <summary>
    /// 뷰 상태에 따라 서브 패널들의 활성화 상태를 제어합니다.
    /// </summary>
    private void TogglePanels(bool isResource)
    {
        _resourcePanel.gameObject.SetActive(isResource);
        _traderPanel.gameObject.SetActive(!isResource);
    }

    /// <summary>
    /// 날짜가 변경되었을 때 현재 활성화된 뷰의 정보를 갱신합니다.
    /// </summary>
    private void HandleDayChanged()
    {
        RefreshButtons();
        _resourcePanel.HandleDayChanged();
        _traderPanel.HandleDayChanged();
    }

    /// <summary>
    /// 현재 리스트에 생성되어 있는 모든 리소스 버튼의 UI(가격, 거래량 등)를 갱신합니다.
    /// </summary>
    public void RefreshButtons()
    {
        if (_isResourceView)
        {
            foreach (Transform child in _marketScrollViewContent)
            {
                child.GetComponent<MarketResourceBtn>().RefreshAllUI();
            }
        }
        else
        {
            RefreshTraderList();
        }
    }

    /// <summary>
    /// 리소스 데이터를 기반으로 스크롤 목록을 완전히 새로고침합니다.
    /// </summary>
    private void RefreshResourceList()
    {
        GameObjectUtils.ClearChildren(_marketScrollViewContent);

        Dictionary<string, ResourceEntry> resources = _dataManager.Resource.GetAllResources();
        foreach (ResourceEntry entry in resources.Values)
        {
            GameObject btnObj = Instantiate(_marketResourceBtnPrefab, _marketScrollViewContent);
            MarketResourceBtn resourceBtn = btnObj.GetComponent<MarketResourceBtn>();
            resourceBtn.Init(this, entry);
        }
    }

    /// <summary>
    /// 거래자 데이터를 기반으로 스크롤 목록을 새로고침합니다.
    /// 자산(wealth)에 따라 내림차순으로 정렬됩니다.
    /// </summary>
    private void RefreshTraderList()
    {
        GameObjectUtils.ClearChildren(_marketScrollViewContent);

        Dictionary<string, MarketActorEntry> traders = _dataManager.MarketActor.GetAllMarketActors();
        
        List<MarketActorEntry> sortedTraders = new List<MarketActorEntry>(traders.Values);
        sortedTraders.Sort((a, b) => b.state.wealth.CompareTo(a.state.wealth));
        
        foreach (MarketActorEntry entry in sortedTraders)
        {
            if (entry.data.marketActorType != MarketActorType.Company) continue;

            GameObject btnObj = Instantiate(_marketTraderBtnPrefab, _marketScrollViewContent);
            MarketTraderBtn traderBtn = btnObj.GetComponent<MarketTraderBtn>();
            traderBtn.Init(this, entry);
        }
    }
}