using System.Collections.Generic;
using UnityEngine;

namespace Pantory.Managers
{
    public class GameProductionIconHandler
    {
        private GameObject _gridSortContentPrefab;
        private GameObject _productionInfoImagePrefab;
        private float _productionIconScale = 1f;

        public GameProductionIconHandler(GameObject gridSortContentPrefab, GameObject productionInfoImagePrefab, float productionIconScale)
        {
            UpdateSettings(gridSortContentPrefab, productionInfoImagePrefab, productionIconScale);
        }

        public void UpdateSettings(GameObject gridSortContentPrefab, GameObject productionInfoImagePrefab, float productionIconScale)
        {
            _gridSortContentPrefab = gridSortContentPrefab;
            _productionInfoImagePrefab = productionInfoImagePrefab;
            _productionIconScale = productionIconScale;
        }

        public GameObject CreateProductionIconContainerWithoutCanvas(Transform parent, string name, Vector3 worldPosition, float containerScale)
        {
            if (_gridSortContentPrefab == null)
            {
                Debug.LogWarning("[GameProductionIconHandler] GridSortContentPrefab is not assigned.");
                return null;
            }

            if (parent == null)
            {
                Debug.LogWarning("[GameProductionIconHandler] Parent transform is null.");
                return null;
            }

            GameObject container = Object.Instantiate(_gridSortContentPrefab, parent);
            container.name = name;

            if (container.TryGetComponent(out RectTransform rectTransform))
            {
                rectTransform.sizeDelta = new Vector2(200, 50);
            }

            container.transform.position = worldPosition;
            container.transform.rotation = Quaternion.identity;
            container.transform.localScale = Vector3.one * containerScale;

            return container;
        }

        public GameObject CreateProductionIcon(Transform parent, ResourceEntry resourceEntry, int amount)
        {
            if (_productionInfoImagePrefab == null)
            {
                Debug.LogWarning("[GameProductionIconHandler] ProductionInfoImagePrefab is not assigned.");
                return null;
            }

            if (parent == null)
            {
                Debug.LogWarning("[GameProductionIconHandler] Parent transform is null.");
                return null;
            }

            if (resourceEntry == null)
            {
                Debug.LogWarning("[GameProductionIconHandler] ResourceEntry is null.");
                return null;
            }

            GameObject iconObj = Object.Instantiate(_productionInfoImagePrefab, parent);

            if (iconObj.TryGetComponent(out RectTransform iconRect))
            {
                iconRect.localScale = Vector3.one * _productionIconScale;
            }

            if (iconObj.TryGetComponent(out ProductionInfoImage iconComponent))
            {
                iconComponent.OnInitialize(resourceEntry, amount);
            }

            return iconObj;
        }

        public void CreateProductionIcons(Transform parent, Dictionary<string, int> productionCounts, GameDataManager dataManager)
        {
            if (productionCounts == null || productionCounts.Count == 0)
                return;

            if (dataManager == null)
            {
                Debug.LogWarning("[GameProductionIconHandler] GameDataManager is null.");
                return;
            }

            foreach (var kvp in productionCounts)
            {
                if (string.IsNullOrEmpty(kvp.Key))
                    continue;

                ResourceEntry resourceEntry = dataManager.GetResourceEntry(kvp.Key);
                if (resourceEntry != null)
                {
                    CreateProductionIcon(parent, resourceEntry, kvp.Value);
                }
            }
        }
    }
}

