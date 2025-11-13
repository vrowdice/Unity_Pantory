using UnityEngine;
using UnityEngine.UI;

namespace Pantory.Managers
{
    public class GameWorldCanvasHandler
    {
        private readonly Transform _ownerTransform;

        private string _canvasName;
        private int _sortingOrder;
        private float _dynamicPixelsPerUnit;
        private Vector2 _size;

        private GameObject _sharedWorldCanvas;

        public GameWorldCanvasHandler(Transform ownerTransform, string canvasName, int sortingOrder, float dynamicPixelsPerUnit, Vector2 size)
        {
            _ownerTransform = ownerTransform;
            UpdateSettings(canvasName, sortingOrder, dynamicPixelsPerUnit, size);
        }

        public void UpdateSettings(string canvasName, int sortingOrder, float dynamicPixelsPerUnit, Vector2 size)
        {
            _canvasName = canvasName;
            _sortingOrder = sortingOrder;
            _dynamicPixelsPerUnit = dynamicPixelsPerUnit;
            _size = size;

            if (_sharedWorldCanvas != null)
            {
                _sharedWorldCanvas.name = GetCanvasName();

                if (_sharedWorldCanvas.TryGetComponent(out Canvas canvas))
                {
                    canvas.sortingOrder = _sortingOrder;
                }

                if (_sharedWorldCanvas.TryGetComponent(out CanvasScaler scaler))
                {
                    scaler.dynamicPixelsPerUnit = _dynamicPixelsPerUnit;
                }

                if (_sharedWorldCanvas.TryGetComponent(out RectTransform rectTransform))
                {
                    rectTransform.sizeDelta = _size;
                }
            }
        }

        public RectTransform GetWorldCanvas(Transform parent, Camera worldCamera)
        {
            EnsureWorldCanvas(parent, worldCamera);
            return _sharedWorldCanvas != null ? _sharedWorldCanvas.GetComponent<RectTransform>() : null;
        }

        public Transform GetWorldCanvasTransform()
        {
            return _sharedWorldCanvas != null ? _sharedWorldCanvas.transform : null;
        }

        public Vector3? GetWorldCanvasPosition()
        {
            return _sharedWorldCanvas != null ? _sharedWorldCanvas.transform.position : (Vector3?)null;
        }

        public void EnsureWorldCanvas(Transform parent, Camera worldCamera)
        {
            if (_sharedWorldCanvas == null)
            {
                CreateWorldCanvas(parent, worldCamera);
                return;
            }

            if (parent != null && _sharedWorldCanvas.transform.parent != parent)
            {
                _sharedWorldCanvas.transform.SetParent(parent, false);
            }

            if (worldCamera != null && _sharedWorldCanvas.TryGetComponent(out Canvas canvas))
            {
                canvas.worldCamera = worldCamera;
            }
        }

        public void DestroyCanvas()
        {
            if (_sharedWorldCanvas != null)
            {
                Object.Destroy(_sharedWorldCanvas);
                _sharedWorldCanvas = null;
            }
        }

        private void CreateWorldCanvas(Transform parent, Camera worldCamera)
        {
            Transform targetParent = parent != null ? parent : _ownerTransform;

            _sharedWorldCanvas = new GameObject(GetCanvasName());
            _sharedWorldCanvas.transform.SetParent(targetParent, false);

            Canvas canvas = _sharedWorldCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = _sortingOrder;
            canvas.worldCamera = worldCamera != null ? worldCamera : Camera.main;

            CanvasScaler scaler = _sharedWorldCanvas.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = _dynamicPixelsPerUnit;

            CanvasGroup group = _sharedWorldCanvas.AddComponent<CanvasGroup>();
            group.interactable = false;
            group.blocksRaycasts = false;

            RectTransform rectTransform = _sharedWorldCanvas.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.sizeDelta = _size;
            }
        }

        private string GetCanvasName()
        {
            return string.IsNullOrWhiteSpace(_canvasName) ? "SharedWorldCanvas" : _canvasName;
        }
    }
}
