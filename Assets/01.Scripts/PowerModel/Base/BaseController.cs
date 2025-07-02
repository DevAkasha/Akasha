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

    protected override void Awake()
    {
        base.Awake();
        CallAwake();
    }

    protected override void Start()
    {
        base.Start();
        CallStart();
    }

    protected virtual void OnEnable()
    {
        CallEnable();
    }

    protected virtual void OnDisable()
    {
        CallDisable();
    }

    protected override void OnDestroy()
    {
        CallDestroy();
        base.OnDestroy();
    }

    private void CallAwake()
    {
        AtAwake();
        LogDebug($"Controller {GetType().Name} awakened");
    }

    private void CallStart()
    {
        AtStart();
        CallInit();
        LogDebug($"Controller {GetType().Name} started");
    }

    private void CallEnable()
    {
        AtEnable();
        if (!isLifecycleInitialized)
        {
            CallInit();
        }
        LogDebug($"Controller {GetType().Name} enabled");
    }

    private void CallDisable()
    {
        AtDisable();
        if (EnablePooling && isLifecycleInitialized)
        {
            CallDeinit();
        }
        LogDebug($"Controller {GetType().Name} disabled");
    }

    private void CallDestroy()
    {
        if (isLifecycleInitialized)
        {
            CallDeinit();
        }
        AtDestroy();
        Unload();
        LogDebug($"Controller {GetType().Name} destroyed");
    }

    private void CallInit()
    {
        if (isLifecycleInitialized) return;

        AtInit();
        isLifecycleInitialized = true;
        LogDebug($"Controller {GetType().Name} initialized");
    }

    private void CallDeinit()
    {
        if (!isLifecycleInitialized) return;

        AtDeinit();
        isLifecycleInitialized = false;
        LogDebug($"Controller {GetType().Name} deinitialized");
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

public abstract class BaseController<M> : MController, IRxCaller, IModelOwner<M>
    where M : BaseModel
{
    [Header("Model Controller Settings")]
    [SerializeField] protected bool enableDebugLogs = false;

    public M Model { get; set; }

    public bool IsLogicalCaller => true;
    public bool IsMultiRolesCaller => true;
    public bool IsFunctionalCaller => false;

    private bool isLifecycleInitialized = false;

    public override BaseModel GetBaseModel() => Model;
    public M GetModel() => Model;

    protected override void Awake()
    {
        base.Awake();
        CallAwake();
    }

    protected override void Start()
    {
        base.Start();
        CallStart();
    }

    protected virtual void OnEnable()
    {
        CallEnable();
    }

    protected virtual void OnDisable()
    {
        CallDisable();
    }

    protected override void OnDestroy()
    {
        CallDestroy();
        base.OnDestroy();
    }

    private void CallAwake()
    {
        SetupModel();
        AtSetModel();
        AtAwake();
        LogDebug($"Model Controller {GetType().Name} awakened with model {typeof(M).Name}");
    }

    private void CallStart()
    {
        AtStart();
        CallInit();
        LogDebug($"Model Controller {GetType().Name} started");
    }

    private void CallEnable()
    {
        AtEnable();
        if (!isLifecycleInitialized)
        {
            CallInit();
        }
        LogDebug($"Model Controller {GetType().Name} enabled");
    }

    private void CallDisable()
    {
        AtDisable();
        if (EnablePooling && isLifecycleInitialized)
        {
            CallDeinit();
        }
        LogDebug($"Model Controller {GetType().Name} disabled");
    }

    private void CallDestroy()
    {
        if (isLifecycleInitialized)
        {
            CallDeinit();
        }
        AtDestroy();
        Model?.Unload();
        LogDebug($"Model Controller {GetType().Name} destroyed");
    }

    private void CallInit()
    {
        if (isLifecycleInitialized) return;

        AtInit();
        isLifecycleInitialized = true;
        LogDebug($"Model Controller {GetType().Name} initialized");
    }

    private void CallDeinit()
    {
        if (!isLifecycleInitialized) return;

        AtDeinit();
        isLifecycleInitialized = false;
        LogDebug($"Model Controller {GetType().Name} deinitialized");
    }

    protected virtual void AtSetModel() { }
    protected virtual void AtAwake() { }
    protected virtual void AtStart() { }
    protected virtual void AtInit() { }
    protected virtual void AtEnable() { }
    protected virtual void AtDisable() { }
    protected virtual void AtDeinit() { }
    protected virtual void AtDestroy() { }

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

    private bool isLifecycleInitialized = false;

    public override BaseModel GetBaseModel() => Model;
    public M GetModel() => Model;

    protected override void Awake()
    {
        if (entity == null)
            entity = GetComponentInChildren<E>();

        base.Awake();
        CallAwake();
    }

    protected override void Start()
    {
        base.Start();
        CallStart();
    }

    protected virtual void OnEnable()
    {
        CallEnable();
    }

    protected virtual void OnDisable()
    {
        CallDisable();
    }

    protected override void OnDestroy()
    {
        CallDestroy();
        base.OnDestroy();
    }

    private void CallAwake()
    {
        entity?.CallAwake();
        AtAwake();
        LogDebug($"Entity Controller {GetType().Name} awakened with entity {typeof(E).Name}");
    }

    private void CallStart()
    {
        entity?.CallStart();
        AtStart();
        CallInit();
        LogDebug($"Entity Controller {GetType().Name} started");
    }

    private void CallEnable()
    {
        entity?.CallEnable();
        AtEnable();
        if (!isLifecycleInitialized)
        {
            CallInit();
        }
        LogDebug($"Entity Controller {GetType().Name} enabled");
    }

    private void CallDisable()
    {
        entity?.CallDisable();
        AtDisable();
        if (EnablePooling && isLifecycleInitialized)
        {
            CallDeinit();
        }
        LogDebug($"Entity Controller {GetType().Name} disabled");
    }

    private void CallDestroy()
    {
        entity?.CallDestroy();
        if (isLifecycleInitialized)
        {
            CallDeinit();
        }
        AtDestroy();
        LogDebug($"Entity Controller {GetType().Name} destroyed");
    }

    private void CallInit()
    {
        if (isLifecycleInitialized) return;

        entity?.CallInit();
        AtInit();
        isLifecycleInitialized = true;
        LogDebug($"Entity Controller {GetType().Name} initialized");
    }

    private void CallDeinit()
    {
        if (!isLifecycleInitialized) return;

        entity?.CallDeinit();
        AtDeinit();
        isLifecycleInitialized = false;
        LogDebug($"Entity Controller {GetType().Name} deinitialized");
    }

    protected virtual void AtSetModel() { }
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