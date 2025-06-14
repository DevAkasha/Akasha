using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// GameManager의 프레임워크 핵심 부분 - 기본 매니저 시스템만
/// </summary>
public partial class GameManager : Singleton<GameManager>, IRxOwner, IRxCaller
{
    #region Framework Core Fields
    [Header("Core Manager References")]
    [SerializeField] private UIManager uiManager;
    [SerializeField] private WorldManager worldManager;

    // 모든 매니저들을 관리하는 리스트
    private readonly List<ManagerBase> allManagers = new();
    private bool isInitialized = false;
    #endregion

    #region Static Properties for Easy Access
    public static UIManager UI => Instance?.uiManager;
    public static WorldManager World => Instance?.worldManager;
    #endregion

    #region IRxOwner, IRxCaller Implementation
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
    #endregion

    #region Unity Lifecycle
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
    #endregion

    #region Manager System
    private void InitializeManagers()
    {
        if (isInitialized) return;

        // 1. 모든 매니저들 찾기
        var managers = FindObjectsOfType<ManagerBase>(true);
        allManagers.AddRange(managers);

        // 2. UIManager, WorldManager 찾기
        var uiMgr = FindObjectOfType<UIManager>(true);
        var worldMgr = FindObjectOfType<WorldManager>(true);

        if (uiMgr != null && !allManagers.Contains(uiMgr))
            allManagers.Add(uiMgr);

        if (worldMgr != null && !allManagers.Contains(worldMgr))
            allManagers.Add(worldMgr);

        // 3. 우선도 기반으로 정렬 후 초기화
        var sortedManagers = allManagers
            .OrderBy(m => m.InitializationPriority)
            .ThenBy(m => m.name)
            .ToList();

        foreach (var manager in sortedManagers)
        {
            Debug.Log($"[GameManager] Initialized: {manager.GetType().Name} (Priority: {manager.InitializationPriority})");
        }

        // 4. 참조 설정
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
    #endregion

    #region Public API
    public T GetManager<T>() where T : class
    {
        return allManagers.FirstOrDefault(m => m is T) as T;
    }
    #endregion

    #region Scene Event Management
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
    #endregion

    #region Partial Methods for Project Extension
    partial void InitializeProject();
    partial void ShutdownProject();
    #endregion
}
public partial class GameManager
{
    #region Project Specific Fields
    [Header("Project Managers")]
    [SerializeField] private EffectManager effectManager;
    [SerializeField] private EffectRunner effectRunner;
    // 추가 매니저들...
    #endregion

    #region Project Static Properties
    public static EffectManager Effect => Instance?.effectManager;
    public static EffectRunner EffectRunner => Instance?.effectRunner;
    // 추가 정적 접근자들...
    #endregion

    #region Project Implementation
    partial void InitializeProject()
    {
        // 프로젝트별 매니저 참조 설정
        effectManager = GetManager<EffectManager>();
        effectRunner = GetManager<EffectRunner>();

        // 게임별 초기화 로직...
    }

    partial void ShutdownProject()
    {
        // 게임별 종료 로직...
    }
    #endregion
}