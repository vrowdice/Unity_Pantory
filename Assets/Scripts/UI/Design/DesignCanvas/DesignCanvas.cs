using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public partial class DesignCanvas : CanvasBase
{
    [SerializeField] private DesignRunner _designRunner;
    [SerializeField] private TextMeshProUGUI _loadThreadNameText;
    [SerializeField] private ThreadSaveInfoPopup _threadSaveInformationPanel;

    public DesignRunner DesignRunner => _designRunner;

    public void Init(DesignRunner designRunner)
    {
        base.Init();

        _designRunner = designRunner;

        foreach (BuildingType buildingType in EnumUtils.GetAllEnumValues<BuildingType>())
        {
            GameObject btn = Instantiate(_buildingTypeBtnPrefab, _buildingTypeBtnContent);
            BuildingTypeBtn buildingTypeBtn = btn.GetComponent<BuildingTypeBtn>();
            if (buildingTypeBtn != null)
            {
                buildingTypeBtn.Initialize(this, buildingType);
                _buildingTypeBtns.Add(buildingTypeBtn);
            }
        }

        SelectBuildingType(BuildingType.Distribution);
    }

    public void UpdateModeBtnImages(bool isPlacementMode, bool isRemovalMode)
    {
        _deselectBuildingBtnImage.color = isPlacementMode ? VisualManager.Instance.ValidColor : Color.white;
        _removalModeBtnImage.color = isRemovalMode ? VisualManager.Instance.InvalidColor : Color.white;
    }

    /// <summary>
    /// 저장 버튼 클릭 시 호출됩니다. 현재 배치된 건물들의 생산 체인과 유지비 등을 계산하여 요약 패널을 띄웁니다.
    /// </summary>
    public void OnClickSaveButton()
    {
        string threadName = _designRunner.CurrentThreadId;

        List<string> inputResourceIdentifiers;
        Dictionary<string, int> inputResourceCounts;
        List<string> outputResourceIdentifiers;
        Dictionary<string, int> outputResourceCounts;

        _designRunner.CalculateProductionChain(
            threadName,
            out inputResourceIdentifiers,
            out inputResourceCounts,
            out outputResourceIdentifiers,
            out outputResourceCounts
        );

        int totalMaintenanceCost = _designRunner.CalculateTotalMaintenanceCost(threadName);

        _threadSaveInformationPanel.Init(
            inputResourceIdentifiers,
            inputResourceCounts,
            outputResourceIdentifiers,
            outputResourceCounts,
            totalMaintenanceCost,
            this
        );
    }

    /// <summary>
    /// 실제 저장 로직을 실행합니다. BuildingTileManager의 최종 저장 기능을 호출합니다.
    /// </summary>
    public void SaveThreadChanges(string threadName, string categoryIdentifier)
    {
        bool success = _designRunner.SaveThread(threadName, categoryIdentifier);
        if (success)
        {
            GameManager.ShowWarningPopup(WarningMessage.SavedSuccessfully);
            _loadThreadNameText.text = threadName;
        }
        else
        {
            GameManager.ShowWarningPopup(WarningMessage.SaveFailed);
        }

        DeselectBuilding();
    }

    /// <summary>
    /// 로드 버튼 클릭 시 호출됩니다. 스레드 관리 패널을 엽니다.
    /// </summary>
    public void OnClickLoadButton()
    {
        GameManager.ShowManageThreadPopup((string selectedThreadName) =>
        {
            LoadThread(selectedThreadName);
        });
    }

    /// <summary>
    /// 선택된 스레드 식별자를 기반으로 데이터를 불러오고 화면을 갱신합니다.
    /// </summary>
    private void LoadThread(string threadName)
    {
        if (string.IsNullOrEmpty(threadName))
        {
            return;
        }

        ThreadState threadState = DataManager.Thread.GetThread(threadName);
        if (threadState == null)
        {
            Debug.LogWarning("[DesignUiManager] Thread not found: " + threadName);
            return;
        }

        if (_designRunner != null)
        {
            _designRunner.LoadThread(threadName);
        }

        if (_loadThreadNameText != null)
        {
            string displayName = string.IsNullOrEmpty(threadState.threadName) ? threadState.threadId : threadState.threadName;
            _loadThreadNameText.text = displayName;
        }

        Debug.Log($"[DesignUiManager] Thread loaded: {threadName}");
    }
}
