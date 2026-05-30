using TMPro;
using UnityEngine;
using Evo.UI;

/// <summary>
/// 월드 상단 대표 원자재 건물의 머리 위 플로팅 UI. 가동 개수 조절과 설치 상한 표시.
/// 직원·생산 자원 할당은 건물 클릭 후 BuildingInfoPopup에서 처리합니다.
/// </summary>
public class RawBuildingInfoPanel : MonoBehaviour
{
    [Header("UI Component")]
    [SerializeField] private TextMeshProUGUI _countText;
    [SerializeField] private Button _subButton;
    [SerializeField] private Button _sub10Button;

    [Header("Requirement Info")]
    [SerializeField] private TextMeshProUGUI _titleText;
    [SerializeField] private TextMeshProUGUI _priceText;
    [SerializeField] private TextMeshProUGUI _infoText;
    [SerializeField] private float _uiScale = 0.01f;
    [SerializeField] private Vector3 _offset = new Vector3(0f, 1.3f, 0f);

    private RawBuildingObject _targetRawBuilding;

    public void Init(RawBuildingObject target)
    {
        _targetRawBuilding = target;
        transform.localScale = Vector3.one * _uiScale;
        EnsurePanelInteraction();
        RefreshUI();
    }

    private void EnsurePanelInteraction()
    {
        CanvasGroup panelGroup = GetComponent<CanvasGroup>();
        if (panelGroup == null)
            panelGroup = gameObject.AddComponent<CanvasGroup>();

        panelGroup.interactable = true;
        panelGroup.blocksRaycasts = true;
    }

    private void Start()
    {
        if (_targetRawBuilding != null)
            RefreshUI();
    }

    private void Update()
    {
        if (_targetRawBuilding == null)
            return;

        transform.position = _targetRawBuilding.transform.position + _offset;
    }

    public void ModifyCount(int delta)
    {
        if (_targetRawBuilding == null)
            return;

        int current = _targetRawBuilding.RawMaterialCount;
        int nextCount = Mathf.Max(0, current + delta);
        if (nextCount == current)
            return;

        if (!_targetRawBuilding.TrySetRawMaterialCount(nextCount))
        {
            if (delta > 0)
            {
                RawMaterialFactoryData buildingData = _targetRawBuilding.BuildingData;
                DataManager dataManager = DataManager.Instance;
                long cost = buildingData != null ? buildingData.buildCost * (nextCount - current) : 0;
                if (cost > 0 && dataManager != null && dataManager.Finances.Credit < cost)
                    UIManager.Instance.ShowWarningPopup(WarningMessage.NotEnoughCredits);
                else
                    UIManager.Instance.ShowWarningPopup(WarningMessage.BuildingPlacedCountLimitReached);
            }
            return;
        }

        RefreshUI();

        BuildingSceneRunnerBase runner = GameManager.Instance?.CurrentRunner as BuildingSceneRunnerBase;
        if (delta > 0 &&
            runner?.PlacementHandler != null &&
            runner.PlacementHandler.IsAutoEmployeePlacement)
        {
            _targetRawBuilding.TryAutoAssignEmployeesToFill();
        }

        runner?.FlushPlacedLayoutToDataManager();
    }

    public void RefreshUI()
    {
        if (_targetRawBuilding == null)
            return;

        RawMaterialFactoryData buildingData = _targetRawBuilding.BuildingData;
        int count = _targetRawBuilding.RawMaterialCount;

        if (_countText != null)
            _countText.text = count.ToString();

        if (_subButton != null)
            _subButton.interactable = count > 0;
        if (_sub10Button != null)
            _sub10Button.interactable = count > 0;

        if (_titleText != null && buildingData != null)
            _titleText.text = buildingData.id.Localize(LocalizationUtils.TABLE_BUILDING);

        if (_priceText != null && buildingData != null)
        {
            string formattedCost = ReplaceUtils.FormatNumberWithCommas(buildingData.buildCost);
            string creditLabel = "Credit".Localize(LocalizationUtils.TABLE_COMMON);
            _priceText.text = $"{formattedCost} {creditLabel}";
        }

        if (_infoText != null && buildingData != null)
        {
            if (buildingData.usePlacedCountLimit)
            {
                int max = DataManager.Instance.Building.GetMaxPlacedCount(buildingData);
                _infoText.text = $"{count}/{max}";
            }
            else
            {
                _infoText.text = count.ToString();
            }
        }
    }
}
