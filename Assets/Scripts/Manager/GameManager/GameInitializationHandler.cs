using UnityEngine;

namespace Pantory.Managers
{
    /// <summary>
    /// GameManager의 초기화 로직을 담당하는 핸들러
    /// </summary>
    public class GameInitializationHandler
    {
        private readonly MonoBehaviour _gameManager;
        private readonly GameObject _visualManagerPrefab;
        private readonly GameObject _gameDataManagerPrefab;
        private readonly bool _autoCreateVisualManager;
        private readonly bool _autoCreateGameDataManager;

        private VisualManager _visualManager;
        private GameDataManager _gameDataManager;

        public VisualManager VisualManager => _visualManager;
        public GameDataManager GameDataManager => _gameDataManager;

        public GameInitializationHandler(
            MonoBehaviour gameManager,
            GameObject visualManagerPrefab,
            GameObject gameDataManagerPrefab,
            bool autoCreateVisualManager = true,
            bool autoCreateGameDataManager = true)
        {
            _gameManager = gameManager;
            _visualManagerPrefab = visualManagerPrefab;
            _gameDataManagerPrefab = gameDataManagerPrefab;
            _autoCreateVisualManager = autoCreateVisualManager;
            _autoCreateGameDataManager = autoCreateGameDataManager;
        }

        /// <summary>
        /// 모든 매니저를 초기화합니다.
        /// </summary>
        public bool InitializeAll()
        {
            // VisualManager를 먼저 초기화
            if (!InitializeVisualManager())
            {
                Debug.LogError("[GameInitializationHandler] VisualManager initialization failed.");
                return false;
            }

            // GameDataManager를 초기화
            if (!InitializeGameDataManager())
            {
                Debug.LogError("[GameInitializationHandler] GameDataManager initialization failed.");
                return false;
            }

            // GameDataManager가 완전히 초기화되었는지 확인
            if (!IsGameDataManagerReady())
            {
                Debug.LogError("[GameInitializationHandler] GameDataManager is not fully initialized.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// VisualManager를 초기화합니다. 씬에 없으면 생성합니다.
        /// </summary>
        private bool InitializeVisualManager()
        {
            // 이미 인스턴스가 있으면 사용
            if (VisualManager.Instance != null)
            {
                _visualManager = VisualManager.Instance;
                Debug.Log("[GameInitializationHandler] Using existing VisualManager instance.");
                return true;
            }

            // 씬에서 VisualManager 찾기
            _visualManager = Object.FindFirstObjectByType<VisualManager>();
            
            if (_visualManager != null)
            {
                Debug.Log("[GameInitializationHandler] Found VisualManager in scene.");
                return true;
            }
            
            // VisualManager가 없으면 생성
            if (_autoCreateVisualManager)
            {
                // 프리팹이 있으면 프리팹에서 생성
                if (_visualManagerPrefab != null)
                {
                    GameObject vmObject = Object.Instantiate(_visualManagerPrefab);
                    vmObject.name = "VisualManager";
                    Object.DontDestroyOnLoad(vmObject);
                    _visualManager = vmObject.GetComponent<VisualManager>();
                    Debug.Log("[GameInitializationHandler] VisualManager created from prefab.");
                    return true;
                }
                else
                {
                    // 프리팹이 없으면 빈 GameObject에 컴포넌트 추가
                    GameObject vmObject = new GameObject("VisualManager");
                    Object.DontDestroyOnLoad(vmObject);
                    _visualManager = vmObject.AddComponent<VisualManager>();
                    Debug.LogWarning("[GameInitializationHandler] VisualManager created without prefab.");
                    return true;
                }
            }
            else
            {
                Debug.LogWarning("[GameInitializationHandler] VisualManager not found in scene and auto-create is disabled.");
                return false;
            }
        }

        /// <summary>
        /// GameDataManager를 초기화합니다. 씬에 없으면 생성합니다.
        /// </summary>
        private bool InitializeGameDataManager()
        {
            // 이미 인스턴스가 있으면 사용
            if (GameDataManager.Instance != null)
            {
                _gameDataManager = GameDataManager.Instance;
                Debug.Log("[GameInitializationHandler] Using existing GameDataManager instance.");
                return true;
            }

            // 씬에서 GameDataManager 찾기
            _gameDataManager = Object.FindFirstObjectByType<GameDataManager>();
            
            if (_gameDataManager != null)
            {
                Debug.Log("[GameInitializationHandler] Found GameDataManager in scene.");
                return true;
            }
            
            // GameDataManager가 없으면 생성
            if (_autoCreateGameDataManager)
            {
                // 프리팹이 있으면 프리팹에서 생성
                if (_gameDataManagerPrefab != null)
                {
                    GameObject gdmObject = Object.Instantiate(_gameDataManagerPrefab);
                    gdmObject.name = "GameDataManager";
                    Object.DontDestroyOnLoad(gdmObject);
                    _gameDataManager = gdmObject.GetComponent<GameDataManager>();
                    Debug.Log("[GameInitializationHandler] GameDataManager created from prefab.");
                    return true;
                }
                else
                {
                    // 프리팹이 없으면 빈 GameObject에 컴포넌트 추가
                    GameObject gdmObject = new GameObject("GameDataManager");
                    Object.DontDestroyOnLoad(gdmObject);
                    _gameDataManager = gdmObject.AddComponent<GameDataManager>();
                    Debug.LogWarning("[GameInitializationHandler] GameDataManager created without prefab. Initial data ScriptableObjects may not be set.");
                    return true;
                }
            }
            else
            {
                Debug.LogWarning("[GameInitializationHandler] GameDataManager not found in scene and auto-create is disabled.");
                return false;
            }
        }

        /// <summary>
        /// GameDataManager가 완전히 초기화되었는지 확인합니다.
        /// </summary>
        private bool IsGameDataManagerReady()
        {
            if (_gameDataManager == null)
                return false;

            // 모든 핵심 핸들러가 초기화되었는지 확인
            return _gameDataManager.Time != null &&
                   _gameDataManager.Thread != null &&
                   _gameDataManager.Resource != null &&
                   _gameDataManager.Market != null &&
                   _gameDataManager.Finances != null &&
                   _gameDataManager.Employee != null &&
                   _gameDataManager.Building != null;
        }
    }
}

