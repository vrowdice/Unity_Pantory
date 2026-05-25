using System.Collections.Generic;

/// <summary>
/// 튜토리얼 진행 중 허용할 UI 입력을 제한합니다.
/// </summary>
public static class TutorialInputGate
{
    public static bool IsActive { get; private set; }

    private static readonly HashSet<string> AllowedBuildingIds = new HashSet<string>();
    private static readonly HashSet<MainPanelType> AllowedPanels = new HashSet<MainPanelType>();
    private static bool _allowAutoEmployeeToggle;
    private static bool _allowTimePlay;

    public static void Configure(
        bool active,
        IEnumerable<string> allowedBuildingIds = null,
        IEnumerable<MainPanelType> allowedPanels = null,
        bool allowAutoEmployeeToggle = false,
        bool allowTimePlay = false)
    {
        IsActive = active;
        AllowedBuildingIds.Clear();
        AllowedPanels.Clear();
        _allowAutoEmployeeToggle = allowAutoEmployeeToggle;
        _allowTimePlay = allowTimePlay;

        if (!active)
            return;

        if (allowedBuildingIds != null)
        {
            foreach (string id in allowedBuildingIds)
            {
                if (!string.IsNullOrEmpty(id))
                    AllowedBuildingIds.Add(id);
            }
        }

        if (allowedPanels != null)
        {
            foreach (MainPanelType panel in allowedPanels)
                AllowedPanels.Add(panel);
        }
    }

    public static void Clear()
    {
        Configure(false);
    }

    public static bool CanSelectBuilding(string buildingId)
    {
        if (!IsActive)
            return true;

        return !string.IsNullOrEmpty(buildingId) && AllowedBuildingIds.Contains(buildingId);
    }

    public static bool CanOpenPanel(MainPanelType panelType)
    {
        if (!IsActive)
            return true;

        return AllowedPanels.Contains(panelType);
    }

    public static bool CanUseRemovalMode() => true;

    public static bool CanToggleAutoEmployeePlacement()
    {
        if (!IsActive)
            return true;

        return _allowAutoEmployeeToggle;
    }

    public static bool CanUseTimePlay()
    {
        if (!IsActive)
            return true;

        return _allowTimePlay;
    }
}
