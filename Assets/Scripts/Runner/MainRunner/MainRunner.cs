using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 메인 씬의 건설(건물 배치) 러너.
/// </summary>
public class MainRunner : BuildingSceneRunnerBase
{
    [Header("UI")]
    [SerializeField] private MainCanvas _mainCanvas;

    public MainCanvas MainCanvas => _mainCanvas;
    public override IBuildSceneCanvas BuildSceneCanvas => _mainCanvas;

    protected override void InitBuildSceneCanvas()
    {
        _mainCanvas.Init(this);
    }
}
