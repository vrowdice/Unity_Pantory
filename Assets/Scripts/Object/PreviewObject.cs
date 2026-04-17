using UnityEngine;
using TMPro;

public class PreviewObject : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _buildingSprite;
    [SerializeField] private GameObject _dirImageObject;
    [SerializeField] private TextMeshPro _buildingNameText;
    [SerializeField] private TextMeshPro _buildingPriceText;
    [SerializeField] private float _previewAlpha = 0.65f;

    public void SetBuildingData(BuildingData buildingData)
    {
        _buildingSprite.sprite = buildingData.buildingSprite;
        _buildingNameText.text = buildingData.displayName;
        _buildingPriceText.text = ReplaceUtils.FormatNumberWithCommas(buildingData.buildCost);
    }

    public void SetPreviewScale(Vector2Int size)
    {
        _buildingSprite.transform.localScale = new Vector3(size.x, size.y, 1f);
    }

    public void SetPlacementState(bool canPlace)
    {
        VisualManager visualManager = VisualManager.Instance;
        Color stateColor = canPlace ? visualManager.ValidColor : visualManager.InvalidColor;
        _buildingSprite.color = new Color(stateColor.r, stateColor.g, stateColor.b, _previewAlpha);
    }

    public void SetPreviewRotation(int rotation)
    {
        Quaternion zRotation = Quaternion.Euler(0f, 0f, -rotation * 90f);
        _buildingSprite.transform.localRotation = zRotation;
        _dirImageObject.transform.localRotation = zRotation;
    }
}
