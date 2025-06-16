using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// 모든 컨트롤러의 기본 클래스 (AggregateRoot 상속)
public abstract class Controller : AggregateRoot
{
    protected bool isInitialized;
    [SerializeField] protected bool isEnableLifecycle = true;

    protected virtual void AtDestroy() { }
    protected virtual void AtDisable() { }
    protected virtual void AtInit() { }
    protected virtual void AtDeinit() { }
}

public abstract class MController : Controller, IModelOwner
{
    public abstract BaseModel GetBaseModel();
}

/// <summary>
/// 개선된 BaseController - AggregateRoot 자동 등록 활용
/// </summary>
public abstract class BaseController : Controller, IRxCaller, IRxOwner
{
    [Header("Controller Settings")]
    [SerializeField] protected bool enableDebugLogs = false;
    [SerializeField] protected bool manualManagerRegistration = false;

    #region IRxOwner, IRxCaller Implementation
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
    #endregion

    #region Unity Lifecycle - AggregateRoot 통합
    protected override void Awake()
    {
        base.Awake(); // AggregateRoot.Awake() 호출
    }

    protected override void Start()
    {
        base.Start(); // AggregateRoot.Start() 호출 (자동 등록 포함)

        Initialize();

        if (manualManagerRegistration && GameManager.World != null)
        {
            GameManager.World.RegisterWithAutoMetadata(this);
            LogDebug("Manual registration to WorldManager completed");
        }
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        if (!isInitialized && isEnableLifecycle)
            Initialize();
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        if (isEnableLifecycle)
            Deinitialize();

        AtDisable();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy(); // AggregateRoot.OnDestroy() 호출 (자동 해제 포함)

        AtDestroy();
        Deinitialize();
    }
    #endregion

    #region Controller Lifecycle
    private void Initialize()
    {
        if (isInitialized) return;

        SetupControllerTags();
        AtInit();
        isInitialized = true;

        LogDebug($"Controller {GetType().Name} initialized");
    }

    private void Deinitialize()
    {
        if (!isInitialized) return;

        AtDeinit();
        Unload();
        isInitialized = false;

        LogDebug($"Controller {GetType().Name} deinitialized");
    }
    #endregion

    #region AggregateRoot Integration
    protected void SetupControllerTags()
    {
        AddTag(ObjectTag.System);

        // 컨트롤러 타입에 따른 자동 태그 설정
        var typeName = GetType().Name.ToLower();

        if (typeName.Contains("player"))
        {
            AddTag(ObjectTag.Player);
        }
        else if (typeName.Contains("enemy"))
        {
            AddTag(ObjectTag.Enemy);
        }
        else if (typeName.Contains("boss"))
        {
            AddTag(ObjectTag.Boss);
        }
        else if (typeName.Contains("npc"))
        {
            AddTag(ObjectTag.NPC);
        }
        else if (typeName.Contains("item"))
        {
            AddTag(ObjectTag.Item);
        }
    }

    protected void UpdateControllerMetadata()
    {
        SetMetadata("IsInitialized", isInitialized);
        SetMetadata("LifecycleEnabled", isEnableLifecycle);
        SetMetadata("LastUpdate", DateTime.Now);

        if (transform != null)
        {
            SetMetadata("Position", transform.position);
            SetMetadata("Rotation", transform.rotation);
            SetMetadata("Scale", transform.localScale);
        }
    }
    #endregion

    #region Lifecycle Hooks
    protected override void AtInit()
    {
        UpdateControllerMetadata();
        LogDebug($"Controller {GetType().Name} initialized");
    }

    protected override void AtDeinit()
    {
        LogDebug($"Controller {GetType().Name} deinitialized");
    }

    protected override void AtDisable()
    {
        LogDebug($"Controller {GetType().Name} disabled");
    }

    protected override void AtDestroy()
    {
        LogDebug($"Controller {GetType().Name} destroyed");
    }
    #endregion

    #region Utility Methods
    protected void LogDebug(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[{AggregateId}] {message}");
        }
    }

    protected void LogWarning(string message)
    {
        Debug.LogWarning($"[{AggregateId}] {message}");
    }

    protected void LogError(string message)
    {
        Debug.LogError($"[{AggregateId}] {message}");
    }
    #endregion

    #region Properties
    public bool IsInitialized => isInitialized;
    #endregion
}

/// <summary>
/// 개선된 BaseController<M> - 모델을 가지는 컨트롤러
/// </summary>
public abstract class BaseController<M> : MController, IRxCaller, IModelOwner<M>
    where M : BaseModel
{
    [Header("Model Controller Settings")]
    [SerializeField] protected bool enableDebugLogs = false;
    [SerializeField] protected bool manualManagerRegistration = false;

    public M Model { get; set; }

    #region IRxCaller Implementation
    public bool IsLogicalCaller => true;
    public bool IsMultiRolesCaller => true;
    public bool IsFunctionalCaller => false;
    #endregion

    #region IModelOwner Implementation
    public override BaseModel GetBaseModel() => Model;
    public M GetModel() => Model;
    #endregion

    #region Unity Lifecycle
    protected override void Awake()
    {
        base.Awake();
    }

    protected override void Start()
    {
        base.Start();

        Initialize();

        if (manualManagerRegistration && GameManager.World != null)
        {
            GameManager.World.RegisterWithAutoMetadata(this);
            LogDebug("Manual registration to WorldManager completed");
        }
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        if (!isInitialized && isEnableLifecycle)
            Initialize();
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        if (isEnableLifecycle)
        {
            Deinitialize();
        }
        AtDisable();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        Deinitialize();
        AtDestroy();
    }
    #endregion

    #region Controller Lifecycle
    private void Initialize()
    {
        if (isInitialized) return;

        SetupModel();
        SetupModelControllerTags();
        AtInit();
        isInitialized = true;

        LogDebug($"Model Controller {GetType().Name} initialized with model {typeof(M).Name}");
    }

    private void Deinitialize()
    {
        if (!isInitialized) return;

        AtDeinit();
        Model?.Unload();
        isInitialized = false;

        LogDebug($"Model Controller {GetType().Name} deinitialized");
    }

    protected abstract void SetupModel();
    #endregion

    #region AggregateRoot Integration
    protected void SetupModelControllerTags()
    {
        AddTag(ObjectTag.System);

        var modelTypeName = typeof(M).Name.ToLower();

        if (modelTypeName.Contains("player"))
        {
            AddTag(ObjectTag.Player);
        }
        else if (modelTypeName.Contains("enemy"))
        {
            AddTag(ObjectTag.Enemy);
        }
        else if (modelTypeName.Contains("item"))
        {
            AddTag(ObjectTag.Item);
        }
        else if (modelTypeName.Contains("weapon"))
        {
            AddTag(ObjectTag.Weapon);
        }

        SetMetadata("ModelType", typeof(M).Name);
        SetMetadata("HasModel", Model != null);
    }

    protected void UpdateModelMetadata()
    {
        SetMetadata("ModelType", typeof(M).Name);
        SetMetadata("HasModel", Model != null);
        SetMetadata("LastModelUpdate", DateTime.Now);

        if (Model != null)
        {
            SetMetadata("ModelFieldCount", Model.GetAllRxFields().Count());
            SetMetadata("ModelModifiableCount", Model.GetModifiables().Count());
        }
    }
    #endregion

    #region Lifecycle Hooks
    protected override void AtInit()
    {
        UpdateModelMetadata();
        LogDebug($"Model Controller {GetType().Name} initialized");
    }

    protected override void AtDeinit()
    {
        LogDebug($"Model Controller {GetType().Name} deinitialized");
    }

    protected override void AtDisable()
    {
        LogDebug($"Model Controller {GetType().Name} disabled");
    }

    protected override void AtDestroy()
    {
        LogDebug($"Model Controller {GetType().Name} destroyed");
    }
    #endregion

    #region Utility Methods
    protected void LogDebug(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[{AggregateId}] {message}");
        }
    }

    protected void LogWarning(string message)
    {
        Debug.LogWarning($"[{AggregateId}] {message}");
    }

    protected void LogError(string message)
    {
        Debug.LogError($"[{AggregateId}] {message}");
    }
    #endregion

    #region Properties
    public bool IsInitialized => isInitialized;
    #endregion
}

/// <summary>
/// 개선된 BaseController<E, M> - 엔티티와 모델을 가지는 컨트롤러
/// </summary>
public abstract class BaseController<E, M> : MController, IRxCaller
    where E : BaseEntity<M> where M : BaseModel
{
    [Header("Entity Controller Settings")]
    [SerializeField] protected bool enableDebugLogs = false;
    [SerializeField] protected bool manualManagerRegistration = false;
    [SerializeField] private E entity;

    public E Entity => entity;
    public M Model => entity.Model;

    #region IRxCaller Implementation
    bool IRxCaller.IsLogicalCaller => true;
    bool IRxCaller.IsMultiRolesCaller => false;
    bool IRxCaller.IsFunctionalCaller => false;
    #endregion

    #region IModelOwner Implementation
    public override BaseModel GetBaseModel() => Model;
    public M GetModel() => Model;
    #endregion

    #region Unity Lifecycle
    protected override void Awake()
    {
        base.Awake();
    }

    protected override void Start()
    {
        base.Start();

        Initialize();

        if (manualManagerRegistration && GameManager.World != null)
        {
            GameManager.World.RegisterWithAutoMetadata(this);
            LogDebug("Manual registration to WorldManager completed");
        }
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        if (!isInitialized && isEnableLifecycle)
        {
            Initialize();
        }
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        if (isEnableLifecycle)
        {
            Deinitialize();
        }
        entity.CallDisable();
        AtDisable();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        Deinitialize();
        entity.CallDestroy();
        AtDestroy();
    }
    #endregion

    #region Controller Lifecycle
    private void Initialize()
    {
        if (isInitialized) return;

        if (entity == null)
            entity = GetComponentInChildren<E>();

        entity.CallInit();
        SetupEntityControllerTags();
        AtInit();
        isInitialized = true;

        LogDebug($"Entity Controller {GetType().Name} initialized with entity {typeof(E).Name}");
    }

    private void Deinitialize()
    {
        if (!isInitialized) return;

        entity.CallDeinit();
        AtDeinit();
        Model?.Unload();
        isInitialized = false;

        LogDebug($"Entity Controller {GetType().Name} deinitialized");
    }
    #endregion

    #region AggregateRoot Integration
    protected void SetupEntityControllerTags()
    {
        AddTag(ObjectTag.System);

        var entityTypeName = typeof(E).Name.ToLower();

        if (entityTypeName.Contains("player"))
        {
            AddTag(ObjectTag.Player);
        }
        else if (entityTypeName.Contains("enemy"))
        {
            AddTag(ObjectTag.Enemy);
        }
        else if (entityTypeName.Contains("boss"))
        {
            AddTag(ObjectTag.Boss);
        }
        else if (entityTypeName.Contains("npc"))
        {
            AddTag(ObjectTag.NPC);
        }
        else if (entityTypeName.Contains("item"))
        {
            AddTag(ObjectTag.Item);
        }

        SetMetadata("EntityType", typeof(E).Name);
        SetMetadata("ModelType", typeof(M).Name);
        SetMetadata("HasEntity", entity != null);
        SetMetadata("HasModel", Model != null);
    }

    protected void UpdateEntityMetadata()
    {
        SetMetadata("EntityType", typeof(E).Name);
        SetMetadata("ModelType", typeof(M).Name);
        SetMetadata("HasEntity", entity != null);
        SetMetadata("HasModel", Model != null);
        SetMetadata("LastEntityUpdate", DateTime.Now);

        if (entity != null && Model != null)
        {
            SetMetadata("EntityPartsCount", entity.GetParts<BasePart>().Count());
            SetMetadata("ModelFieldCount", Model.GetAllRxFields().Count());
        }
    }
    #endregion

    #region Lifecycle Hooks
    protected override void AtInit()
    {
        UpdateEntityMetadata();
        LogDebug($"Entity Controller {GetType().Name} initialized");
    }

    protected override void AtDeinit()
    {
        LogDebug($"Entity Controller {GetType().Name} deinitialized");
    }

    protected override void AtDisable()
    {
        LogDebug($"Entity Controller {GetType().Name} disabled");
    }

    protected override void AtDestroy()
    {
        LogDebug($"Entity Controller {GetType().Name} destroyed");
    }
    #endregion

    #region Utility Methods
    protected void LogDebug(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[{AggregateId}] {message}");
        }
    }

    protected void LogWarning(string message)
    {
        Debug.LogWarning($"[{AggregateId}] {message}");
    }

    protected void LogError(string message)
    {
        Debug.LogError($"[{AggregateId}] {message}");
    }
    #endregion

    #region Properties
    public bool IsInitialized => isInitialized;
    #endregion
}