using TMPro;
using UnityEngine;
using System.Collections.Generic;

public class BlueprintPreviewObject : MonoBehaviour
{
    [SerializeField] private TextMeshPro _blueprintNameText;
    [SerializeField] private TextMeshPro _blueprintPriceText;
    [SerializeField] private float _previewAlpha = 0.65f;

    private readonly List<SpriteRenderer> _spriteRenderers = new List<SpriteRenderer>();

    public void SetBlueprintInfo(string blueprintName, long totalPrice)
    {
        if (_blueprintNameText != null)
            _blueprintNameText.text = string.IsNullOrEmpty(blueprintName) ? "Blueprint" : blueprintName;
        if (_blueprintPriceText != null)
            _blueprintPriceText.text = ReplaceUtils.FormatNumberWithCommas(totalPrice);
    }

    public void RegisterSpriteRenderer(SpriteRenderer spriteRenderer)
    {
        if (spriteRenderer == null) return;
        _spriteRenderers.Add(spriteRenderer);
    }

    public void ClearRegisteredSpriteRenderers()
    {
        _spriteRenderers.Clear();
    }

    public void SetPlacementState(bool canPlace)
    {
        VisualManager visualManager = VisualManager.Instance;
        Color stateColor = canPlace ? visualManager.ValidColor : visualManager.InvalidColor;
        Color color = new Color(stateColor.r, stateColor.g, stateColor.b, _previewAlpha);
        for (int i = 0; i < _spriteRenderers.Count; i++)
        {
            SpriteRenderer sr = _spriteRenderers[i];
            if (sr != null)
                sr.color = color;
        }
    }

}
