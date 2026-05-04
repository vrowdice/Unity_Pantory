using System.Collections.Generic;

/// <summary>
/// 플레이어 공통 런타임 상태(튜토리얼 진행 등). GameSaveData.tutorialAutoShowPending 과 동기화.
/// 튜토리얼: 키 = TutorialBase가 붙은 오브젝트 이름, 값 = true면 자동 표시 대기, false면 이미 한 번 끝까지 본 상태.
/// </summary>
public class PlayerDataHandler
{
    private readonly Dictionary<string, bool> _tutorialAutoShowPendingByOwnerName = new Dictionary<string, bool>();

    public bool ShouldAutoStartTutorialForOwner(string ownerGameObjectName)
    {
        if (string.IsNullOrEmpty(ownerGameObjectName))
        {
            return false;
        }

        if (!_tutorialAutoShowPendingByOwnerName.TryGetValue(ownerGameObjectName, out bool pending))
        {
            return true;
        }

        return pending;
    }

    public void MarkTutorialSequenceFinishedForOwner(string ownerGameObjectName)
    {
        if (string.IsNullOrEmpty(ownerGameObjectName))
        {
            return;
        }

        _tutorialAutoShowPendingByOwnerName[ownerGameObjectName] = false;
    }

    public void SetTutorialAutoShowPendingForOwner(string ownerGameObjectName, bool pending)
    {
        if (string.IsNullOrEmpty(ownerGameObjectName))
        {
            return;
        }

        _tutorialAutoShowPendingByOwnerName[ownerGameObjectName] = pending;
    }

    public void CaptureTo(GameSaveData saveData)
    {
        if (saveData == null)
        {
            return;
        }

        saveData.tutorialAutoShowPending.Clear();
        foreach (KeyValuePair<string, bool> kvp in _tutorialAutoShowPendingByOwnerName)
        {
            saveData.tutorialAutoShowPending.Add(new TutorialAutoShowPendingSaveData
            {
                ownerGameObjectName = kvp.Key,
                pendingAutoShow = kvp.Value
            });
        }
    }

    public void ApplyFromSave(GameSaveData saveData)
    {
        if (saveData == null)
        {
            return;
        }

        _tutorialAutoShowPendingByOwnerName.Clear();
        foreach (TutorialAutoShowPendingSaveData entry in saveData.tutorialAutoShowPending)
        {
            if (entry == null || string.IsNullOrEmpty(entry.ownerGameObjectName))
            {
                continue;
            }

            _tutorialAutoShowPendingByOwnerName[entry.ownerGameObjectName] = entry.pendingAutoShow;
        }
    }
}
