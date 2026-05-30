using System.Collections.Generic;

public partial class BuildingObject
{
    private bool CanEmitOutputsToAdjacentRoads(Dictionary<string, int> outputs)
    {
        if (outputs == null || outputs.Count == 0) return false;
        return _mainRunner != null && _mainRunner.ResourceFlowHandler != null && _mainRunner.ResourceFlowHandler.CanAcceptBuildingOutputs(this, outputs);
    }

    private bool TryEmitOutputsToAdjacentRoads(Dictionary<string, int> outputs)
    {
        if (outputs == null || outputs.Count == 0) return false;
        return _mainRunner != null && _mainRunner.ResourceFlowHandler != null && _mainRunner.ResourceFlowHandler.TryEmitBuildingOutputs(this, outputs);
    }
}
