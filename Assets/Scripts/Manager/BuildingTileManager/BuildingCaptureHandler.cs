using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 건물 레이아웃 캡쳐를 처리하는 핸들러
/// </summary>
public class BuildingCaptureHandler
{
    private readonly BuildingTileManager _buildingTileManager;
    private readonly BuildingGridHandler _gridHandler;
    private readonly dataManager _dataManager;
    private readonly Camera _mainCamera;

    public BuildingCaptureHandler(BuildingTileManager buildingTileManager)
    {
        _buildingTileManager = buildingTileManager;
        _gridHandler = buildingTileManager.GridGenHandler;
        _dataManager = buildingTileManager.DataManager;
        _mainCamera = buildingTileManager.MainCamera;
    }

    /// <summary>
    /// 현재 Thread의 건물 레이아웃을 이미지로 캡처합니다.
    /// </summary>
    public string CaptureThreadLayout(string threadId)
    {
        if (string.IsNullOrEmpty(threadId) || _mainCamera == null) return null;

        List<BuildingState> buildingStates = (threadId == _buildingTileManager.CurrentThreadId) 
            ? _buildingTileManager.GetCurrentBuildingStates() 
            : _dataManager.Thread.GetBuildingStates(threadId);
            
        if (buildingStates == null || buildingStates.Count == 0) return null;

        // 건물들의 경계 계산
        int minX = int.MaxValue, minY = int.MaxValue;
        int maxX = int.MinValue, maxY = int.MinValue;
        
        foreach (var buildingState in buildingStates)
        {
            BuildingData buildingData = _dataManager.Building.GetBuildingData(buildingState.buildingId);
            if (buildingData != null)
            {
                Vector2Int rotatedSize = GetRotatedSize(buildingData.size, buildingState.rotation);
                minX = Mathf.Min(minX, buildingState.positionX);
                minY = Mathf.Min(minY, buildingState.positionY);
                maxX = Mathf.Max(maxX, buildingState.positionX + rotatedSize.x);
                maxY = Mathf.Max(maxY, buildingState.positionY + rotatedSize.y);
            }
        }
        
        if (minX == int.MaxValue) return null;

        // 패딩과 그리드 크기 계산
        int padding = 2;
        int gridWidth = (maxX + padding) - (minX - padding);
        int gridHeight = (maxY + padding) - (minY - padding);
        float centerX = (minX + maxX) / 2f;
        float centerY = (minY + maxY) / 2f;

        // 렌더 텍스처 크기 계산
        int width = Mathf.Max(512, gridWidth * 64);
        int height = Mathf.Max(512, gridHeight * 64);
        RenderTexture renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);

        // 카메라 원래 설정 저장
        RenderTexture originalRT = _mainCamera.targetTexture;
        CameraClearFlags originalClearFlags = _mainCamera.clearFlags;
        Color originalBackgroundColor = _mainCamera.backgroundColor;
        float originalOrthographicSize = _mainCamera.orthographicSize;
        Vector3 originalPosition = _mainCamera.transform.position;
        
        try
        {
            // 카메라 설정 변경
            _mainCamera.targetTexture = renderTexture;
            _mainCamera.clearFlags = CameraClearFlags.Color;
            _mainCamera.backgroundColor = new Color(0, 0, 0, 0);
            _mainCamera.transform.position = new Vector3(centerX, -centerY, originalPosition.z);
            _mainCamera.orthographicSize = Mathf.Max(gridWidth / _mainCamera.aspect, gridHeight) / 2f + padding;
            _mainCamera.Render();

            // 텍스처 읽기
            RenderTexture.active = renderTexture;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false); 
            texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            texture.Apply();
            
            // PNG로 인코딩 및 저장
            byte[] imageBytes = texture.EncodeToPNG();
            string filePath = System.IO.Path.Combine(Application.persistentDataPath, $"ThreadPreview_{threadId}.png");
            System.IO.File.WriteAllBytes(filePath, imageBytes);

            Debug.Log($"[BuildingCaptureHandler] Thread layout captured: {filePath} (Size: {gridWidth}x{gridHeight})");
            return filePath;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[BuildingCaptureHandler] Failed to capture thread layout: {e.Message}");
            return null;
        }
        finally
        {
            // 카메라 원래 설정 복원
            _mainCamera.targetTexture = originalRT;
            _mainCamera.clearFlags = originalClearFlags;
            _mainCamera.backgroundColor = originalBackgroundColor;
            _mainCamera.orthographicSize = originalOrthographicSize;
            _mainCamera.transform.position = originalPosition;
            RenderTexture.active = null;

            // 렌더 텍스처 정리
            if (renderTexture != null)
            {
                renderTexture.Release();
                Object.Destroy(renderTexture);
            }
        }
    }

    /// <summary>
    /// 회전에 따라 건물 크기를 계산합니다.
    /// </summary>
    private Vector2Int GetRotatedSize(Vector2Int size, int rotation)
    {
        rotation %= 4;
        if (rotation == 1 || rotation == 3) return new Vector2Int(size.y, size.x);
        return size;
    }
}
