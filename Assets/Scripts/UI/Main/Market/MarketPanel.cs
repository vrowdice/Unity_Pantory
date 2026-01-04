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
    [SerializeField] private MarketResurcePanel _resourcePanel;
    [SerializeField] private MarketTraderPanel _traderPanel;

    [Header("Scroll Content & Prefabs")]
    [SerializeField] private Transform _marketScrollViewContent;
    [SerializeField] private GameObject _marketResourceBtnPrefab;
    [SerializeField] private GameObject _marketTraderBtnPrefab;

    private bool _isResourceView = true;

    /// <summary>
    /// 마켓 패널 초기화 및 이벤트 구독을 수행합니다.
    /// </summary>
    protected override void OnInitialize()
    {
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
        if (_gameManager.ActionBtnPrefab == null || _marketActionBtnContentTransform == null)
        {
            return;
        }

        GameObjectUtils.ClearChildren(_marketActionBtnContentTransform);

        CreateActionButton("Resources", ShowResourceView);
        CreateActionButton("Traders", ShowTraderView);
    }

    /// <summary>
    /// 공통 액션 버튼 생성 로직입니다.
    /// </summary>
    private void CreateActionButton(string label, Action action)
    {
        GameObject btnObj = Instantiate(_gameManager.ActionBtnPrefab, _marketActionBtnContentTransform);
        ActionBtn btn = btnObj.GetComponent<ActionBtn>();

        if (btn != null)
        {
            btn.OnInitialize(label, action);
        }
    }

    /// <summary>
    /// 리소스 목록 중 하나가 클릭되었을 때 상세 정보 패널에 전달합니다.
    /// </summary>
    /// <param name="entry">선택된 리소스 데이터</param>
    public void HandleResourceButtonClicked(ResourceEntry entry)
    {
        _resourcePanel.HandleResourceButtonClicked(entry);
    }

    /// <summary>
    /// 화면을 리소스 목록 뷰로 전환합니다.
    /// </summary>
    private void ShowResourceView()
    {
        _isResourceView = true;
        TogglePanels(true);

        _resourcePanel.OnInitialize(_dataManager, this);
        RefreshResourceList();
    }

    /// <summary>
    /// 화면을 거래자 목록 뷰로 전환합니다.
    /// </summary>
    private void ShowTraderView()
    {
        _isResourceView = false;
        TogglePanels(false);

        _traderPanel.OnInitialize(_dataManager, this);
        RefreshTraderList();
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

            if (resourceBtn != null)
            {
                resourceBtn.OnInitialize(this, entry);
            }
        }
    }

    /// <summary>
    /// 날짜가 변경되었을 때 현재 활성화된 뷰의 정보를 갱신합니다.
    /// </summary>
    private void HandleDayChanged()
    {
        if (_isResourceView)
        {
            RefreshResourceButtons();
        }
        else
        {
            RefreshTraderList();
        }
    }

    /// <summary>
    /// 현재 리스트에 생성되어 있는 모든 리소스 버튼의 UI(가격, 거래량 등)를 갱신합니다.
    /// </summary>
    public void RefreshResourceButtons()
    {
        foreach (Transform child in _marketScrollViewContent)
        {
            child.GetComponent<MarketResourceBtn>().RefreshAllUI();
        }
    }

    /// <summary>
    /// 거래자 데이터를 기반으로 스크롤 목록을 새로고침합니다.
    /// </summary>
    private void RefreshTraderList()
    {
        GameObjectUtils.ClearChildren(_marketScrollViewContent);
    }
}