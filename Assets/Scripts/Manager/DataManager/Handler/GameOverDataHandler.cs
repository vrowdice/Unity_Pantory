using System;
using UnityEngine;

/// <summary>
/// 파산·20년 기한·기업 1등 달성 등 게임 오버 조건을 판정하고 UI를 띄웁니다.
/// </summary>
public class GameOverDataHandler : IDataHandlerEvents, IMonthChangeHandler, IGameSaveHandler
{
    private readonly DataManager _dataManager;

    private GameOverType _currentGameOverType = GameOverType.None;
    private bool _isGameOver;
    private bool _yearLimitResolved;
    private bool _companyRankFirstCelebrated;

    public event Action<GameOverType> OnGameOverTriggered;

    public bool IsGameOver => _isGameOver;
    public GameOverType CurrentGameOverType => _currentGameOverType;
    public bool CanContinueAfterGameOver => _currentGameOverType == GameOverType.CompanyRankFirst;

    public GameOverDataHandler(DataManager dataManager)
    {
        _dataManager = dataManager;
    }

    public void HandleYearChanged()
    {
        if (_isGameOver || _yearLimitResolved || !IsGameYearLimitReached())
        {
            return;
        }

        _yearLimitResolved = true;

        if (IsPlayerTopCompany())
        {
            if (!_companyRankFirstCelebrated)
            {
                TriggerGameOver(GameOverType.CompanyRankFirst);
            }
        }
        else
        {
            TriggerGameOver(GameOverType.GovernmentDissolution);
        }
    }

    public void HandleMonthChanged()
    {
        if (_isGameOver || _companyRankFirstCelebrated || IsGameYearLimitReached())
        {
            return;
        }

        if (!IsPlayerTopCompany())
        {
            return;
        }

        TriggerGameOver(GameOverType.CompanyRankFirst);
    }

    public void TriggerGameOver(GameOverType type)
    {
        if (type == GameOverType.None || _isGameOver)
        {
            return;
        }

        _isGameOver = true;
        _currentGameOverType = type;

        if (type == GameOverType.CompanyRankFirst)
        {
            _companyRankFirstCelebrated = true;
        }

        _dataManager.Time?.PauseTime();
        UIManager.Instance?.ShowGameOverPopup(type);
        OnGameOverTriggered?.Invoke(type);
    }

    public void ClearGameOverForContinue()
    {
        if (_currentGameOverType != GameOverType.CompanyRankFirst)
        {
            return;
        }

        _isGameOver = false;
        _currentGameOverType = GameOverType.None;
        _dataManager.Time?.ResumeTime();
    }

    public void ResetForTitleReturn()
    {
        _isGameOver = false;
        _currentGameOverType = GameOverType.None;
        _yearLimitResolved = false;
        _companyRankFirstCelebrated = false;
        _dataManager.Time?.ResumeTime();
    }

    public void NotifyGameOverUiRestored()
    {
        if (!_isGameOver || _currentGameOverType == GameOverType.None)
        {
            return;
        }

        OnGameOverTriggered?.Invoke(_currentGameOverType);
        UIManager.Instance?.ShowGameOverPopup(_currentGameOverType);
    }

    public bool IsPlayerTopCompany()
    {
        if (_dataManager?.Finances == null || _dataManager.MarketActor == null)
        {
            return false;
        }

        return _dataManager.MarketActor.IsPlayerWealthRankFirstAmongCompanies(_dataManager.Finances.Wealth);
    }

    private bool IsGameYearLimitReached()
    {
        InitialTimeData settings = _dataManager?.InitialTimeData;
        if (settings == null || settings.gameYearLimit <= 0 || _dataManager.Time == null)
        {
            return false;
        }

        return _dataManager.Time.Year >= settings.startYear + settings.gameYearLimit;
    }

    public void ClearAllSubscriptions()
    {
        OnGameOverTriggered = null;
    }

    public void CaptureTo(GameSaveData saveData)
    {
        if (saveData == null)
        {
            return;
        }

        saveData.gameOverType = (int)_currentGameOverType;
        saveData.isGameOver = _isGameOver;
        saveData.yearLimitResolved = _yearLimitResolved;
        saveData.companyRankFirstCelebrated = _companyRankFirstCelebrated;
    }

    public void ApplyFromSave(GameSaveData saveData)
    {
        if (saveData == null)
        {
            return;
        }

        if (saveData.isBankruptcyGameOver && saveData.gameOverType == (int)GameOverType.None)
        {
            saveData.gameOverType = (int)GameOverType.Bankruptcy;
            saveData.isGameOver = true;
        }

        _currentGameOverType = (GameOverType)saveData.gameOverType;
        _isGameOver = saveData.isGameOver;
        _yearLimitResolved = saveData.yearLimitResolved;
        _companyRankFirstCelebrated = saveData.companyRankFirstCelebrated;

        if (_isGameOver)
        {
            _dataManager.Time?.PauseTime();
        }
    }
}
