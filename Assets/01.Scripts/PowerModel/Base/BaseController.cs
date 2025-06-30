using System.Collections.Generic;
using UnityEngine;

public abstract class Controller : AggregateRoot
{
    protected override void OnInitialize()
    {
        base.OnInitialize();
        OnControllerInitialize();
    }

    protected override void OnDeinitialize()
    {
        OnControllerDeinitialize();
        base.OnDeinitialize();
    }

    protected virtual void OnControllerInitialize() { }
    protected virtual void OnControllerDeinitialize() { }
}

public abstract class MController : Controller, IModelOwner
{
    public abstract BaseModel GetBaseModel();
}

public abstract class BaseController : Controller, IRxCaller, IRxOwner
{
    [Header("Controller Settings")]
    [SerializeField] protected bool enableDebugLogs = false;

    public bool IsLogicalCaller => true;
    public bool IsMultiRolesCaller => true;
    public bool IsFunctionalCaller => false;
    public bool IsRxVarOwner => true;
    public bool IsRxAllOwner => false;

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

    protected override void OnControllerInitialize()
    {
        base.OnControllerInitialize();
        LogDebug($"Controller {GetType().Name} initialized");
    }

    protected override void OnControllerDeinitialize()
    {
        Unload();
        LogDebug($"Controller {GetType().Name} deinitialized");
        base.OnControllerDeinitialize();
    }

    protected void LogDebug(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[{GetAggregateId()}] {message}");
        }
    }

    protected void LogWarning(string message)
    {
        Debug.LogWarning($"[{GetAggregateId()}] {message}");
    }

    protected void LogError(string message)
    {
        Debug.LogError($"[{GetAggregateId()}] {message}");
    }
}

public abstract class BaseController<M> : MController, IRxCaller, IModelOwner<M>
    where M : BaseModel
{
    [Header("Model Controller Settings")]
    [SerializeField] protected bool enableDebugLogs = false;

    public M Model { get; set; }

    public bool IsLogicalCaller => true;
    public bool IsMultiRolesCaller => true;
    public bool IsFunctionalCaller => false;

    public override BaseModel GetBaseModel() => Model;
    public M GetModel() => Model;

    protected override void OnControllerInitialize()
    {
        SetupModel();
        base.OnControllerInitialize();
        LogDebug($"Model Controller {GetType().Name} initialized with model {typeof(M).Name}");
    }

    protected override void OnControllerDeinitialize()
    {
        Model?.Unload();
        LogDebug($"Model Controller {GetType().Name} deinitialized");
        base.OnControllerDeinitialize();
    }

    protected abstract void SetupModel();

    protected void LogDebug(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[{GetAggregateId()}] {message}");
        }
    }

    protected void LogWarning(string message)
    {
        Debug.LogWarning($"[{GetAggregateId()}] {message}");
    }

    protected void LogError(string message)
    {
        Debug.LogError($"[{GetAggregateId()}] {message}");
    }
}

public abstract class BaseController<E, M> : MController, IRxCaller
    where E : BaseEntity<M> where M : BaseModel
{
    [Header("Entity Controller Settings")]
    [SerializeField] protected bool enableDebugLogs = false;
    [SerializeField] private E entity;

    public E Entity => entity;
    public M Model => entity?.Model;

    bool IRxCaller.IsLogicalCaller => true;
    bool IRxCaller.IsMultiRolesCaller => false;
    bool IRxCaller.IsFunctionalCaller => false;

    public override BaseModel GetBaseModel() => Model;
    public M GetModel() => Model;

    protected override void Awake()
    {
        if (entity == null)
            entity = GetComponentInChildren<E>();

        base.Awake();
    }

    protected override void OnControllerInitialize()
    {
        entity?.CallInit();
        base.OnControllerInitialize();
        LogDebug($"Entity Controller {GetType().Name} initialized with entity {typeof(E).Name}");
    }

    protected override void OnControllerDeinitialize()
    {
        entity?.CallDeinit();
        Model?.Unload();
        LogDebug($"Entity Controller {GetType().Name} deinitialized");
        base.OnControllerDeinitialize();
    }

    protected void OnDisable()
    {
        entity?.CallDisable();
    }

    protected override void OnDestroy()
    {
        entity?.CallDestroy();
        base.OnDestroy();
    }

    protected void LogDebug(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[{GetAggregateId()}] {message}");
        }
    }

    protected void LogWarning(string message)
    {
        Debug.LogWarning($"[{GetAggregateId()}] {message}");
    }

    protected void LogError(string message)
    {
        Debug.LogError($"[{GetAggregateId()}] {message}");
    }
}