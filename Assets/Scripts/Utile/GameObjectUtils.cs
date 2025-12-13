using System.Collections.Generic;
using UnityEngine;

public static class GameObjectUtils
{
    public static void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            GameObject.Destroy(parent.GetChild(i).gameObject);
        }
    }

    /// <summary>
    /// 스프라이트를 지정된 월드 크기에 맞춰 자동으로 스케일을 조절합니다.
    /// </summary>
    /// <param name="transform">스케일을 조절할 Transform</param>
    /// <param name="sprite">대상 스프라이트</param>
    /// <param name="targetWorldSize">원하는 월드 상의 크기</param>
    /// <param name="keepAspectRatio">비율 유지 여부</param>
    public static void SetSpriteToWorldSize(Transform transform, Sprite sprite, Vector2 targetWorldSize, bool keepAspectRatio = true)
    {
        if (transform == null || sprite == null)
        {
            return;
        }

        // 현재 스프라이트의 원래 크기 (로컬 스케일 1일 때의 크기)
        Vector2 spriteSize = sprite.bounds.size;

        Vector2 newScale = Vector2.one;

        if (keepAspectRatio)
        {
            // 비율을 유지하면서 타겟 박스 안에 맞추기 (Fit)
            float widthRatio = targetWorldSize.x / spriteSize.x;
            float heightRatio = targetWorldSize.y / spriteSize.y;
            
            // 더 작은 비율을 선택하여 박스 안에 꽉 차게 함 (Fit Inside)
            float minRatio = Mathf.Min(widthRatio, heightRatio);
            newScale = new Vector2(minRatio, minRatio);
        }
        else
        {
            // 비율 무시하고 강제로 타겟 사이즈에 맞춤 (Stretch)
            newScale.x = targetWorldSize.x / spriteSize.x;
            newScale.y = targetWorldSize.y / spriteSize.y;
        }

        transform.localScale = newScale;
    }

    /// <summary>
    /// SpriteRenderer의 스프라이트를 지정된 월드 크기에 맞춰 자동으로 스케일을 조절합니다.
    /// </summary>
    /// <param name="spriteRenderer">대상 SpriteRenderer</param>
    /// <param name="targetWorldSize">원하는 월드 상의 크기</param>
    /// <param name="keepAspectRatio">비율 유지 여부</param>
    public static void SetSpriteRendererToWorldSize(SpriteRenderer spriteRenderer, Vector2 targetWorldSize, bool keepAspectRatio = true)
    {
        if (spriteRenderer == null || spriteRenderer.sprite == null)
        {
            return;
        }

        SetSpriteToWorldSize(spriteRenderer.transform, spriteRenderer.sprite, targetWorldSize, keepAspectRatio);
    }

    /// <summary>
    /// 자식 오브젝트의 스케일을 조절하여 부모의 스케일 영향을 제거합니다.
    /// 부모가 스케일되어 있어도 자식이 원래 크기로 보이도록 합니다.
    /// </summary>
    /// <param name="childTransform">스케일을 조절할 자식 Transform</param>
    /// <param name="parentTransform">부모 Transform (null이면 childTransform.parent 사용)</param>
    public static void CompensateParentScale(Transform childTransform, Transform parentTransform = null)
    {
        if (childTransform == null)
        {
            return;
        }

        if (parentTransform == null)
        {
            parentTransform = childTransform.parent;
        }

        if (parentTransform == null)
        {
            return;
        }

        Vector3 parentScale = parentTransform.localScale;
        childTransform.localScale = new Vector3(
            1f / parentScale.x,
            1f / parentScale.y,
            1f / parentScale.z
        );
    }

    /// <summary>
    /// 리소스 ID 리스트를 집계하여 ID별 개수를 딕셔너리로 반환합니다.
    /// </summary>
    /// <param name="resourceIds">리소스 ID 리스트</param>
    /// <returns>ID별 개수 딕셔너리</returns>
    public static Dictionary<string, int> AggregateResourceCounts(List<string> resourceIds)
    {
        Dictionary<string, int> counts = new Dictionary<string, int>();

        if (resourceIds == null)
            return counts;

        foreach (var resourceId in resourceIds)
        {
            if (string.IsNullOrEmpty(resourceId))
                continue;

            if (counts.ContainsKey(resourceId))
            {
                counts[resourceId]++;
            }
            else
            {
                counts[resourceId] = 1;
            }
        }

        return counts;
    }
}
