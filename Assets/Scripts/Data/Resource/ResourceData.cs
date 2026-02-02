using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewResourceData", menuName = "Game Data/Resource Data")]
public class ResourceData : ScriptableObject
{
    public string id;
    public string displayName;

    public long baseValue = 10;

    public ResourceType type;
    public Sprite icon;
    [TextArea(3, 10)] public string description;

    public List<ResourceRequirement> requirements = new List<ResourceRequirement>();

    public int initialAmount;
}