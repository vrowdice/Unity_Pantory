using System;
using System.IO;
using UnityEngine;
using System.Collections.Generic;

public static class SpriteUtils
{
    private static readonly Dictionary<string, Sprite> _spriteCache = new Dictionary<string, Sprite>();

    /// <summary>
    /// 파일 경로에서 이미지를 읽어 Sprite로 생성합니다.
    /// </summary>
    /// <param name="imagePath">이미지 파일의 절대 경로</param>
    /// <param name="pivot">Sprite 생성 시 사용할 피벗 값 (기본값: 중앙)</param>
    /// <param name="pixelsPerUnit">Sprite 생성 시 사용할 픽셀 퍼 유닛 값 (기본값: 100)</param>
    /// <param name="useCache">동일 경로에 대해 캐싱된 Sprite를 재사용할지 여부</param>
    public static Sprite LoadSpriteFromFile(string imagePath, Vector2? pivot = null, float pixelsPerUnit = 100f, bool useCache = true)
    {
        if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
            return null;

        if (useCache && _spriteCache.TryGetValue(imagePath, out Sprite cachedSprite) && cachedSprite != null)
        {
            return cachedSprite;
        }

        try
        {
            byte[] imageData = File.ReadAllBytes(imagePath);
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);

            if (!texture.LoadImage(imageData, markNonReadable: false))
            {
                UnityEngine.Object.Destroy(texture);
                Debug.LogWarning($"[SpriteUtils] Failed to decode image data from: {imagePath}");
                return null;
            }

            Vector2 spritePivot = pivot ?? new Vector2(0.5f, 0.5f);
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), spritePivot, pixelsPerUnit);

            if (useCache)
            {
                _spriteCache[imagePath] = sprite;
            }

            return sprite;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SpriteUtils] Failed to load sprite from '{imagePath}': {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// 캐시된 Sprite를 제거하고 메모리를 해제합니다.
    /// </summary>
    public static void UnloadSprite(string imagePath)
    {
        if (string.IsNullOrEmpty(imagePath))
            return;

        if (_spriteCache.TryGetValue(imagePath, out Sprite sprite) && sprite != null)
        {
            Texture2D texture = sprite.texture;
            UnityEngine.Object.Destroy(sprite);

            if (texture != null)
            {
                UnityEngine.Object.Destroy(texture);
            }
        }

        _spriteCache.Remove(imagePath);
    }

    /// <summary>
    /// 모든 캐시된 Sprite를 해제합니다.
    /// </summary>
    public static void ClearCache()
    {
        foreach (var kvp in _spriteCache)
        {
            Sprite sprite = kvp.Value;
            if (sprite == null)
                continue;

            Texture2D texture = sprite.texture;
            UnityEngine.Object.Destroy(sprite);

            if (texture != null)
            {
                UnityEngine.Object.Destroy(texture);
            }
        }

        _spriteCache.Clear();
    }
}

