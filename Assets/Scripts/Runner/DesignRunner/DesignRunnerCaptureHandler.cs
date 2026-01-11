using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class DesignRunnerCaptureHandler
{
    private readonly DesignRunner _manager;
    private readonly Camera _mainCamera;
    private readonly DesignRunnerGridHandler _gridHandler;

    public DesignRunnerCaptureHandler(DesignRunner manager)
    {
        _manager = manager;
        _mainCamera = manager.MainCamera;
        _gridHandler = manager.GridGenHandler;
    }

    /// <summary>
    /// 특정 스레드(건물 배치 데이터)를 캡처하여 파일로 저장합니다.
    /// </summary>
    public string CaptureThreadLayout(string threadId, List<BuildingState> statesToCapture)
    {
        if (string.IsNullOrEmpty(threadId) || _mainCamera == null || statesToCapture == null || statesToCapture.Count == 0)
        {
            Debug.LogWarning("[Capture] 캡처할 데이터가 없거나 카메라가 없습니다.");
            return null;
        }

        Bounds worldBounds = CalculateWorldBounds(statesToCapture);

        int paddingPixels = 64;
        int pixelsPerUnit = 64;

        int resWidth = Mathf.Max(512, (int)(worldBounds.size.x * pixelsPerUnit) + paddingPixels);
        int resHeight = Mathf.Max(512, (int)(worldBounds.size.y * pixelsPerUnit) + paddingPixels);

        RenderTexture rt = new RenderTexture(resWidth, resHeight, 24, RenderTextureFormat.ARGB32);

        CameraBackup backup = new CameraBackup(_mainCamera);

        try
        {
            SetupCameraForCapture(_mainCamera, rt, worldBounds);

            _mainCamera.Render();
            return SaveRenderTextureToPng(rt, threadId);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Capture] 캡처 실패: {e.Message}");
            return null;
        }
        finally
        {
            RestoreCamera(_mainCamera, backup);

            if (rt != null)
            {
                rt.Release();
                Object.Destroy(rt);
            }
        }
    }

    /// <summary>
    /// 그리드 좌표 기반의 BuildingState들을 월드 좌표 Bounds로 변환합니다.
    /// </summary>
    private Bounds CalculateWorldBounds(List<BuildingState> states)
    {
        if (states.Count == 0) return new Bounds(Vector3.zero, Vector3.one);

        Vector3 min = new Vector3(float.MaxValue, float.MaxValue, 0);
        Vector3 max = new Vector3(float.MinValue, float.MinValue, 0);

        foreach (var state in states)
        {
            BuildingData data = _manager.DataManager.Building.GetBuildingData(state.buildingId);
            if (data == null) continue;

            Vector2Int rotatedSize = GetRotatedGridSize(data.size, state.rotation);

            Vector3 worldCenter = _gridHandler.GridToWorldPosition(new Vector2Int(state.positionX, state.positionY), rotatedSize);

            Vector3 halfSize = new Vector3(rotatedSize.x * 0.5f, rotatedSize.y * 0.5f, 0);

            Vector3 buildingMin = worldCenter - halfSize;
            Vector3 buildingMax = worldCenter + halfSize;

            min = Vector3.Min(min, buildingMin);
            max = Vector3.Max(max, buildingMax);
        }

        Bounds bounds = new Bounds();
        bounds.SetMinMax(min, max);
        return bounds;
    }

    /// <summary>
    /// 캡처를 위해 카메라 위치와 옵션을 변경합니다.
    /// </summary>
    private void SetupCameraForCapture(Camera cam, RenderTexture rt, Bounds targetBounds)
    {
        cam.targetTexture = rt;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0, 0, 0, 0);

        cam.cullingMask &= ~(1 << LayerMask.NameToLayer("UI"));

        cam.transform.position = new Vector3(targetBounds.center.x, targetBounds.center.y, -10f);

        float padding = 1.0f;
        float targetHeight = targetBounds.size.y / 2f;
        float targetWidthInHeight = (targetBounds.size.x / cam.aspect) / 2f;

        cam.orthographicSize = Mathf.Max(targetHeight, targetWidthInHeight) + padding;
    }

    private string SaveRenderTextureToPng(RenderTexture rt, string threadId)
    {
        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);

        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();

        byte[] bytes = tex.EncodeToPNG();
        string filename = $"Preview_{threadId}_{System.DateTime.Now:yyyyMMdd_HHmmss}.png";
        string path = Path.Combine(Application.persistentDataPath, filename);

        File.WriteAllBytes(path, bytes);
        Debug.Log($"[Capture] 저장 완료: {path}");

        RenderTexture.active = null;
        Object.Destroy(tex);
        return path;
    }

    private Vector2Int GetRotatedGridSize(Vector2Int size, int rotation)
    {
        return (rotation % 2 != 0) ? new Vector2Int(size.y, size.x) : size;
    }

    private struct CameraBackup
    {
        public RenderTexture rt;
        public CameraClearFlags flags;
        public Color bg;
        public float size;
        public Vector3 pos;
        public int mask;

        public CameraBackup(Camera cam)
        {
            rt = cam.targetTexture;
            flags = cam.clearFlags;
            bg = cam.backgroundColor;
            size = cam.orthographicSize;
            pos = cam.transform.position;
            mask = cam.cullingMask;
        }
    }

    private void RestoreCamera(Camera cam, CameraBackup backup)
    {
        if (cam == null) return;
        cam.targetTexture = backup.rt;
        cam.clearFlags = backup.flags;
        cam.backgroundColor = backup.bg;
        cam.orthographicSize = backup.size;
        cam.transform.position = backup.pos;
        cam.cullingMask = backup.mask;
        RenderTexture.active = null;
    }
}