using System;

[Serializable]
public class ResourceEntry
{
    public ResourceData data;   // 자원의 정적 데이터 (ScriptableObject)
    public ResourceState state; // 자원의 동적 상태
    
    public ResourceEntry(ResourceData data)
    {
        this.data = data;
        state = new ResourceState();
        
        state.InitializeFromData(data);
    }
}
