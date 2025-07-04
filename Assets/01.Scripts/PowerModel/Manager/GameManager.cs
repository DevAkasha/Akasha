using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Akasha.Modifier;

namespace Akasha
{
    public partial class GameManager : Singleton<GameManager>, IRxOwner, IRxCaller
    {
        [Header("Core Manager References")]
        [SerializeField] private ControllerManager controllerManager;
        [SerializeField] private ModelControllerManager modelControllerManager;
        [SerializeField] private PresenterManager presenterManager;

        private readonly List<ManagerBase> allManagers = new();
        private bool isInitialized = false;
        private bool isProjectInitialized = false;

        public static ControllerManager Controllers => Instance?.controllerManager;
        public static ModelControllerManager ModelControllers => Instance?.modelControllerManager;
        public static PresenterManager Presenters => Instance?.presenterManager;

        public bool IsProjectInitialized => isProjectInitialized;

        public bool IsRxVarOwner => true;
        public bool IsRxAllOwner => false;
        public bool IsLogicalCaller => true;
        public bool IsMultiRolesCaller => true;
        public bool IsFunctionalCaller => true;

        private readonly HashSet<RxBase> trackedRxVars = new();

        public void RegisterRx(RxBase rx)
        {
            trackedRxVars.Add(rx);
        }

        public void Unload()
        {
            foreach (var rx in trackedRxVars)
            {
                rx.ClearRelation();
            }
            trackedRxVars.Clear();
        }

        protected override void Awake()
        {
            base.Awake();

            if (Instance == this)
            {
                RegisterSceneEvents();
                InitializeManagers();
                InitializeProject();
            }
        }

        protected virtual void OnDestroy()
        {
            if (Instance == this)
            {
                UnregisterSceneEvents();
                ShutdownProject();
                ShutdownManagers();
                Unload();
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (Instance == this)
            {
                NotifyAllManagers(manager => manager.OnApplicationFocusChanged(hasFocus));
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (Instance == this)
            {
                NotifyAllManagers(manager => manager.OnApplicationPauseChanged(pauseStatus));
            }
        }

        private void InitializeManagers()
        {
            if (isInitialized) return;

            var managers = FindObjectsOfType<ManagerBase>(true);
            allManagers.AddRange(managers);

            var controllerMgr = FindObjectOfType<ControllerManager>(true);
            var modelControllerMgr = FindObjectOfType<ModelControllerManager>(true);
            var presenterMgr = FindObjectOfType<PresenterManager>(true);

            if (controllerMgr != null && !allManagers.Contains(controllerMgr))
                allManagers.Add(controllerMgr);

            if (modelControllerMgr != null && !allManagers.Contains(modelControllerMgr))
                allManagers.Add(modelControllerMgr);

            if (presenterMgr != null && !allManagers.Contains(presenterMgr))
                allManagers.Add(presenterMgr);

            InitializeProject();

            var sortedManagers = allManagers
                .OrderBy(m => m.InitializationPriority)
                .ThenBy(m => m.name)
                .ToList();

            foreach (var manager in sortedManagers)
            {
                if (manager.gameObject.activeInHierarchy)
                {
                    manager.CallOnManagerAwake();
                }

                Debug.Log($"[GameManager] Initialized: {manager.GetType().Name} (Priority: {manager.InitializationPriority})");
            }

            controllerManager = controllerMgr;
            modelControllerManager = modelControllerMgr;
            presenterManager = presenterMgr;

            isInitialized = true;
        }

        private void ShutdownManagers()
        {
            var reversedManagers = allManagers
                .OrderByDescending(m => m.InitializationPriority)
                .ToList();

            foreach (var manager in reversedManagers)
            {
                if (manager is IRxOwner rxOwner)
                {
                    rxOwner.Unload();
                }
            }

            allManagers.Clear();
            isInitialized = false;
        }

        public T GetManager<T>() where T : class
        {
            return allManagers.FirstOrDefault(m => m is T) as T;
        }

        public T GetController<T>() where T : BaseController
        {
            return Controllers?.GetController<T>();
        }

        public T GetModelController<T>() where T : MController
        {
            return ModelControllers?.GetModelController<T>();
        }

        public T GetPresenter<T>() where T : BasePresenter
        {
            return Presenters?.GetPresenter<T>();
        }

        public void SaveAllModels()
        {
            ModelControllers?.SaveAllModels();
            Debug.Log("[GameManager] Saved all models");
        }

        public void LoadAllModels()
        {
            ModelControllers?.LoadAllModels();
            Debug.Log("[GameManager] Loaded all models");
        }

        public void ShowAllPresenters()
        {
            Presenters?.ShowAll();
            Debug.Log("[GameManager] Showed all presenters");
        }

        public void HideAllPresenters()
        {
            Presenters?.HideAll();
            Debug.Log("[GameManager] Hid all presenters");
        }

        public void InitializeAllControllers()
        {
            Controllers?.InitializeAll();
            ModelControllers?.InitializeAll();
            Debug.Log("[GameManager] Initialized all controllers");
        }

        public void DeinitializeAllControllers()
        {
            Controllers?.DeinitializeAll();
            ModelControllers?.DeinitializeAll();
            Debug.Log("[GameManager] Deinitialized all controllers");
        }

        public void InitializeAllPresenters()
        {
            Presenters?.InitializeAll();
            Debug.Log("[GameManager] Initialized all presenters");
        }

        public void DeinitializeAllPresenters()
        {
            Presenters?.DeinitializeAll();
            Debug.Log("[GameManager] Deinitialized all presenters");
        }

        private void RegisterSceneEvents()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
            SceneManager.activeSceneChanged += OnActiveSceneChanged;
        }

        private void UnregisterSceneEvents()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"[GameManager] Scene loaded: {scene.name} (Mode: {mode})");
            NotifyAllManagers(manager => manager.OnSceneLoaded(scene, mode));
        }

        private void OnSceneUnloaded(Scene scene)
        {
            Debug.Log($"[GameManager] Scene unloaded: {scene.name}");
            NotifyAllManagers(manager => manager.OnSceneUnloaded(scene));
        }

        private void OnActiveSceneChanged(Scene previousScene, Scene newScene)
        {
            Debug.Log($"[GameManager] Active scene changed: {previousScene.name} → {newScene.name}");
            NotifyAllManagers(manager => manager.OnActiveSceneChanged(previousScene, newScene));
        }

        private void NotifyAllManagers(System.Action<ManagerBase> action)
        {
            foreach (var manager in allManagers)
            {
                if (manager != null)
                {
                    try
                    {
                        action(manager);
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"[GameManager] Error notifying {manager.GetType().Name}: {ex.Message}");
                    }
                }
            }
        }

        public Dictionary<string, int> GetSystemStatistics()
        {
            var stats = new Dictionary<string, int>();

            if (Controllers != null)
            {
                stats["Controllers"] = Controllers.RegisteredCount;
                stats["Pooled Controllers"] = Controllers.PooledCount;
            }

            if (ModelControllers != null)
            {
                stats["Model Controllers"] = ModelControllers.RegisteredCount;
                stats["Pooled Model Controllers"] = ModelControllers.PooledCount;
            }

            if (Presenters != null)
            {
                stats["Presenters"] = Presenters.RegisteredCount;
                stats["Pooled Presenters"] = Presenters.PooledCount;
                stats["Total Views"] = Presenters.GetTotalViewCount();
            }

            stats["Total Managers"] = allManagers.Count;
            stats["Tracked RxVars"] = trackedRxVars.Count;

            return stats;
        }

        public void PrintSystemStatus()
        {
            var stats = GetSystemStatistics();
            var statusText = string.Join("\n", stats.Select(kvp => $"  {kvp.Key}: {kvp.Value}"));

            Debug.Log($"[GameManager] System Status:\n{statusText}");
        }

        partial void InitializeProject();
        partial void ShutdownProject();
    }

    public partial class GameManager
    {
        [Header("Project Managers")]
        [SerializeField] private EffectManager effectManager;
        [SerializeField] private EffectRunner effectRunner;
        [SerializeField] private ModifierManager modifierManager;

        public static EffectManager Effect => Instance?.effectManager;
        public static EffectRunner EffectRunner => Instance?.effectRunner;
        public static ModifierManager Modifier => Instance?.modifierManager;

        partial void InitializeProject()
        {
            effectManager = GetManager<EffectManager>();
            effectRunner = GetManager<EffectRunner>();
            modifierManager = GetManager<ModifierManager>();

            if (modifierManager == null)
            {
                Debug.LogError("[GameManager] ModifierManager not found! Please add ModifierManager component to the scene.");
            }
            else
            {
                Debug.Log("[GameManager] ModifierManager successfully initialized");
            }

            if (effectManager == null)
            {
                Debug.LogError("[GameManager] EffectManager not found! Please add EffectManager component to the scene.");
            }
            else
            {
                Debug.Log("[GameManager] EffectManager successfully initialized");
            }

            if (effectRunner == null)
            {
                Debug.LogError("[GameManager] EffectRunner not found! Please add EffectRunner component to the scene.");
            }
            else
            {
                Debug.Log("[GameManager] EffectRunner successfully initialized");
            }

            isProjectInitialized = true;
            Debug.Log("[GameManager] Project initialization completed - all managers ready");
        }

        partial void ShutdownProject()
        {
            isProjectInitialized = false;
            Debug.Log("[GameManager] Project shutdown completed");
        }
    }
}