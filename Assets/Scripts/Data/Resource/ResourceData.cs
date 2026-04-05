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

    [Header("Recipe outputs (per batch)")]
    [Tooltip("배치당 산출 품목·개수. 비어 있으면 id를 primaryOutputPerBatch만큼 산출로 표시합니다.")]
    public List<ResourceRequirement> recipeOutputs = new List<ResourceRequirement>();

    [Tooltip("recipeOutputs가 비어 있을 때 배치당 주 산출 개수")]
    public int primaryOutputPerBatch = 1;

    public int initialAmount;

    /// <summary>
    /// 배치 1회 기준 산출 자원 ID별 개수 (UI·월드 아이콘용).
    /// </summary>
    public Dictionary<string, int> GetBatchOutputCounts()
    {
        Dictionary<string, int> counts = new Dictionary<string, int>();

        if (recipeOutputs != null && recipeOutputs.Count > 0)
        {
            foreach (ResourceRequirement req in recipeOutputs)
            {
                if (req == null || req.resource == null || string.IsNullOrEmpty(req.resource.id)) continue;
                int amount = Mathf.Max(1, req.count);
                if (counts.TryGetValue(req.resource.id, out int existing)) counts[req.resource.id] = existing + amount;
                else counts[req.resource.id] = amount;
            }

            if (counts.Count > 0) return counts;
        }

        if (!string.IsNullOrEmpty(id))
            counts[id] = Mathf.Max(1, primaryOutputPerBatch);

        return counts;
    }

    /// <summary>
    /// 건물 UI 레시피 출력 ID 리스트 등에 배치당 산출 ID를 개수만큼 추가합니다.
    /// </summary>
    public void AppendBatchOutputIds(List<string> list)
    {
        if (list == null) return;

        foreach (KeyValuePair<string, int> kvp in GetBatchOutputCounts())
        {
            for (int i = 0; i < kvp.Value; i++)
                list.Add(kvp.Key);
        }
    }
}