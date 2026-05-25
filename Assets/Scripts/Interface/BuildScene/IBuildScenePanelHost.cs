using UnityEngine;

/// <summary>
/// MainCanvas / TutorialCanvas 공통 패널 호스트 계약.
/// </summary>
public interface IBuildScenePanelHost
{
    GameManager GameManager { get; }
    DataManager DataManager { get; }
    UIManager UIManager { get; }
    SoundManager SoundManager { get; }
    VisualManager VisualManager { get; }
    Transform CanvasTrans { get; }

    void OpenPanel(MainPanelType panelType);
    void CloseAllPanels();
}
