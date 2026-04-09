using System.Collections.Generic;
using UnityEngine;

public partial class BuildingObject
{
    /// <summary>
    /// UI 레시피 그리드용. 선택 레시피 기준 입력/출력 ID 목록을 채웁니다.
    /// </summary>
    public void GetRecipeDisplayData(List<string> inputIds, List<string> outputIds, out string currentResourceId)
    {
        inputIds.Clear();
        outputIds.Clear();
        currentResourceId = _selectedResource != null ? _selectedResource.id : null;
        if (_selectedResource == null) return;

        if (_selectedResource.requirements != null)
        {
            foreach (ResourceRequirement req in _selectedResource.requirements)
            {
                if (req.resource == null) continue;
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

        GameManager gameManager = GameManager.Instance;
        if (gameManager == null || UIManager.Instance == null) return;

        Transform worldCanvas = gameManager.GetWorldCanvas();
        if (worldCanvas == null) return;

        Dictionary<string, int> counts = BuildOutgoingResourceCounts();
        if (counts == null || counts.Count == 0) return;
        Vector3 worldPosition = transform.position + new Vector3(0f, 0f, -1f);

        _outgoingIconContainer = UIManager.Instance.CreateProductionIconContainer(
            worldCanvas,
            $"BuildingOutgoingIcons_{gameObject.GetInstanceID()}",
            worldPosition,
            _outgoingIconScale,
            counts);
    }

    private Dictionary<string, int> BuildOutgoingResourceCounts()
    {
        Dictionary<string, int> counts = new Dictionary<string, int>();
        if (_buildingData == null || _buildingData is LoadStationData)
            return counts;

        if (_buildingData is ProductionBuildingData || _buildingData is UnloadStationData || _buildingData is RawMaterialFactoryData)
        {
            if (_selectedResource == null) return counts;
            return _selectedResource.GetBatchOutputCounts();
        }

        return counts;
    }

    private void ClearOutgoingIconContainer()
    {
        if (_outgoingIconContainer == null) return;
        PoolingManager.Instance?.ClearChildrenToPool(_outgoingIconContainer.transform);
        Object.Destroy(_outgoingIconContainer);
        _outgoingIconContainer = null;
    }
}

