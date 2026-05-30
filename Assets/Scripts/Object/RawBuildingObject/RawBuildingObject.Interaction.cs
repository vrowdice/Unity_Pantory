using System.Collections.Generic;
using UnityEngine;

public partial class RawBuildingObject
{
    private BuildingSceneRunnerBase _runner;
    private bool _clickArmed;
    private Vector2 _pointerDownScreenPos;
    private const float ClickDragThresholdPixels = 8f;

    public ResourceData SelectedResource => _selectedResource;
    public bool HasConfiguredOutputResource =>
        _selectedResource != null && !string.IsNullOrEmpty(_selectedResource.id);

    public void Init(RawMaterialFactoryData buildingData, Vector2Int origin, BuildingSceneRunnerBase runner)
    {
        _runner = runner;
        _buildingData = buildingData;
        _origin = origin;
        _size = buildingData.size;

        if (_viewRenderer != null)
        {
            _viewRenderer.sprite = buildingData.buildingSprite;
            _viewRenderer.transform.localScale = new Vector3(_size.x, _size.y, 1f);
        }

        if (_collider != null)
        {
            _collider.size = new Vector2(_size.x, _size.y);
            _collider.offset = Vector2.zero;
        }

        _selectedResource = buildingData.DefaultRawResource;
    }

    public float GetProductionProgressNormalized() => Mathf.Clamp01(_workProgress);

    public float GetAverageAssignedEfficiencyNormalized(DataManager dataManager)
    {
        int total = _assignedWorkers + _assignedTechnicians;
        if (total <= 0)
            return RequiredEmployeeSlots <= 0 ? 1f : 0f;

        EmployeeEntry workerEntry = dataManager.Employee.GetEmployeeEntry(EmployeeType.Worker);
        EmployeeEntry technicianEntry = dataManager.Employee.GetEmployeeEntry(EmployeeType.Technician);
        float workerEfficiency = Mathf.Clamp01(workerEntry.state.currentEfficiency);
        float technicianEfficiency = Mathf.Clamp01(technicianEntry.state.currentEfficiency);
        return Mathf.Clamp01((_assignedWorkers * workerEfficiency + _assignedTechnicians * technicianEfficiency) / total);
    }

    public void GetRecipeDisplayData(List<string> inputIds, List<string> outputIds, out string currentResourceId)
    {
        inputIds.Clear();
        outputIds.Clear();
        if (_selectedResource == null)
        {
            currentResourceId = null;
            return;
        }

        currentResourceId = _selectedResource.id;

        if (_selectedResource.requirements != null)
        {
            foreach (ResourceRequirement req in _selectedResource.requirements)
            {
                int count = Mathf.Max(1, req.count);
                for (int i = 0; i < count; i++)
                    inputIds.Add(req.resource.id);
            }
        }

        _selectedResource.AppendBatchOutputIds(outputIds);
    }

    public bool TrySetSelectedResource(ResourceData data)
    {
        if (_buildingData == null || data == null)
            return false;

        List<ResourceData> producible = _buildingData.ProducibleResources;
        if (producible != null && producible.Count > 0)
        {
            bool allowed = false;
            for (int i = 0; i < producible.Count; i++)
            {
                if (producible[i].id == data.id)
                {
                    allowed = true;
                    break;
                }
            }

            if (!allowed)
                return false;
        }
        else if (data.type != ResourceType.raw)
        {
            return false;
        }

        _selectedResource = data;
        return true;
    }

    private void Update()
    {
        if (_runner == null ||
            _runner.PlacementHandler.IsPlacementMode ||
            _runner.PlacementHandler.IsRemovalMode ||
            _runner.PlacementHandler.IsBlueprintPlacementMode ||
            _runner.BlueprintHandler.IsBlueprintMode)
            return;

        if (PointerInput.IsMultiTouch)
            return;

        if (PointerInput.GetPrimaryPointerDown())
        {
            if (PointerInput.IsPointerOverUi())
                return;

            Vector3 pointerWorld = PointerInput.ScreenToWorldOnPlane(Camera.main, PointerInput.PrimaryScreenPosition);
            if (_collider == null || !_collider.OverlapPoint(pointerWorld))
                return;

            _clickArmed = true;
            _pointerDownScreenPos = PointerInput.PrimaryScreenPosition;
            return;
        }

        if (PointerInput.GetPrimaryPointerUp() && _clickArmed)
        {
            _clickArmed = false;

            if (PointerInput.IsPointerOverUi())
                return;

            Vector2 delta = PointerInput.PrimaryScreenPosition - _pointerDownScreenPos;
            if (delta.sqrMagnitude > ClickDragThresholdPixels * ClickDragThresholdPixels)
                return;

            Vector3 pointerWorld = PointerInput.ScreenToWorldOnPlane(Camera.main, PointerInput.PrimaryScreenPosition);
            if (_collider == null || !_collider.OverlapPoint(pointerWorld))
                return;

            UIManager.Instance.ShowBuildingInfoPopup(this);
        }
    }
}
