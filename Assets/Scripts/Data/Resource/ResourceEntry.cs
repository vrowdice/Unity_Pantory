using System;

[Serializable]
public class ResourceEntry
{
    public ResourceData resourceData;   // 자원의 정적 데이터 (ScriptableObject)
    public ResourceState resourceState; // 자원의 동적 상태
    
    public ResourceEntry(ResourceData data)
    {
        resourceData = data;
        resourceState = new ResourceState();
        
        resourceState.InitializeFromData(data);
    }
}
