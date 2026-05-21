using TMPro;
using UnityEngine;
using Evo.UI;

public partial class MainCanvas
{
    [Header("Mobile")]
    [SerializeField] private RectTransform _buildingControlContainer;
    [SerializeField] private GameObject _buildModeCancelBtn;

    private bool _mobileUiInitialized;

    private void InitMobileUi()
    {
        if (_mobileUiInitialized)
            return;

        _mobileUiInitialized = true;

        if (Application.isMobilePlatform)
        {
            EnsureBuildModeCancelButton();
            ApplyMobileSafeAreaInsets();
        }

        RefreshBuildModeCancelVisibility();
    }

    public bool TryCancelActiveBuildMode()
    {
        if (_mainRunner == null)
            return false;

        if (_mainRunner.BlueprintHandler != null && _mainRunner.BlueprintHandler.IsBlueprintMode)
        {
            _mainRunner.SetBlueprintMode(false);
            RefreshBuildModeCancelVisibility();
            return true;
        }

        MainBuildingPlacementHandler placementHandler = _mainRunner.PlacementHandler;
        if (placementHandler == null)
            return false;

        if (placementHandler.IsBlueprintPlacementMode)
        {
            placementHandler.CancelBlueprintPlacement();
            _activeBlueprintLayoutKey = null;
            RefreshBlueprintUi();
            RefreshBuildModeCancelVisibility();
            return true;
        }

        if (placementHandler.IsRemovalMode)
        {
            _removalModeSwitch.SetValue(false);
            ApplyRemovalMode(false);
            RefreshBuildModeCancelVisibility();
            return true;
        }

        if (placementHandler.IsPlacementMode)
        {
            DeselectBuilding();
            RefreshBuildModeCancelVisibility();
            return true;
        }

        return false;
    }

    public void CancelActiveBuildMode()
    {
        TryCancelActiveBuildMode();
    }

    private void UpdateMobileUi()
    {
        if (!Application.isMobilePlatform)
            return;

        RefreshBuildModeCancelVisibility();
    }

    private void RefreshBuildModeCancelVisibility()
    {
        if (!Application.isMobilePlatform || _buildModeCancelBtn == null)
            return;

        bool shouldShow = IsAnyBuildModeActive();
        if (_buildModeCancelBtn.activeSelf != shouldShow)
            _buildModeCancelBtn.SetActive(shouldShow);
    }

    private bool IsAnyBuildModeActive()
    {
        if (_mainRunner == null)
            return false;

        if (_mainRunner.BlueprintHandler != null && _mainRunner.BlueprintHandler.IsBlueprintMode)
            return true;

        MainBuildingPlacementHandler placementHandler = _mainRunner.PlacementHandler;
        if (placementHandler == null)
            return false;

        return placementHandler.IsPlacementMode
               || placementHandler.IsBlueprintPlacementMode
               || placementHandler.IsRemovalMode;
    }

    private void EnsureBuildModeCancelButton()
    {
        if (_buildModeCancelBtn != null)
            return;

        Transform controlContainer = GetBuildingControlContainer();
        if (controlContainer == null)
            return;

        Transform rotateLeft = FindChildRecursive(controlContainer, "RotateBuildingLeftBtn");
        if (rotateLeft == null)
            return;

        Transform rotateContainer = rotateLeft.parent;
        if (rotateContainer == null)
            return;

        GameObject cancelObj = Object.Instantiate(rotateLeft.gameObject, rotateContainer);
        cancelObj.name = "BuildModeCancelBtn";

        TextMeshProUGUI label = cancelObj.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label != null)
            label.text = "X";

        Button cancelButton = cancelObj.GetComponent<Button>();
        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(CancelActiveBuildMode);
        }

        cancelObj.SetActive(false);
        _buildModeCancelBtn = cancelObj;
    }

    private void ApplyMobileSafeAreaInsets()
    {
        if (!Application.isMobilePlatform)
            return;

        ApplySafeAreaInset(GetBuildingControlContainer(), SafeAreaEdgeInset.Edge.Bottom, 8f);
        ApplySafeAreaInset(transform.Find("TopInfoContainer"), SafeAreaEdgeInset.Edge.Top, 4f);
        ApplySafeAreaInset(transform.Find("QuickMoveContainer"), SafeAreaEdgeInset.Edge.Left, 4f);
    }

    private Transform GetBuildingControlContainer()
    {
        if (_buildingControlContainer != null)
            return _buildingControlContainer;

        return transform.Find("BuildingControllContainer");
    }

    private static void ApplySafeAreaInset(Transform target, SafeAreaEdgeInset.Edge edge, float extraPadding)
    {
        if (target == null)
            return;

        SafeAreaEdgeInset inset = target.GetComponent<SafeAreaEdgeInset>();
        if (inset == null)
            inset = target.gameObject.AddComponent<SafeAreaEdgeInset>();

        inset.Configure(edge, extraPadding);
    }

    private static Transform FindChildRecursive(Transform parent, string childName)
    {
        if (parent == null)
            return null;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == childName)
                return child;

            Transform nested = FindChildRecursive(child, childName);
            if (nested != null)
                return nested;
        }

        return null;
    }
}
