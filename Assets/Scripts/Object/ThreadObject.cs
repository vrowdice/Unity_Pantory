using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// 월드 상에 배치되는 스레드(건물/오브젝트)의 시각적 요소와 정보 UI를 관리합니다.
/// </summary>
public class ThreadObject : MonoBehaviour
{

    [Header("Size Settings")]
    [SerializeField] private Vector2 _targetWorldSize = Vector2.one;
    [SerializeField] private float _threadTitleScale = 0.01f;
    [SerializeField] private float _resourceIconScale = 0.005f;

    [Header("Offset Settings")]
    [SerializeField] private float _threadTitleYOffset = 0.6f;
    [SerializeField] private float _consumptionYOffset = 0.2f;
    [SerializeField] private float _productionYOffset = 0.4f;

    private GameManager _gameManager;
    private ThreadState _threadState;
    private SpriteRenderer _spriteRenderer;
    private Color _baseColor = Color.white;
    private Vector2Int _gridPosition;

    private RectTransform _threadTitleRect;
    private TextMeshProUGUI _threadTitleLabel;
    private GameObject _consumptionIconContainer;
    private GameObject _productionIconContainer;
    private Camera _mainCamera;

    public ThreadState ThreadState => _threadState;
    public Vector2Int GridPosition => _gridPosition;
    public bool IsPreview { get; private set; }

    /// <summary>
    /// 실제 배치된 스레드 오브젝트를 초기화합니다.
    /// </summary>
    public void Init(ThreadState threadState, GameManager gameManager)
    {
        _gameManager = gameManager;
        _spriteRenderer = GetComponent<SpriteRenderer>();

        InitializeCommon(threadState, false);
        ResetColor();
    }

    /// <summary>
    /// 배치 전 프리뷰 모드로 초기화합니다.
    /// </summary>
    public void InitializePreview(ThreadState threadState)
    {
        _gameManager = GameManager.Instance;
        _spriteRenderer = GetComponent<SpriteRenderer>();
        InitializeCommon(threadState, true);
        SetPreviewColor(true);
    }

    private void InitializeCommon(ThreadState threadState, bool isPreview)
    {
        _threadState = threadState;
        IsPreview = isPreview;

        Sprite sprite = SpriteUtils.LoadSpriteFromFile(_threadState.previewImagePath);
        _spriteRenderer.sprite = sprite;
        _spriteRenderer.enabled = sprite != null;

        if (sprite != null)
        {
            GameObjectUtils.SetSpriteToWorldSize(transform, sprite, _targetWorldSize);
        }

        UpdateThreadTitle();
        UpdateResourceIcons();
    }

    public void SetGridPosition(Vector2Int gridPos)
    {
        _gridPosition = gridPos;
        UpdatePositions();
    }

    /// <summary>
    /// 배치 가능 여부에 따라 프리뷰 색상을 변경합니다.
    /// </summary>
    public void SetPreviewColor(bool canPlace)
    {
        if (!IsPreview || _spriteRenderer == null) return;

        VisualManager visualManager = VisualManager.Instance;
        Color baseColor = canPlace 
            ? (visualManager != null ? visualManager.ThreadPreviewValidColor : Color.green)
            : (visualManager != null ? visualManager.ThreadPreviewInvalidColor : Color.red);
        
        float alpha = visualManager != null ? visualManager.ThreadPreviewAlpha : 0.6f;
        baseColor.a = alpha;
        _spriteRenderer.color = baseColor;
    }

    public void SetHighlight(Color highlightColor)
    {
        if (_spriteRenderer != null) _spriteRenderer.color = highlightColor;
    }

    public void ResetColor()
    {
        if (_spriteRenderer != null) _spriteRenderer.color = _baseColor;
    }

    private void UpdateThreadTitle()
    {
        string title = string.IsNullOrWhiteSpace(_threadState.threadName) ? _threadState.threadId : _threadState.threadName;
        if (string.IsNullOrWhiteSpace(title) || !EnsureThreadTitleLabel()) return;

        _threadTitleLabel.text = title;
    }

    private void UpdatePositions()
    {
        _threadTitleRect.position = transform.position + new Vector3(0.0f, _threadTitleYOffset, 0.0f);
        if( _consumptionIconContainer != null || _productionIconContainer != null )
        {
            _consumptionIconContainer.transform.position = transform.position + new Vector3(0.0f, _consumptionYOffset, 0.0f);
            _productionIconContainer.transform.position = transform.position + new Vector3(0.0f, _productionYOffset, 0.0f);
        }
    }

    private void LateUpdate()
    {
        UpdatePositions();
    }

    private bool EnsureThreadTitleLabel()
    {
        if (_threadTitleLabel != null) return true;
        if (_gameManager == null) return false;
        
        Transform sharedCanvas = _gameManager.GetWorldCanvas();
        if (sharedCanvas == null) return false;

        GameObject titleObj = new GameObject("ThreadTitleLabel", typeof(RectTransform));
        _threadTitleRect = titleObj.GetComponent<RectTransform>();
        _threadTitleRect.SetParent(sharedCanvas, false);
        _threadTitleRect.localScale = Vector3.one * _threadTitleScale;

        _threadTitleLabel = titleObj.AddComponent<TextMeshProUGUI>();
        _threadTitleLabel.fontSize = 20f;
        _threadTitleLabel.alignment = TextAlignmentOptions.Center;
        _threadTitleLabel.color = Color.black;
        _threadTitleLabel.raycastTarget = false;

        return true;
    }

    private void UpdateResourceIcons()
    {
        ClearProductionIconContainers();
        if (IsPreview || _threadState == null) return;

        if (!EnsureGameManager()) return;

        Transform sharedCanvas = _gameManager.GetWorldCanvas();
        if (sharedCanvas == null) return;

        if (!_threadState.TryGetAggregatedResourceCounts(out var consumption, out var production)) return;

        _consumptionIconContainer = CreateProductionIconContainer(consumption, "Consumption", _consumptionYOffset, sharedCanvas);
        _productionIconContainer = CreateProductionIconContainer(production, "Production", _productionYOffset, sharedCanvas);
    }

    /// <summary>
    /// Production Icon 컨테이너를 생성합니다.
    /// </summary>
    private GameObject CreateProductionIconContainer(Dictionary<string, int> counts, string suffix, float yOffset, Transform sharedCanvas)
    {
        if (counts == null || counts.Count == 0 || _gameManager == null)
            return null;

        Vector3 worldPosition = transform.position + new Vector3(0f, yOffset, 0f);
        
        GameObject container = _gameManager.CreateProductionIconContainer(
            sharedCanvas, 
            $"Thread{suffix}_{_threadState.threadId}", 
            worldPosition, 
            _resourceIconScale,
            counts);
        
        return container;
    }

    /// <summary>
    /// GameManager 참조를 확인하고 가져옵니다.
    /// </summary>
    private bool EnsureGameManager()
    {
        if (_gameManager != null)
            return true;
        
        _gameManager = GameManager.Instance;
        return _gameManager != null;
    }

    /// <summary>
    /// Production Icon 컨테이너들을 정리합니다.
    /// </summary>
    private void ClearProductionIconContainers()
    {
        if (_consumptionIconContainer != null)
        {
            // 컨테이너 내부의 ProductionIcon들을 풀로 반환
            if (PoolingManager.Instance != null)
            {
                PoolingManager.Instance.ClearChildrenToPool(_consumptionIconContainer.transform);
            }
            Destroy(_consumptionIconContainer);
            _consumptionIconContainer = null;
        }
        
        if (_productionIconContainer != null)
        {
            // 컨테이너 내부의 ProductionIcon들을 풀로 반환
            if (PoolingManager.Instance != null)
            {
                PoolingManager.Instance.ClearChildrenToPool(_productionIconContainer.transform);
            }
            Destroy(_productionIconContainer);
            _productionIconContainer = null;
        }
    }

    private void OnDestroy()
    {
        if (_threadTitleRect != null) Destroy(_threadTitleRect.gameObject);
        ClearProductionIconContainers();
    }
}