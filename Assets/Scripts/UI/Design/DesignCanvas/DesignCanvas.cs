using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public partial class DesignCanvas : CanvasBase
{
    [SerializeField] private DesignRunner _designRunner;
    [SerializeField] private TextMeshProUGUI _loadThreadNameText;

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
    /// 저장 버튼 클릭 시 호출됩니다. 현재 스레드의 생산 체인과 유지비를 계산한 뒤 저장 정보 팝업을 띄웁니다.
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

        UIManager.Instance.ShowThreadSaveInfoPopup(
            inputResourceIdentifiers,
            inputResourceCounts,
            outputResourceIdentifiers,
            outputResourceCounts,
            totalMaintenanceCost,
            this);
    }

    /// <summary>
    /// 스레드 변경 사항을 저장합니다. DesignRunner에 저장 요청을 보냅니다.
    /// </summary>
    public void SaveThreadChanges(string threadName, string categoryIdentifier)
    {
        bool success = _designRunner.SaveThread(threadName, categoryIdentifier);
        if (success)
        {
            UIManager.Instance.ShowWarningPopup(WarningMessage.SavedSuccessfully);
            _loadThreadNameText.text = threadName;
        }
        else
        {
            UIManager.Instance.ShowWarningPopup(WarningMessage.SaveFailed);
        }

        DeselectBuilding();
    }

    /// <summary>
    /// 불러오기 버튼 클릭 시 호출됩니다. 관리 팝업에서 선택한 스레드를 불러옵니다.
    /// </summary>
    public void OnClickLoadButton()
    {
        UIManager.Instance.ShowManageThreadPopup((string selectedThreadName) =>
        {
            LoadThread(selectedThreadName);
        });
    }

    /// <summary>
    /// 지정한 스레드 이름의 데이터를 로드하여 편집 세션을 시작합니다.
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

#if UNITY_EDITOR
    private void OnDestroy()
    {
        if (UnityEditor.Selection.activeObject == this || UnityEditor.Selection.activeGameObject == gameObject)
        {
            UnityEditor.Selection.activeObject = null;
        }
    }
#endif
}
