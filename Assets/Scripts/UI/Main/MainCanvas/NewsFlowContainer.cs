using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class NewsFlowContainer : MonoBehaviour
{
    [SerializeField] private GameObject _flowText;
    [SerializeField] private float _flowTextDuration = 5f;
    [SerializeField] private float _scrollPixelsPerSecond = 120f;
    [SerializeField] private float _startPadding = 8f;

    private RectTransform _rectTransform;
    private DataManager _dataManager;
    private readonly Queue<NewsState> _pendingTitles = new();
    private Coroutine _releasePumpCoroutine;

    public void Init()
    {
        _rectTransform = transform as RectTransform;
        _dataManager = DataManager.Instance;

        if (_dataManager != null && _dataManager.News != null)
        {
            _dataManager.News.OnNewsChanged -= OnNewsChanged;
            _dataManager.News.OnNewsChanged += OnNewsChanged;
        }

        if (_releasePumpCoroutine == null)
        {
            _releasePumpCoroutine = StartCoroutine(ReleasePump());
        }
    }

    private void OnDestroy()
    {
        if (_dataManager != null && _dataManager.News != null)
        {
            _dataManager.News.OnNewsChanged -= OnNewsChanged;
        }
    }

    private void OnNewsChanged(NewsState newsState)
    {
        if (newsState == null || newsState.IsExpired)
        {
            return;
        }

        string title = ResolveTitle(newsState);
        if (string.IsNullOrEmpty(title))
        {
            return;
        }

        _pendingTitles.Enqueue(newsState);
    }

    private string ResolveTitle(NewsState newsState)
    {
        NewsData data = _dataManager.News.GetNewsData(newsState.id);
        if (data != null && !string.IsNullOrEmpty(data.displayName))
        {
            return data.displayName;
        }

        return newsState.id;
    }

    private IEnumerator ReleasePump()
    {
        WaitForSeconds waitStep = new WaitForSeconds(_flowTextDuration);

        while (true)
        {
            yield return waitStep;

            if (_pendingTitles.Count == 0 || _flowText == null || _rectTransform == null)
            {
                continue;
            }

            NewsState newsState = _pendingTitles.Dequeue();
            StartCoroutine(FlowOneTitle(newsState));
        }
    }

    private IEnumerator FlowOneTitle(NewsState newsState)
    {
        GameObject textObj = Instantiate(_flowText, _rectTransform, false);
        RectTransform childRt = textObj.GetComponent<RectTransform>();
        TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();

        if (childRt == null || tmp == null)
        {
            Destroy(textObj);
            yield break;
        }

        tmp.text = newsState.id.Localize(LocalizationUtils.TABLE_NEWS);
        tmp.ForceMeshUpdate();

        Vector2 preferred = tmp.GetPreferredValues(tmp.text);
        float textWidth = Mathf.Max(preferred.x, 1f);
        float textHeight = Mathf.Max(preferred.y, childRt.sizeDelta.y);

        childRt.anchorMin = new Vector2(0.5f, 0.5f);
        childRt.anchorMax = new Vector2(0.5f, 0.5f);
        childRt.pivot = new Vector2(0.5f, 0.5f);
        childRt.sizeDelta = new Vector2(textWidth, textHeight);

        Rect parentRect = _rectTransform.rect;
        float startX = parentRect.xMax + textWidth * 0.5f + _startPadding;
        childRt.anchoredPosition = new Vector2(startX, 0f);

        bool hasIntersectedParent = false;

        while (textObj != null && childRt != null)
        {
            parentRect = _rectTransform.rect;
            childRt.anchoredPosition += Vector2.left * (_scrollPixelsPerSecond * Time.deltaTime);

            Rect childRect = GetChildRectInParentSpace(childRt);
            if (parentRect.Overlaps(childRect, true))
            {
                hasIntersectedParent = true;
            }
            else if (hasIntersectedParent)
            {
                Destroy(textObj);
                yield break;
            }

            yield return null;
        }
    }

    private Rect GetChildRectInParentSpace(RectTransform childRt)
    {
        Bounds childInParent = RectTransformUtility.CalculateRelativeRectTransformBounds(_rectTransform, childRt);
        return Rect.MinMaxRect(childInParent.min.x, childInParent.min.y, childInParent.max.x, childInParent.max.y);
    }
}
