using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public partial class GameManager : Singleton<GameManager>, IRxOwner, IRxCaller
{
    [Header("Core Manager References")]
    [SerializeField] private UIManager uiManager;
    [SerializeField] private WorldManager worldManager;

    private readonly List<ManagerBase> allManagers = new();
    private bool isInitialized = false;
    private bool isProjectInitialized = false;

    public static UIManager UI => Instance?.uiManager;
    public static WorldManager World => Instance?.worldManager;
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

        var uiMgr = FindObjectOfType<UIManager>(true);
        var worldMgr = FindObjectOfType<WorldManager>(true);

        if (uiMgr != null && !allManagers.Contains(uiMgr))
            allManagers.Add(uiMgr);

        if (worldMgr != null && !allManagers.Contains(worldMgr))
            allManagers.Add(worldMgr);

        // 1단계: 매니저 참조 먼저 설정
        InitializeProject();

        // 2단계: 우선순위 기반으로 Awake 호출
        var sortedManagers = allManagers
            .OrderBy(m => m.InitializationPriority)
            .ThenBy(m => m.name)
            .ToList();

        foreach (var manager in sortedManagers)
        {
            // 이미 Awake가 호출된 매니저들은 OnManagerAwake만 다시 호출
            if (manager.gameObject.activeInHierarchy)
            {
                manager.CallOnManagerAwake(); // 새로운 public 메서드 필요
            }

            Debug.Log($"[GameManager] Initialized: {manager.GetType().Name} (Priority: {manager.InitializationPriority})");
        }

        uiManager = uiMgr;
        worldManager = worldMgr;

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

        // 참조 설정 완료 플래그
        isProjectInitialized = true; // 이제 접근 가능
        Debug.Log("[GameManager] Project initialization completed - managers ready");
    }

    partial void ShutdownProject()
    {
        isProjectInitialized = false;
    }
}