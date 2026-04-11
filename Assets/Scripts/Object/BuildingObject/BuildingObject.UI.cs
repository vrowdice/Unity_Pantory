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

        if (_buildingData is LoadStationData)
            return;

        if (!(_buildingData is ProductionBuildingData || _buildingData is UnloadStationData || _buildingData is RawMaterialFactoryData))
            return;

        if (_selectedResource == null)
            return;

        Dictionary<string, int> counts = _selectedResource.GetBatchOutputCounts();
        if (counts.Count == 0)
            return;

        Transform worldCanvas = GameManager.Instance.GetWorldCanvas();
        Vector3 worldPosition = transform.position + new Vector3(0f, 0f, -1f);

        _outgoingIconContainer = UIManager.Instance.CreateProductionIconContainer(
            worldCanvas,
            $"BuildingOutgoingIcons_{gameObject.GetInstanceID()}",
            worldPosition,
            _outgoingIconScale,
            counts);
    }

    private void ClearOutgoingIconContainer()
    {
        if (_outgoingIconContainer == null) return;
        GameManager.Instance.PoolingManager.ClearChildrenToPool(_outgoingIconContainer.transform);
        Object.Destroy(_outgoingIconContainer);
        _outgoingIconContainer = null;
    }
}
