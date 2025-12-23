using System.Collections.Generic;
using UnityEngine;
using System.IO;

/// <summary>
/// 건물 레이아웃을 이미지로 캡처하여 프리뷰를 생성하는 핸들러입니다.
/// </summary>
public class BuildingCaptureHandler
{
    private readonly BuildingTileManager _manager;
    private readonly List<BuildingState> _states;
    private readonly Camera _mainCamera;

    public BuildingCaptureHandler(BuildingTileManager buildingTileManager, List<BuildingState> currentBuildingStates)
    {
        _manager = buildingTileManager;
        _states = currentBuildingStates ?? new List<BuildingState>();
        _mainCamera = buildingTileManager.MainCamera;
    }

    /// <summary>
    /// 현재 레이아웃을 캡처합니다. UI 레이어를 제외하고 월드 객체만 렌더링합니다.
    /// </summary>
    public string CaptureThreadLayout(string threadId, List<BuildingState> customStates = null)
    {
        if (string.IsNullOrEmpty(threadId) || _mainCamera == null) return null;

        var statesToUse = customStates ?? _states;
        if (statesToUse.Count == 0) return null;

        // 1. 영역 계산 (Bounds)
        CalculateBounds(statesToUse, out Vector2 center, out Vector2 size, out int gridW, out int gridH);

        // 2. 렌더 텍스처 준비
        int padding = 2;
        int resWidth = Mathf.Max(512, (gridW + padding * 2) * 64);
        int resHeight = Mathf.Max(512, (gridH + padding * 2) * 64);
        RenderTexture rt = new RenderTexture(resWidth, resHeight, 24, RenderTextureFormat.ARGB32);

        // 3. 카메라 상태 백업
        var backup = BackupCamera(_mainCamera);

        try
        {
            // 4. 카메라 설정 (UI 제외 설정 핵심)
            _mainCamera.targetTexture = rt;
            _mainCamera.clearFlags = CameraClearFlags.SolidColor;
            _mainCamera.backgroundColor = new Color(0, 0, 0, 0); // 투명 배경

            // UI 레이어(기본 5번)를 제외하고 모든 레이어 렌더링
            _mainCamera.cullingMask &= ~(1 << LayerMask.NameToLayer("UI"));

            _mainCamera.transform.position = new Vector3(center.x, -center.y, backup.pos.z);
            _mainCamera.orthographicSize = Mathf.Max(gridW / _mainCamera.aspect, gridH) / 2f + padding;

            _mainCamera.Render();

            // 5. 텍스처 추출 및 저장
            return SaveTextureToFile(rt, threadId);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[CaptureHandler] Capture Failed: {e.Message}");
            return null;
        }
        finally
        {
            // 6. 상태 복구
            RestoreCamera(_mainCamera, backup);
            rt.Release();
            Object.Destroy(rt);
        }
    }

    private void CalculateBounds(List<BuildingState> states, out Vector2 center, out Vector2 size, out int gridW, out int gridH)
    {
        int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;

        foreach (var state in states)
        {
            var data = _manager.DataManager.Building.GetBuildingData(state.buildingId);
            if (data == null) continue;

            Vector2Int rSize = GetRotatedSize(data.size, state.rotation);
            minX = Mathf.Min(minX, state.positionX);
            minY = Mathf.Min(minY, state.positionY);
            maxX = Mathf.Max(maxX, state.positionX + rSize.x);
            maxY = Mathf.Max(maxY, state.positionY + rSize.y);
        }

        gridW = maxX - minX;
        gridH = maxY - minY;
        center = new Vector2((minX + maxX) / 2f, (minY + maxY) / 2f);
        size = new Vector2(gridW, gridH);
    }

    private string SaveTextureToFile(RenderTexture rt, string threadId)
    {
        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();

        byte[] bytes = tex.EncodeToPNG();
        string path = Path.Combine(Application.persistentDataPath, $"ThreadPreview_{threadId}.png");
        File.WriteAllBytes(path, bytes);

        RenderTexture.active = null;
        Object.Destroy(tex);
        return path;
    }

    #region Camera State Management

    private struct CameraBackup
    {
        public RenderTexture rt;
        public CameraClearFlags flags;
        public Color bg;
        public float size;
        public Vector3 pos;
        public int mask;
    }

    private CameraBackup BackupCamera(Camera cam) => new CameraBackup
    {
        rt = cam.targetTexture,
        flags = cam.clearFlags,
        bg = cam.backgroundColor,
        size = cam.orthographicSize,
        pos = cam.transform.position,
        mask = cam.cullingMask
    };

    private void RestoreCamera(Camera cam, CameraBackup backup)
    {
        cam.targetTexture = backup.rt;
        cam.clearFlags = backup.flags;
        cam.backgroundColor = backup.bg;
        cam.orthographicSize = backup.size;
        cam.transform.position = backup.pos;
        cam.cullingMask = backup.mask;
        RenderTexture.active = null;
    }

    #endregion

    private Vector2Int GetRotatedSize(Vector2Int size, int rotation)
    {
        return (rotation % 4 == 1 || rotation % 4 == 3) ? new Vector2Int(size.y, size.x) : size;
    }
}