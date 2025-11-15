[System.Serializable]
public class ResourceRequirement
{
    public ResourceData resource;  // 필요한 자원
    public int count;              // 필요한 개수

    public ResourceRequirement Clone()
    {
        return new ResourceRequirement
        {
            resource = resource,
            count = count
        };
    }
}
