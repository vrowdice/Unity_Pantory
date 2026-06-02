using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 엔트리 리스트 패널에서 기존 버튼의 동적 UI만 일괄 갱신할 때 사용합니다.
/// </summary>
public static class EntryListPanelUtils
{
    public static void RefreshAll<TBtn>(IEnumerable<TBtn> buttons) where TBtn : IEntryListBtn
    {
        if (buttons == null)
            return;

        foreach (TBtn btn in buttons)
            btn?.Refresh();
    }

    public static void RefreshAll<TKey, TBtn>(Dictionary<TKey, TBtn> buttonsByKey) where TBtn : IEntryListBtn
    {
        if (buttonsByKey == null)
            return;

        foreach (TBtn btn in buttonsByKey.Values)
            btn?.Refresh();
    }

    public static void RefreshAllChildren(Transform content)
    {
        if (content == null)
            return;

        foreach (Transform child in content)
        {
            if (child.TryGetComponent(out IEntryListBtn btn))
                btn.Refresh();
        }
    }
}
