using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 튜토리얼 포커스 대상 검색 및 UI/월드 구분.
/// </summary>
public static class TutorialFocusResolver
{
    public static GameObject FindFocusObject(string objectName, GameObject searchRoot = null)
    {
        if (string.IsNullOrWhiteSpace(objectName))
            return null;

        objectName = objectName.Trim();

        if (objectName.Contains("/"))
            return FindByHierarchyPath(objectName, searchRoot);

        GameObject found = GameObject.Find(objectName);
        if (found != null)
            return found;

        GameObject best = null;
        int bestScore = int.MinValue;
        CollectNameMatches(objectName, searchRoot, ref best, ref bestScore);

        if (objectName.EndsWith("(Clone)"))
        {
            string withoutClone = objectName.Substring(0, objectName.Length - "(Clone)".Length);
            CollectNameMatches(withoutClone, searchRoot, ref best, ref bestScore);
        }
        else
        {
            CollectNameMatches(objectName + "(Clone)", searchRoot, ref best, ref bestScore);
        }

        return best;
    }

    public static bool TryGetFocusTargets(
        GameObject focusGameObject,
        out RectTransform uiTarget,
        out Transform worldTarget)
    {
        uiTarget = null;
        worldTarget = null;

        if (focusGameObject == null)
            return false;

        if (TryGetUiFocusRectTransform(focusGameObject, out uiTarget))
            return true;

        worldTarget = focusGameObject.transform;
        return true;
    }

    public static bool IsUiFocusTarget(GameObject focusGameObject)
    {
        return TryGetUiFocusRectTransform(focusGameObject, out _);
    }

    private static bool TryGetUiFocusRectTransform(GameObject focusGameObject, out RectTransform uiTarget)
    {
        uiTarget = null;
        if (focusGameObject == null)
            return false;

        if (HasWorldMeshPresence(focusGameObject))
            return false;

        RectTransform onRoot = focusGameObject.GetComponent<RectTransform>();
        if (onRoot != null && IsUnderCanvas(onRoot))
        {
            uiTarget = onRoot;
            return true;
        }

        RectTransform[] rects = focusGameObject.GetComponentsInChildren<RectTransform>(true);
        RectTransform best = null;
        int bestScore = int.MinValue;
        for (int i = 0; i < rects.Length; i++)
        {
            RectTransform rect = rects[i];
            if (rect == null || !IsUnderCanvas(rect))
                continue;

            int score = ScoreUiRectCandidate(rect);
            if (score <= bestScore)
                continue;

            bestScore = score;
            best = rect;
        }

        if (best == null)
            return false;

        uiTarget = best;
        return true;
    }

    private static bool HasWorldMeshPresence(GameObject go)
    {
        if (go.GetComponent<Renderer>() != null || go.GetComponent<Collider>() != null)
            return true;

        Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || !renderer.enabled)
                continue;

            RectTransform rectOnSameObject = renderer.GetComponent<RectTransform>();
            if (rectOnSameObject != null && IsUnderCanvas(rectOnSameObject))
                continue;

            return true;
        }

        return false;
    }

    private static int ScoreUiRectCandidate(RectTransform rect)
    {
        int score = 0;
        if (rect.gameObject.activeInHierarchy)
            score += 100;

        if (rect.GetComponent<Graphic>() != null)
            score += 80;

        if (rect.GetComponentInParent<Canvas>() != null)
            score += 50;

        Rect worldRect = RectTransformUtils.GetWorldRect(rect);
        score += Mathf.RoundToInt(worldRect.width * worldRect.height * 0.01f);

        return score;
    }

    private static int ScoreNameMatchCandidate(GameObject go)
    {
        int score = 0;
        if (go.activeInHierarchy)
            score += 200;

        if (IsUnderCanvas(go.transform))
            score += 500;

        if (go.GetComponent<RectTransform>() != null)
            score += 100;

        if (go.GetComponent<Graphic>() != null)
            score += 80;

        return score;
    }

    private static void CollectNameMatches(
        string objectName,
        GameObject searchRoot,
        ref GameObject best,
        ref int bestScore)
    {
        if (searchRoot != null)
            ScoreTransform(searchRoot.transform, objectName, ref best, ref bestScore);

        Transform[] transforms = Object.FindObjectsByType<Transform>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform t = transforms[i];
            if (t == null || t.gameObject.name != objectName)
                continue;

            if (!t.gameObject.scene.IsValid() || !t.gameObject.scene.isLoaded)
                continue;

            ScoreGameObject(t.gameObject, ref best, ref bestScore);
        }
    }

    private static void ScoreTransform(Transform root, string objectName, ref GameObject best, ref int bestScore)
    {
        if (root == null)
            return;

        if (root.name == objectName)
            ScoreGameObject(root.gameObject, ref best, ref bestScore);

        for (int i = 0; i < root.childCount; i++)
            ScoreTransform(root.GetChild(i), objectName, ref best, ref bestScore);
    }

    private static void ScoreGameObject(GameObject candidate, ref GameObject best, ref int bestScore)
    {
        int score = ScoreNameMatchCandidate(candidate);
        if (score <= bestScore)
            return;

        bestScore = score;
        best = candidate;
    }

    private static GameObject FindByHierarchyPath(string path, GameObject searchRoot)
    {
        string[] parts = path.Split('/');
        if (parts.Length == 0)
            return null;

        Transform current = null;
        if (searchRoot != null && searchRoot.name == parts[0])
            current = searchRoot.transform;
        else
        {
            GameObject root = GameObject.Find(parts[0]);
            current = root != null ? root.transform : null;
        }

        if (current == null)
            return null;

        for (int i = 1; i < parts.Length; i++)
        {
            current = current.Find(parts[i]);
            if (current == null)
                return null;
        }

        return current.gameObject;
    }

    private static bool IsUnderCanvas(Transform t)
    {
        return t != null && t.GetComponentInParent<Canvas>() != null;
    }
}
