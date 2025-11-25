using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pantory.Managers
{
    public class GameUiPanelHandler
    {
        private IUIManager _uiManager;
        private GameDataManager _gameDataManager;

        private readonly GameObject _warningPanelPrefab;
        private readonly GameObject _enterNamePanelPrefab;
        private readonly GameObject _selectResourcePanelPrefab;
        private readonly GameObject _manageThreadPanelPrefab;
        private readonly GameObject _manageThreadCategoryPanelPrefab;

        public GameUiPanelHandler(
            IUIManager uiManager,
            GameDataManager gameDataManager,
            GameObject warningPanelPrefab,
            GameObject enterNamePanelPrefab,
            GameObject selectResourcePanelPrefab,
            GameObject manageThreadPanelPrefab,
            GameObject manageThreadCategoryPanelPrefab)
        {
            _warningPanelPrefab = warningPanelPrefab;
            _enterNamePanelPrefab = enterNamePanelPrefab;
            _selectResourcePanelPrefab = selectResourcePanelPrefab;
            _manageThreadPanelPrefab = manageThreadPanelPrefab;
            _manageThreadCategoryPanelPrefab = manageThreadCategoryPanelPrefab;
            
            UpdateReferences(uiManager, gameDataManager);
        }

        public void UpdateReferences(IUIManager uiManager, GameDataManager gameDataManager)
        {
            _uiManager = uiManager;
            _gameDataManager = gameDataManager;
        }

        private Transform CanvasTransform => _uiManager?.CanvasTrans;

        public void ShowWarningPanel()
        {
            if (!ValidateCanvasAndPrefab(_warningPanelPrefab, "[GameUiPanelHandler] Warning panel prefab is not assigned."))
                return;

            UnityEngine.Object.Instantiate(_warningPanelPrefab, CanvasTransform);
            Debug.Log("[GameUiPanelHandler] Warning panel displayed.");
        }

        public void ShowWarningPanel(string message)
        {
            if (!ValidateCanvasAndPrefab(_warningPanelPrefab, "[GameUiPanelHandler] Warning panel prefab is not assigned."))
                return;

            GameObject warningPanel = UnityEngine.Object.Instantiate(_warningPanelPrefab, CanvasTransform);

            if (warningPanel.TryGetComponent(out WarningPanel warningPanelComponent))
            {
                warningPanelComponent.SetMessage(message);
                Debug.Log($"[GameUiPanelHandler] Warning panel displayed with message: {message}");
            }
            else
            {
                Debug.LogWarning("[GameUiPanelHandler] WarningPanel component not found on instantiated prefab.");
            }
        }

        public SelectResourcePanel ShowSelectResourcePanel(List<ResourceType> resourceTypes, Action<ResourceEntry> onResourceSelected, List<ResourceData> producibleResources = null)
        {
            if (!ValidateCanvasAndPrefab(_selectResourcePanelPrefab, "[GameUiPanelHandler] Select resource panel prefab is not assigned."))
                return null;

            if (_gameDataManager == null)
            {
                Debug.LogWarning("[GameUiPanelHandler] GameDataManager reference is null.");
                return null;
            }

            GameObject selectResourcePanel = UnityEngine.Object.Instantiate(_selectResourcePanelPrefab, CanvasTransform);
            if (selectResourcePanel.TryGetComponent(out SelectResourcePanel panelComponent))
            {
                panelComponent.OnInitialize(_gameDataManager, resourceTypes, onResourceSelected, producibleResources);
                Debug.Log("[GameUiPanelHandler] Select resource panel displayed.");
                return panelComponent;
            }

            Debug.LogWarning("[GameUiPanelHandler] SelectResourcePanel component not found on instantiated prefab.");
            return null;
        }

        public ManageThreadCartegoryPanel ShowManageThreadCategoryPanel(GameDataManager dataManager, Action<string> onCategorySelected)
        {
            if (!ValidateCanvasAndPrefab(_manageThreadCategoryPanelPrefab, "[GameUiPanelHandler] ManageThreadCartegoryPanel prefab is not assigned."))
                return null;

            GameDataManager targetDataManager = dataManager ?? _gameDataManager;
            if (targetDataManager == null)
            {
                Debug.LogWarning("[GameUiPanelHandler] GameDataManager reference is null.");
                return null;
            }

            GameObject panel = UnityEngine.Object.Instantiate(_manageThreadCategoryPanelPrefab, CanvasTransform);
            if (panel.TryGetComponent(out ManageThreadCartegoryPanel panelComponent))
            {
                panelComponent.OnInitialize(targetDataManager, onCategorySelected);
                Debug.Log("[GameUiPanelHandler] ManageThreadCartegoryPanel displayed.");
                return panelComponent;
            }

            Debug.LogWarning("[GameUiPanelHandler] ManageThreadCartegoryPanel component not found on instantiated prefab.");
            return null;
        }

        public ManageThreadPanel ShowManageThreadPanel(Action<string> onThreadSelected)
        {
            if (!ValidateCanvasAndPrefab(_manageThreadPanelPrefab, "[GameUiPanelHandler] ManageThreadPanel prefab is not assigned."))
                return null;

            if (_gameDataManager == null)
            {
                Debug.LogWarning("[GameUiPanelHandler] GameDataManager reference is null.");
                return null;
            }

            GameObject panel = UnityEngine.Object.Instantiate(_manageThreadPanelPrefab, CanvasTransform);
            if (panel.TryGetComponent(out ManageThreadPanel panelComponent))
            {
                panelComponent.OnInitialize(_gameDataManager, onThreadSelected);
                Debug.Log("[GameUiPanelHandler] ManageThreadPanel displayed.");
                return panelComponent;
            }

            Debug.LogWarning("[GameUiPanelHandler] ManageThreadPanel component not found on instantiated prefab.");
            return null;
        }

        public EnterNamePanel ShowEnterNamePanel(Action<string> onConfirm)
        {
            if (!ValidateCanvasAndPrefab(_enterNamePanelPrefab, "[GameUiPanelHandler] EnterNamePanel prefab is not assigned."))
                return null;

            GameObject panel = UnityEngine.Object.Instantiate(_enterNamePanelPrefab, CanvasTransform);
            if (panel.TryGetComponent(out EnterNamePanel panelComponent))
            {
                panelComponent.OnInitialize(onConfirm);
                Debug.Log("[GameUiPanelHandler] EnterNamePanel displayed.");
                return panelComponent;
            }

            Debug.LogWarning("[GameUiPanelHandler] EnterNamePanel component not found on instantiated prefab.");
            return null;
        }

        private bool ValidateCanvasAndPrefab(GameObject prefab, string prefabWarning)
        {
            if (prefab == null)
            {
                Debug.LogWarning(prefabWarning);
                return false;
            }

            if (CanvasTransform == null)
            {
                Debug.LogWarning("[GameUiPanelHandler] Canvas transform is null.");
                return false;
            }

            return true;
        }
    }
}

