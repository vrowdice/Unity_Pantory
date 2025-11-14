using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// 월드 상에 배치되는 스레드 오브젝트
/// </summary>
public class ThreadObject : MonoBehaviour
{
    private const float PREVIEW_ALPHA = 0.6f;

    private ThreadState _threadState;
    private SpriteRenderer _spriteRenderer;
    private Color _baseColor = Color.white;
    private Vector2Int _gridPosition;
    [SerializeField] private float _threadTitleYOffset = 0.6f;
    [SerializeField] private float _threadTitleScale = 0.01f;
    [SerializeField] private float _consumptionYOffset = 0.2f;
    [SerializeField] private float _productionYOffset = 0.4f;

    private RectTransform _threadTitleRect;
    private TextMeshProUGUI _threadTitleLabel;
    private GameObject _consumptionIconContainer;
    private GameObject _productionIconContainer;
    private Transform _sharedCanvas;
    private Camera _mainCamera;

    public ThreadState ThreadState => _threadState;
    public Vector2Int GridPosition => _gridPosition;
    public bool IsPreview { get; private set; }

    void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        if (_spriteRenderer != null)
        {
            _baseColor = _spriteRenderer.color;
        }
    }

    /// <summary>
    /// 실제 배치된 스레드를 초기화합니다.
    /// </summary>
    public void Initialize(ThreadState threadState, Transform sharedCanvas = null)
    {
        _threadState = threadState;
        IsPreview = false;
        _sharedCanvas = sharedCanvas;
        ApplyVisual();
        ResetColor();
        UpdateThreadTitle();
        UpdateResourceIcons();
    }

    /// <summary>
    /// 프리뷰 용도로 초기화합니다.
    /// </summary>
    public void InitializePreview(ThreadState threadState, Transform sharedCanvas = null)
    {
        _threadState = threadState;
        IsPreview = true;
        _sharedCanvas = sharedCanvas;
        ApplyVisual();
        SetPreviewColor(true);
        UpdateThreadTitle();
        UpdateResourceIcons();
    }
    public void SetGridPosition(Vector2Int gridPos)
    {
        _gridPosition = gridPos;
        PositionThreadTitle();
    }


    /// <summary>
    /// 프리뷰 가능 여부에 따라 색상을 조정합니다.
    /// </summary>
    public void SetPreviewColor(bool canPlace)
    {
        if (_spriteRenderer == null || !IsPreview)
            return;

        Color color = canPlace ? Color.green : Color.red;
        color.a = PREVIEW_ALPHA;
        _spriteRenderer.color = color;
    }

    /// <summary>
    /// 제거 모드 등에서 하이라이트 색상을 적용합니다.
    /// </summary>
    public void SetHighlight(Color highlightColor)
    {
        if (_spriteRenderer == null)
            return;

        _spriteRenderer.color = highlightColor;
    }

    /// <summary>
    /// 색상을 초기 상태로 되돌립니다.
    /// </summary>
    public void ResetColor()
    {
        if (_spriteRenderer == null)
            return;

        _spriteRenderer.color = _baseColor;
    }

    private void ApplyVisual()
    {
        if (_spriteRenderer == null)
            return;

        Sprite sprite = null;

        if (_threadState != null && !string.IsNullOrEmpty(_threadState.previewImagePath))
        {
            sprite = SpriteUtils.LoadSpriteFromFile(_threadState.previewImagePath);
        }

        //나중에
        //_spriteRenderer.sprite = sprite;
    }

    private void UpdateThreadTitle()
    {
        if (IsPreview || _threadState == null)
        {
            SetThreadTitleActive(false);
            return;
        }

        string title = string.IsNullOrWhiteSpace(_threadState.threadName)
            ? _threadState.threadId
            : _threadState.threadName;

        if (string.IsNullOrWhiteSpace(title))
        {
            SetThreadTitleActive(false);
            ClearResourceIconContainers();
            return;
        }

        if (!EnsureThreadTitleLabel())
            return;

        _threadTitleLabel.text = title;
        PositionThreadTitle();
        SetThreadTitleActive(true);
        UpdateResourceIcons();
    }

    private void PositionThreadTitle()
    {
        if (_threadTitleRect == null)
            return;

        float yOffset = _threadTitleYOffset;

        Vector3 worldPosition = transform.position + new Vector3(0f, yOffset, -0.1f);
        _threadTitleRect.position = worldPosition;
    }

    private void PositionResourceIcons()
    {
        if (_consumptionIconContainer != null)
        {
            Vector3 consumptionPosition = transform.position + new Vector3(0f, _consumptionYOffset, -0.12f);
            _consumptionIconContainer.transform.position = consumptionPosition;
        }

        if (_productionIconContainer != null)
        {
            Vector3 productionPosition = transform.position + new Vector3(0f, _productionYOffset, -0.12f);
            _productionIconContainer.transform.position = productionPosition;
        }
    }

    private void SetThreadTitleActive(bool active)
    {
        if (active && _threadTitleLabel == null)
        {
            if (!EnsureThreadTitleLabel())
                return;
        }

        if (_threadTitleRect != null)
        {
            _threadTitleRect.gameObject.SetActive(active);
        }

        if (!active)
        {
            ClearResourceIconContainers();
        }
    }

    void LateUpdate()
    {
        bool hasActiveLabel = _threadTitleRect != null && _threadTitleRect.gameObject.activeSelf;
        bool hasIcons = _consumptionIconContainer != null || _productionIconContainer != null;

        if (!hasActiveLabel && !hasIcons)
            return;

        if (hasActiveLabel)
        {
            PositionThreadTitle();
        }

        if (hasIcons)
        {
            PositionResourceIcons();
        }

        EnsureMainCamera();
        if (_mainCamera != null)
        {
            Quaternion rotation = Quaternion.identity;
            Vector3 toCameraLabel = hasActiveLabel ? (_mainCamera.transform.position - _threadTitleRect.position) : Vector3.zero;
            if (hasActiveLabel && toCameraLabel.sqrMagnitude > 0.0001f)
            {
                rotation = Quaternion.LookRotation(-toCameraLabel.normalized, Vector3.up);
                _threadTitleRect.rotation = rotation;
            }
            Vector3 toCameraIcons = _mainCamera.transform.position - transform.position;
            Quaternion iconRotation = Quaternion.identity;
            if (toCameraIcons.sqrMagnitude > 0.0001f)
            {
                iconRotation = Quaternion.LookRotation(-toCameraIcons.normalized, Vector3.up);
            }

            if (_consumptionIconContainer != null)
            {
                _consumptionIconContainer.transform.rotation = iconRotation;
            }

            if (_productionIconContainer != null)
            {
                _productionIconContainer.transform.rotation = iconRotation;
            }
        }
    }

    private bool EnsureThreadTitleLabel()
    {
        if (_threadTitleLabel != null)
            return true;

        if (_sharedCanvas == null)
            return false;

        GameObject titleObj = new GameObject("ThreadTitleLabel", typeof(RectTransform));
        _threadTitleRect = titleObj.GetComponent<RectTransform>();
        _threadTitleRect.SetParent(_sharedCanvas, false);
        _threadTitleRect.localScale = Vector3.one * _threadTitleScale;
        _threadTitleRect.localRotation = Quaternion.identity;
        _threadTitleRect.sizeDelta = new Vector2(200f, 60f);

        _threadTitleLabel = titleObj.AddComponent<TextMeshProUGUI>();
        _threadTitleLabel.fontSize = 24f;
        _threadTitleLabel.alignment = TextAlignmentOptions.Center;
        _threadTitleLabel.color = Color.black;
        _threadTitleLabel.textWrappingMode = TextWrappingModes.NoWrap;
        _threadTitleLabel.outlineWidth = 0.2f;
        _threadTitleLabel.outlineColor = Color.black;
        _threadTitleLabel.raycastTarget = false;

        return true;
    }

    private void UpdateResourceIcons()
    {
        ClearResourceIconContainers();

        if (IsPreview || _threadState == null || _sharedCanvas == null || GameManager.Instance == null)
            return;

        if (!_threadState.TryGetAggregatedResourceCounts(out Dictionary<string, int> consumptionCounts, out Dictionary<string, int> productionCounts))
            return;

        GameDataManager dataManager = GameDataManager.Instance;

        if (consumptionCounts.Count > 0)
        {
            _consumptionIconContainer = GameManager.Instance.CreateProductionIconContainerWithoutCanvas(
                _sharedCanvas,
                $"ThreadConsumption_{_threadState.threadId}",
                transform.position,
                0.005f
                );

            if (_consumptionIconContainer != null)
            {
                GameManager.Instance.CreateProductionIcons(
                    _consumptionIconContainer.transform,
                    consumptionCounts,
                    dataManager
                    );
            }
        }

        if (productionCounts.Count > 0)
        {
            _productionIconContainer = GameManager.Instance.CreateProductionIconContainerWithoutCanvas(
                _sharedCanvas,
                $"ThreadProduction_{_threadState.threadId}",
                transform.position,
                0.005f
                );

            if (_productionIconContainer != null)
            {
                GameManager.Instance.CreateProductionIcons(
                    _productionIconContainer.transform,
                    productionCounts,
                    dataManager);
            }
        }

        PositionResourceIcons();
    }

    private void ClearResourceIconContainers()
    {
        if (_consumptionIconContainer != null)
        {
            Object.Destroy(_consumptionIconContainer);
            _consumptionIconContainer = null;
        }

        if (_productionIconContainer != null)
        {
            Object.Destroy(_productionIconContainer);
            _productionIconContainer = null;
        }
    }

    private void CleanupThreadTitle()
    {
        if (_threadTitleRect != null)
        {
            Destroy(_threadTitleRect.gameObject);
            _threadTitleRect = null;
            _threadTitleLabel = null;
        }
    }

    private void EnsureMainCamera()
    {
        if (_mainCamera != null)
            return;

        if (GameManager.Instance != null && GameManager.Instance.MainCameraController != null)
        {
            _mainCamera = GameManager.Instance.MainCameraController.Camera;
        }

        if (_mainCamera == null)
        {
            _mainCamera = Camera.main;
        }
    }

    void OnDestroy()
    {
        CleanupThreadTitle();
        ClearResourceIconContainers();
    }
}