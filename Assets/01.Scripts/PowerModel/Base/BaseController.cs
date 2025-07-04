using System.Collections.Generic;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine;

public interface IController
{
    void OnControllerInitialize();
    void OnControllerDeinitialize();
}

public abstract class BaseController : AggregateRoot, IController, IRxOwner, IRxCaller
{
    [Header("Controller Settings")]
    [SerializeField] protected bool enableDebugLogs = false;
    protected virtual bool EnablePooling => false;

    public override AggregateType GetAggregateType() => AggregateType.Controller;

    public bool IsRxVarOwner => true;
    public bool IsRxAllOwner => false;
    public bool IsLogicalCaller => true;
    public bool IsMultiRolesCaller => true;
    public bool IsFunctionalCaller => false;

    private readonly HashSet<RxBase> trackedRxVars = new();
    private bool isLifecycleInitialized = false;

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

    protected override void OnAwake()
    {
        base.OnAwake();
        AtAwake();
        LogDebug($"Controller {GetType().Name} awakened");
    }

    protected override void OnStart()
    {
        base.OnStart();
        AtStart();
        CallInit();
        LogDebug($"Controller {GetType().Name} started");
    }

    protected virtual void OnEnable()
    {
        AtEnable();
        if (!isLifecycleInitialized)
        {
            CallInit();
        }
        LogDebug($"Controller {GetType().Name} enabled");
    }

    protected virtual void OnDisable()
    {
        AtDisable();
        if (EnablePooling && isLifecycleInitialized)
        {
            CallDeinit();
        }
        LogDebug($"Controller {GetType().Name} disabled");
    }

    protected override void OnDestroyed()
    {
        if (isLifecycleInitialized)
        {
            CallDeinit();
        }
        AtDestroy();
        Unload();
        LogDebug($"Controller {GetType().Name} destroyed");
        base.OnDestroyed();
    }

    private void CallInit()
    {
        if (isLifecycleInitialized) return;

        AtInit();
        isLifecycleInitialized = true;
        LogDebug($"Controller {GetType().Name} lifecycle initialized");
    }

    private void CallDeinit()
    {
        if (!isLifecycleInitialized) return;

        AtDeinit();
        isLifecycleInitialized = false;
        LogDebug($"Controller {GetType().Name} lifecycle deinitialized");
    }

    protected override void OnInitialize()
    {
        base.OnInitialize();
        OnControllerInitialize();
    }

    protected override void OnDeinitialize()
    {
        OnControllerDeinitialize();
        Unload();
        base.OnDeinitialize();
    }

    public virtual void OnControllerInitialize()
    {
        LogDebug($"Controller {GetType().Name} controller initialized");
    }

    public virtual void OnControllerDeinitialize()
    {
        LogDebug($"Controller {GetType().Name} controller deinitialized");
    }

    protected virtual void AtAwake() { }
    protected virtual void AtStart() { }
    protected virtual void AtInit() { }
    protected virtual void AtEnable() { }
    protected virtual void AtDisable() { }
    protected virtual void AtDeinit() { }
    protected virtual void AtDestroy() { }

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
public abstract class MController : ModelAggregate, IController, IModelOwner
{
    public override AggregateType GetAggregateType() => AggregateType.MController;

    public abstract BaseModel GetBaseModel();

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

    public virtual void OnControllerInitialize()
    {
        LogDebug($"Model Controller {GetType().Name} initialized");
    }

    public virtual void OnControllerDeinitialize()
    {
        LogDebug($"Model Controller {GetType().Name} deinitialized");
    }

    protected void LogDebug(string message)
    {
        Debug.Log($"[{GetAggregateId()}] {message}");
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

public abstract class MController<M> : MController, IModelOwner<M> where M : BaseModel
{
    public M Model { get; set; }

    public override BaseModel GetBaseModel() => Model;
    public M GetModel() => Model;

    protected override void SetupModel()
    {
        CreateModel();
        AtSetModel();
    }

    protected override void CleanupModel()
    {
        Model?.Unload();
        Model = null;
    }

    protected abstract void CreateModel();
    protected virtual void AtSetModel() { }
}

public abstract class EMController<E, M> : MController, IRxCaller
    where E : BaseEntity<M> where M : BaseModel
{
    public override AggregateType GetAggregateType() => AggregateType.EMController;

    [SerializeField] private E entity;

    public E Entity => entity;
    public M Model => entity?.Model;

    bool IRxCaller.IsLogicalCaller => true;
    bool IRxCaller.IsMultiRolesCaller => false;
    bool IRxCaller.IsFunctionalCaller => false;

    public override BaseModel GetBaseModel() => Model;
    public M GetModel() => Model;

    protected override void OnAwake()
    {
        if (entity == null)
            entity = GetComponentInChildren<E>();
        base.OnAwake();
    }

    protected override void SetupModel()
    {
        entity?.CallAwake();
        AtSetModel();
    }

    protected override void OnModelInitialized()
    {
        entity?.CallStart();
        entity?.CallInit();
        base.OnModelInitialized();
    }

    protected override void OnModelDeinitializing()
    {
        entity?.CallDeinit();
        entity?.CallDestroy();
        base.OnModelDeinitializing();
    }

    protected virtual void AtSetModel() { }
}

