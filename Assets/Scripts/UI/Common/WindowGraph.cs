using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WindowGraph : MonoBehaviour
{
    [SerializeField] private Sprite _circleSprite;
    [Header("Layout")]
    [SerializeField] private Vector2 _labelOffsetX = new Vector2(0f, -15f);
    [SerializeField] private Vector2 _labelOffsetY = new Vector2(-5f, 0f);
    [SerializeField] private float _graphPadding = 10f;
    [SerializeField, Range(10, 120)] private int _maxDataPoints = ResourceState.PriceHistoryCapacity;
    [SerializeField] private Color _lineColor = new Color(1f, 1f, 1f, 0.5f);

    private RectTransform _graphContainer;
    private RectTransform _labelTemplateX;
    private RectTransform _labelTemplateY;
    private RectTransform _lineTemplateX;
    private RectTransform _lineTemplateY;
    private readonly List<GameObject> _spawnedElements = new List<GameObject>();
    private bool _isInitialized;

    public void OnInitialize()
    {
        if (_isInitialized)
        {
            return;
        }

        _graphContainer = transform.Find("GraphContainer").GetComponent<RectTransform>();
        _labelTemplateX = _graphContainer.Find("LabelTemplateX").GetComponent<RectTransform>();
        _labelTemplateY = _graphContainer.Find("LabelTemplateY").GetComponent<RectTransform>();
        _lineTemplateX = _graphContainer.Find("LineTemplateX").GetComponent<RectTransform>();
        _lineTemplateY = _graphContainer.Find("LineTemplateY").GetComponent<RectTransform>();

        _labelTemplateX.gameObject.SetActive(false);
        _labelTemplateY.gameObject.SetActive(false);
        _lineTemplateX.gameObject.SetActive(false);
        _lineTemplateY.gameObject.SetActive(false);

        _isInitialized = true;
    }

    private GameObject CreateCircle(Vector2 anchoredPosition)
    {
        GameObject gameObject = new GameObject("Circle", typeof(Image));
        gameObject.transform.SetParent(_graphContainer, false);
        gameObject.GetComponent<Image>().sprite = _circleSprite;
        RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = new Vector2(11, 11);
        rectTransform.anchorMin = new Vector2(0, 0) ;
        rectTransform.anchorMax = new Vector2(0, 0);
        _spawnedElements.Add(gameObject);
        return gameObject;
    }

    public void ShowGraph(IReadOnlyList<float> valueList)
    {
        if (!_isInitialized)
        {
            OnInitialize();
        }

        ClearGraph();

        if (valueList == null || valueList.Count == 0 || _graphContainer == null)
        {
            return;
        }

        int targetCount = Mathf.Min(_maxDataPoints, valueList.Count);
        int startIndex = Mathf.Max(0, valueList.Count - targetCount);

        float graphHeight = _graphContainer.rect.height;
        float graphWidth = _graphContainer.rect.width - _graphPadding * 2f;
        if (graphWidth <= 0f)
        {
            graphWidth = _graphContainer.rect.width;
        }
        if (graphHeight <= 0f)
        {
            graphHeight = _graphContainer.rect.height;
        }
        float yMin = float.MaxValue;
        float yMax = float.MinValue;

        // 데이터 포인트를 정수로 변환하여 저장
        List<int> intValueList = new List<int>();
        for (int i = startIndex; i < valueList.Count; i++)
        {
            int intValue = Mathf.RoundToInt(valueList[i]);
            intValueList.Add(intValue);
            if (intValue < yMin) yMin = intValue;
            if (intValue > yMax) yMax = intValue;
        }

        if (Mathf.Approximately(yMax, yMin))
        {
            yMin -= 1f;
            yMax += 1f;
        }

        float verticalRange = yMax - yMin;
        float xSize = targetCount > 1 ? graphWidth / (targetCount - 1) : graphWidth;

        GameObject lastDot = null;
        for (int i = 0; i < targetCount; i++)
        {
            // 정수 값 사용
            int intValue = intValueList[i];
            float normalizedValue = (intValue - yMin) / verticalRange;
            float xPosition = _graphPadding + i * xSize;
            float yPosition = normalizedValue * graphHeight;
            GameObject dot = CreateCircle(new Vector2(xPosition, yPosition));
            if (lastDot != null)
            {
                CreateDotConnection(lastDot.GetComponent<RectTransform>().anchoredPosition, dot.GetComponent<RectTransform>().anchoredPosition);
            }
            lastDot = dot;

            RectTransform labelX = Instantiate(_labelTemplateX, _graphContainer);
            labelX.gameObject.SetActive(true);
            Vector2 baseXLabelPos = new Vector2(xPosition, _labelTemplateX.anchoredPosition.y);
            labelX.anchoredPosition = baseXLabelPos + _labelOffsetX;
            int dayIndex = valueList.Count - targetCount + i + 1;
            labelX.GetComponent<Text>().text = dayIndex.ToString();
            _spawnedElements.Add(labelX.gameObject);

            RectTransform lineX = Instantiate(_lineTemplateX, _graphContainer);
            lineX.gameObject.SetActive(true);
            lineX.anchoredPosition = new Vector2(xPosition, _lineTemplateX.anchoredPosition.y);
            _spawnedElements.Add(lineX.gameObject);
        }

        // 평균값 계산
        float averageValue = 0f;
        if (intValueList.Count > 0)
        {
            int sum = 0;
            foreach (int val in intValueList)
            {
                sum += val;
            }
            averageValue = sum / (float)intValueList.Count;
        }

        // Y축 라벨 생성 (최소 1 이상 차이나도록)
        int intYMin = Mathf.RoundToInt(yMin);
        int intYMax = Mathf.RoundToInt(yMax);
        int range = intYMax - intYMin;

        // 적절한 간격 계산 (최소 1 이상)
        int step = Mathf.Max(1, Mathf.RoundToInt(range / 10f));
        
        // 평균값을 중심으로 라벨 위치 조정
        int centerValue = Mathf.RoundToInt(averageValue);
        List<int> labelValues = new List<int>();
        
        // 평균값 중심으로 위아래로 라벨 생성
        for (int offset = 0; offset <= range; offset += step)
        {
            int valueAbove = centerValue + offset;
            int valueBelow = centerValue - offset;
            
            if (valueAbove <= intYMax && valueAbove >= intYMin && !labelValues.Contains(valueAbove))
            {
                labelValues.Add(valueAbove);
            }
            if (valueBelow >= intYMin && valueBelow <= intYMax && !labelValues.Contains(valueBelow) && offset > 0)
            {
                labelValues.Add(valueBelow);
            }
        }
        
        // 정렬
        labelValues.Sort();

        // 라벨과 선 생성
        foreach (int labelValue in labelValues)
        {
            float normalizedValue = (labelValue - yMin) / verticalRange;
            float yPosition = normalizedValue * graphHeight;

            RectTransform labelY = Instantiate(_labelTemplateY, _graphContainer);
            labelY.gameObject.SetActive(true);
            Vector2 baseYLabelPos = new Vector2(_labelTemplateY.anchoredPosition.x, yPosition);
            labelY.anchoredPosition = baseYLabelPos + _labelOffsetY;
            labelY.GetComponent<Text>().text = labelValue.ToString();
            _spawnedElements.Add(labelY.gameObject);

            RectTransform lineY = Instantiate(_lineTemplateY, _graphContainer);
            lineY.gameObject.SetActive(true);
            lineY.anchoredPosition = new Vector2(_lineTemplateY.anchoredPosition.x, yPosition);
            _spawnedElements.Add(lineY.gameObject);
        }
    }

    private void CreateDotConnection(Vector2 dotPositionA, Vector2 dotPositionB)
    {
        GameObject gameObject = new GameObject("DotConnection", typeof(Image));
        gameObject.transform.SetParent(_graphContainer, false);
        gameObject.GetComponent<Image>().color = _lineColor;
        RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
        Vector2 direction = (dotPositionB - dotPositionA).normalized;
        float distance = Vector2.Distance(dotPositionA, dotPositionB);
        rectTransform.anchorMin = new Vector2(0, 0);
        rectTransform.anchorMax = new Vector2(0, 0);
        rectTransform.sizeDelta = new Vector2(distance, 3f);
        rectTransform.anchoredPosition = dotPositionA + direction * distance * 0.5f;
        rectTransform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        _spawnedElements.Add(gameObject);
    }

    private void ClearGraph()
    {
        foreach (var element in _spawnedElements)
        {
            if (element != null)
            {
                Destroy(element);
            }
        }

        _spawnedElements.Clear();
    }
}
