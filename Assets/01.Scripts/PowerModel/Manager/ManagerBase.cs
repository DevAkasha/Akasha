using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public abstract class ManagerBase : MonoBehaviour, IRxOwner, IRxCaller
{
    public virtual int InitializationPriority => 100;

    private bool managerAwakeCalled = false;

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

    protected virtual void Awake()
    {
        // GameManager가 초기화되기 전까지 대기
        if (GameManager.Instance == null || !GameManager.Instance.IsProjectInitialized)
        {
            return;
        }

        CallOnManagerAwake();
    }

    public void CallOnManagerAwake()
    {
        if (!managerAwakeCalled)
        {
            OnManagerAwake();
            managerAwakeCalled = true;
        }
    }
    protected virtual void OnDestroy()
    {
        OnManagerDestroy();
        Unload();
    }

    protected virtual void OnManagerAwake()
    {
        // 하위 클래스에서 오버라이드
    }

    public virtual void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 하위 클래스에서 오버라이드
    }

  
    public virtual void OnSceneUnloaded(Scene scene)
    {
        // 하위 클래스에서 오버라이드
    }

    public virtual void OnActiveSceneChanged(Scene previousScene, Scene newScene)
    {
        // 하위 클래스에서 오버라이드
    }

    protected virtual void OnManagerDestroy()
    {
        // 하위 클래스에서 오버라이드
    }

 
    public virtual void OnApplicationFocusChanged(bool hasFocus)
    {
        // 하위 클래스에서 오버라이드
    }

   
    public virtual void OnApplicationPauseChanged(bool pauseStatus)
    {
        // 하위 클래스에서 오버라이드
    }


#if UNITY_EDITOR
    [Header("Debug Info")]
    [SerializeField, TextArea(2, 5)] protected string debugInfo;  // private -> protected

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

}