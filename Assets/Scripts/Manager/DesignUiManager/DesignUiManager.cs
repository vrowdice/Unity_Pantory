using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public partial class DesignUiManager : MonoBehaviour, IUIManager
{
    [Header("UI Prefabs & Contents")]
    [SerializeField] private GameObject _buildingTypeBtnPrefab;
    [SerializeField] private Transform _buildingTypeBtnContent;
    [SerializeField] private GameObject _buildingBtnPrefab;
    [SerializeField] private Transform _buildingBtnContent;

    [Header("Mode & Panel References")]
    [SerializeField] private Image _deselectBuildingBtnImage;
    [SerializeField] private Image _removalModeBtnImage;
    [SerializeField] private BuildingInfoPanel _buildingInfoPanel;
    [SerializeField] private ThreadSaveInfoPanel _saveInfoPanel;

    [Header("Managers")]
    [SerializeField] private BuildingTileManager _buildingTileManager;

    private GameManager _gameManager;
    private GameDataManager _dataManager;
    private List<BuildingData> _buildingDataList;
    private BuildingData _selectedBuilding;
    private BuildingType _selectedBuildingType = BuildingType.Distribution;
    private bool _isRemovalMode;
    private GameObject _productionInfoImage;
    private string _currentThreadTitle = DefaultThreadTitle;
    private List<BuildingTypeBtn> _buildingTypeBtns = new List<BuildingTypeBtn>();
    private List<BuildingBtn> _buildingBtns = new List<BuildingBtn>();

    public Transform CanvasTrans => transform;
    public BuildingTileManager BuildingTileManager => _buildingTileManager;
    public GameManager GameManager => _gameManager;
    public GameDataManager GameDataManager => _dataManager;
    public BuildingData SelectedBuilding => _selectedBuilding;
    public bool IsRemovalMode => _isRemovalMode;
    public GameObject ProductionInfoImage => _productionInfoImage;

    private const string DefaultThreadTitle = "Main Line";

    public void Initialize(GameManager argGameManager, GameDataManager argGameDataManager)
    {
        _gameManager = argGameManager;
        _dataManager = argGameDataManager;
        _productionInfoImage = argGameManager.ProductionInfoImage;

        EnumUtils.GetAllEnumValues<BuildingType>().ForEach(buildingType =>
        {
            var btn = Instantiate(_buildingTypeBtnPrefab, _buildingTypeBtnContent);
            var buildingTypeBtn = btn.GetComponent<BuildingTypeBtn>();
            if (buildingTypeBtn != null)
            {
                buildingTypeBtn.Initialize(this, buildingType);
                _buildingTypeBtns.Add(buildingTypeBtn);
            }
        });

        SelectBuildingType(BuildingType.Distribution);

        if (_saveInfoPanel != null)
        {
            _saveInfoPanel.OnInitialize(_dataManager);
        }

        InitializeThreadTitle();
    }

    public void UpdateAllMainText()
    {
        Debug.Log("[DesignUiManager] UpdateAllMainText called (placeholder)");
    }

    public void UpdateModeBtnImages(bool isPlacementMode, bool isRemovalMode)
    {
        _deselectBuildingBtnImage.color = isPlacementMode ? VisualManager.Instance.ValidColor : Color.white;
        _removalModeBtnImage.color = isRemovalMode ? VisualManager.Instance.InvalidColor : Color.white;
    }
}
