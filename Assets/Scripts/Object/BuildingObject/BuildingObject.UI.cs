using System.Collections.Generic;
using UnityEngine;

public partial class BuildingObject
{
    private bool ShowsOutgoingResourceIcons =>
        _buildingData is ProductionBuildingData
        || _buildingData is UnloadStationData
        || _buildingData is RawMaterialFactoryData;

    /// <summary>
    /// UI 레시피 그리드용. 선택 레시피 기준 입력/출력 ID 목록을 채웁니다.
    /// </summary>
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

    public void RefreshOutgoingResourceIcons()
    {
        ClearOutgoingIconContainer();

        if (!ShowsOutgoingResourceIcons || _selectedResource == null)
            return;

        Dictionary<string, int> counts = _selectedResource.GetBatchOutputCounts();
        if (counts.Count == 0 || !ResourceFlowFx.IsWorldPointVisible(transform.position))
            return;

        Transform worldCanvas = GameManager.Instance.GetWorldCanvas();
        if (worldCanvas == null) return;

        _outgoingIconContainer = UIManager.Instance.CreateResourceImageContainer(
            worldCanvas,
            $"BuildingOutgoingIcons_{gameObject.GetInstanceID()}",
            transform.position + ResourceFlowFx.IconWorldOffset,
            _outgoingIconScale,
            counts);
    }

    private void TryRefreshOutgoingIconsWhenVisible()
    {
        if (_outgoingIconContainer != null)
            return;

        if (_selectedResource == null || !ShowsOutgoingResourceIcons)
            return;

        if (!ResourceFlowFx.IsWorldPointVisible(transform.position))
            return;

        if (_selectedResource.GetBatchOutputCounts().Count == 0)
            return;

        RefreshOutgoingResourceIcons();
    }

    private void ClearOutgoingIconContainer()
    {
        if (_outgoingIconContainer == null) return;
        GameManager.Instance.PoolingManager.ClearChildrenToPool(_outgoingIconContainer.transform);
        Object.Destroy(_outgoingIconContainer);
        _outgoingIconContainer = null;
    }
}
