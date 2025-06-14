using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public abstract class ManagerBase : MonoBehaviour, IRxOwner, IRxCaller
{
    public virtual int InitializationPriority => 100;

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
    protected virtual void Awake()
    {
        OnManagerAwake();
    }

    protected virtual void OnDestroy()
    {
        OnManagerDestroy();
        Unload();
    }
    #endregion

    #region Manager Lifecycle Hooks
    /// <summary>
    /// 매니저가 깨어날 때 호출 (Awake 시점)
    /// </summary>
    protected virtual void OnManagerAwake()
    {
        // 하위 클래스에서 오버라이드
    }

    /// <summary>
    /// 씬이 로드될 때 호출
    /// GameManager에서 SceneManager.sceneLoaded 이벤트를 받아서 호출
    /// </summary>
    /// <param name="scene">로드된 씬</param>
    /// <param name="mode">로드 모드</param>
    public virtual void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 하위 클래스에서 오버라이드
    }

    /// <summary>
    /// 씬이 언로드될 때 호출
    /// GameManager에서 SceneManager.sceneUnloaded 이벤트를 받아서 호출
    /// </summary>
    /// <param name="scene">언로드된 씬</param>
    public virtual void OnSceneUnloaded(Scene scene)
    {
        // 하위 클래스에서 오버라이드
    }

    /// <summary>
    /// 활성 씬이 변경될 때 호출
    /// GameManager에서 SceneManager.activeSceneChanged 이벤트를 받아서 호출
    /// </summary>
    /// <param name="previousScene">이전 활성 씬</param>
    /// <param name="newScene">새로운 활성 씬</param>
    public virtual void OnActiveSceneChanged(Scene previousScene, Scene newScene)
    {
        // 하위 클래스에서 오버라이드
    }

    /// <summary>
    /// 매니저가 파괴될 때 호출 (OnDestroy 시점)
    /// </summary>
    protected virtual void OnManagerDestroy()
    {
        // 하위 클래스에서 오버라이드
    }

    /// <summary>
    /// 애플리케이션이 포커스를 잃을 때 호출
    /// GameManager에서 OnApplicationFocus 이벤트를 받아서 호출
    /// </summary>
    /// <param name="hasFocus">포커스 상태</param>
    public virtual void OnApplicationFocusChanged(bool hasFocus)
    {
        // 하위 클래스에서 오버라이드
    }

    /// <summary>
    /// 애플리케이션이 일시정지될 때 호출
    /// GameManager에서 OnApplicationPause 이벤트를 받아서 호출
    /// </summary>
    /// <param name="pauseStatus">일시정지 상태</param>
    public virtual void OnApplicationPauseChanged(bool pauseStatus)
    {
        // 하위 클래스에서 오버라이드
    }
    #endregion

    #region Debug Info
#if UNITY_EDITOR
    [Header("Debug Info")]
    [SerializeField, TextArea(2, 5)] private string debugInfo;

    protected virtual void OnValidate()
    {
        if (Application.isPlaying)
        {
            debugInfo = $"Priority: {InitializationPriority}\n" +
                       $"Tracked Rx: {trackedRxVars.Count}\n" +
                       $"Type: {GetType().Name}";
        }
    }
#endif
    #endregion
}