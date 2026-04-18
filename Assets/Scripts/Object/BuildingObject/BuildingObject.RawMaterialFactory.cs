using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public partial class BuildingObject
{
    private void TickSimulationRawMaterialFactory(DataManager dataManager, RawMaterialFactoryData rawFactory)
    {
        if (_selectedResource == null || string.IsNullOrEmpty(_selectedResource.id)) return;
        if (!IsResourceAllowedForRawFactory(rawFactory, _selectedResource)) return;
        TickStaffedBatchWork(
            dataManager,
            () => CanCompleteRawMaterialFactoryBatch(dataManager),
            () => TryCompleteRawMaterialFactoryBatch(dataManager));
    }

    private bool CanCompleteRawMaterialFactoryBatch(DataManager dataManager)
    {
        return dataManager.Resource.GetResourceEntry(_selectedResource.id) != null;
    }

    private bool TryCompleteRawMaterialFactoryBatch(DataManager dataManager)
    {
        if (!dataManager.Resource.ModifyResourceCount(_selectedResource.id, 1)) return false;
        PlayRawMaterialToWarehouseFx();
        return true;
    }

    /// <summary>
    /// 창고 반영 직후: SharedWorldCanvas 위에서 건물 근처 아이콘이 월드 Y로 올라가며 사라집니다.
    /// </summary>
    private void PlayRawMaterialToWarehouseFx()
    {
        if (_selectedResource == null || _selectedResource.icon == null) return;

        GameManager gameManager = GameManager.Instance;
        if (gameManager == null) return;

        RectTransform root = gameManager.GetWorldCanvas();
        if (root == null) return;

        GameObject fx = new GameObject("RawMaterialToWarehouseFx");
        RectTransform rt = fx.AddComponent<RectTransform>();
        rt.SetParent(root, false);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(48f, 48f);
        rt.localScale = Vector3.one * 0.02f;

        Vector3 startWorld = transform.position + new Vector3(0f, 0.4f, -1f);
        Vector3 endWorld = startWorld + new Vector3(0f, 0.85f, 0f);
        rt.position = startWorld;
        rt.rotation = Quaternion.identity;

        Image image = fx.AddComponent<Image>();
        image.sprite = _selectedResource.icon;
        image.raycastTarget = false;
        image.preserveAspect = true;

        CanvasGroup group = fx.AddComponent<CanvasGroup>();
        group.alpha = 1f;
        group.interactable = false;
        group.blocksRaycasts = false;

        const float duration = 0.5f;

        Sequence seq = DOTween.Sequence();
        seq.SetLink(fx);
        seq.Append(rt.DOMove(endWorld, duration).SetEase(Ease.OutQuad));
        seq.Join(group.DOFade(0f, duration));
        seq.OnComplete(() =>
        {
            if (fx != null)
                Object.Destroy(fx);
        });
    }

    private static bool IsResourceAllowedForRawFactory(RawMaterialFactoryData raw, ResourceData resource)
    {
        List<ResourceData> list = raw.ProducibleResources;
        if (list != null && list.Count > 0)
        {
            foreach (ResourceData item in list)
            {
                if (item.id == resource.id) return true;
            }
            return false;
        }

        return resource.type == ResourceType.raw;
    }
}
