using UnityEngine;

public class BuildingEntry
{
    public BuildingData buildingData;
    public BuildingState buildingState;

    public BuildingEntry(BuildingData data)
    {
        buildingData = data;
        buildingState = new BuildingState();
    }
}
