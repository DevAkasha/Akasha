using System;
using UnityEngine;

public enum AggregateType
{
    Controller,
    Presenter,
    Entity,
    Part,
    System,
    Unknown
}

public abstract class AggregateRoot : MonoBehaviour
{
    [Header("Aggregate Identity")]
    [SerializeField] private AggregateType aggregateType = AggregateType.Unknown;
    [SerializeField] private int aggregateIndex = -1;
    [SerializeField] private int instanceId = -1;

    [Header("Logic Flags")]
    [SerializeField] private bool isSceneCreated = true;
    [SerializeField] private bool enablePooling = false;
    [SerializeField] private bool enableInitialization = true;
    [SerializeField] private bool enableSaveLoad = false;

    [Header("Meta State")]
    [SerializeField] private bool isInPool = false;
    [SerializeField] private bool isInitialized = false;

    private Transform cachedTransform;
    private static int nextInstanceId = 1;

    public AggregateType AggregateType => aggregateType;
    public int AggregateIndex => aggregateIndex;
    public int InstanceId => instanceId;
    public bool IsSceneCreated => isSceneCreated;
    public bool EnablePooling => enablePooling;
    public bool EnableInitialization => enableInitialization;
    public bool EnableSaveLoad => enableSaveLoad;
    public new Transform transform => cachedTransform ?? (cachedTransform = base.transform);
    public bool IsInPool => isInPool;
    public bool IsInitialized => isInitialized;

    protected virtual void Awake()
    {
        InitializeIdentity();

        if (enableInitialization)
        {
            PerformInitialization();
        }
    }

    private void InitializeIdentity()
    {
        if (instanceId == -1)
        {
            instanceId = nextInstanceId++;
        }

        if (aggregateType == AggregateType.Unknown)
        {
            aggregateType = DetermineAggregateType();
        }

        if (aggregateIndex == -1)
        {
            aggregateIndex = AssignAggregateIndex();
        }

        cachedTransform = base.transform;
    }

    private AggregateType DetermineAggregateType()
    {
        if (this is Controller) return AggregateType.Controller;
        if (this is Presenter) return AggregateType.Presenter;
        if (this is BaseEntity) return AggregateType.Entity;
        if (this is BasePart) return AggregateType.Part;
        return AggregateType.System;
    }

    private int AssignAggregateIndex()
    {
        var manager = GetResponsibleManager();
        return manager?.GetNextIndex(aggregateType) ?? 0;
    }

    private IAggregateManager GetResponsibleManager()
    {
        return aggregateType switch
        {
            AggregateType.Controller => GameManager.World as IAggregateManager,
            AggregateType.Presenter => GameManager.UI as IAggregateManager,
            AggregateType.Entity => GameManager.World as IAggregateManager,
            AggregateType.Part => GameManager.World as IAggregateManager,
            _ => null
        };
    }

    protected virtual void Start()
    {
        RegisterToManager();
    }

    protected virtual void OnDestroy()
    {
        if (enableInitialization && isInitialized)
        {
            PerformDeinitialization();
        }

        UnregisterFromManager();
    }

    public virtual void PerformInitialization()
    {
        if (isInitialized) return;

        OnBeforeInitialize();
        OnInitialize();
        OnAfterInitialize();

        isInitialized = true;
    }

    public virtual void PerformDeinitialization()
    {
        if (!isInitialized) return;

        OnBeforeDeinitialize();
        OnDeinitialize();
        OnAfterDeinitialize();

        isInitialized = false;
    }

    protected virtual void OnBeforeInitialize() { }
    protected virtual void OnInitialize() { }
    protected virtual void OnAfterInitialize() { }
    protected virtual void OnBeforeDeinitialize() { }
    protected virtual void OnDeinitialize() { }
    protected virtual void OnAfterDeinitialize() { }

    public void SetInPool(bool inPool)
    {
        isInPool = inPool;
        OnPoolStateChanged(inPool);
    }

    protected virtual void OnPoolStateChanged(bool inPool)
    {
        if (inPool)
        {
            OnEnterPool();
        }
        else
        {
            OnExitPool();
        }
    }

    protected virtual void OnEnterPool() { }
    protected virtual void OnExitPool() { }

    private void RegisterToManager()
    {
        var manager = GetResponsibleManager();
        manager?.RegisterAggregate(this);
    }

    private void UnregisterFromManager()
    {
        var manager = GetResponsibleManager();
        manager?.UnregisterAggregate(this);
    }

    public void ForceSetIdentity(AggregateType type, int index, int id)
    {
        aggregateType = type;
        aggregateIndex = index;
        instanceId = id;
    }

    public string GetAggregateId()
    {
        return $"{aggregateType}_{aggregateIndex}_{instanceId}";
    }

    public override string ToString()
    {
        return $"{GetType().Name}({GetAggregateId()})";
    }
}

public interface IAggregateManager
{
    int GetNextIndex(AggregateType type);
    void RegisterAggregate(AggregateRoot aggregate);
    void UnregisterAggregate(AggregateRoot aggregate);
}