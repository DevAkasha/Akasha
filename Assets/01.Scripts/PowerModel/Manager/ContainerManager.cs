using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ContainerManager<T> : ManagerBase, IAggregateManager where T : AggregateRoot
{
    [Header("Container Settings")]
    [SerializeField] private bool enableDebugLogs = true;

    private readonly Dictionary<int, T> registeredAggregates = new();
    private readonly Dictionary<AggregateType, int> typeCounters = new();
    private readonly Dictionary<AggregateType, List<T>> typeGroups = new();
    private readonly Dictionary<int, T> pooledObjects = new();

    public int RegisteredCount => registeredAggregates.Count;
    public int PooledCount => pooledObjects.Count;

    protected override void OnManagerAwake()
    {
        InitializeTypeCounters();
    }

    private void InitializeTypeCounters()
    {
        foreach (AggregateType type in Enum.GetValues(typeof(AggregateType)))
        {
            typeCounters[type] = 0;
            typeGroups[type] = new List<T>();
        }
    }

    public int GetNextIndex(AggregateType type)
    {
        return typeCounters[type]++;
    }

    public void RegisterAggregate(AggregateRoot aggregate)
    {
        if (!(aggregate is T typedAggregate))
        {
            LogWarning($"Cannot register aggregate of type {aggregate.GetType().Name} to {typeof(T).Name} manager");
            return;
        }

        int instanceId = aggregate.InstanceId;
        if (registeredAggregates.ContainsKey(instanceId))
        {
            LogWarning($"Aggregate already registered: {aggregate}");
            return;
        }

        registeredAggregates[instanceId] = typedAggregate;
        typeGroups[aggregate.AggregateType].Add(typedAggregate);

        OnAggregateRegistered(typedAggregate);
        Log($"Registered: {aggregate}");
    }

    public void UnregisterAggregate(AggregateRoot aggregate)
    {
        if (!(aggregate is T typedAggregate))
        {
            return;
        }

        int instanceId = aggregate.InstanceId;
        if (!registeredAggregates.TryGetValue(instanceId, out var registered))
        {
            LogWarning($"Aggregate not found for unregister: {aggregate}");
            return;
        }

        registeredAggregates.Remove(instanceId);
        typeGroups[aggregate.AggregateType].Remove(typedAggregate);
        pooledObjects.Remove(instanceId);

        OnAggregateUnregistered(typedAggregate);
        Log($"Unregistered: {aggregate}");
    }

    protected virtual void OnAggregateRegistered(T aggregate) { }
    protected virtual void OnAggregateUnregistered(T aggregate) { }

    public T GetById(int instanceId)
    {
        registeredAggregates.TryGetValue(instanceId, out var aggregate);
        return aggregate;
    }

    public IEnumerable<T> GetByType(AggregateType type)
    {
        return typeGroups.TryGetValue(type, out var list) ? list : Enumerable.Empty<T>();
    }

    public IEnumerable<T> GetAll()
    {
        return registeredAggregates.Values;
    }

    public IEnumerable<T> GetInitialized()
    {
        return registeredAggregates.Values.Where(a => a.IsInitialized);
    }

    public IEnumerable<T> GetPooled()
    {
        return pooledObjects.Values;
    }

    public T GetFromPool<TSpecific>() where TSpecific : T
    {
        var pooled = pooledObjects.Values.OfType<TSpecific>().FirstOrDefault(p => p.EnablePooling);
        if (pooled != null)
        {
            pooled.SetInPool(false);
            pooledObjects.Remove(pooled.InstanceId);
            Log($"Retrieved from pool: {pooled}");
            return pooled;
        }
        return null;
    }

    public void ReturnToPool(T aggregate)
    {
        if (!aggregate.EnablePooling)
        {
            LogWarning($"Aggregate {aggregate} does not support pooling");
            return;
        }

        if (aggregate.IsInPool)
        {
            LogWarning($"Aggregate {aggregate} is already in pool");
            return;
        }

        aggregate.SetInPool(true);
        pooledObjects[aggregate.InstanceId] = aggregate;
        Log($"Returned to pool: {aggregate}");
    }

    public void SetActive(T aggregate, bool active)
    {
        if (aggregate == null) return;

        aggregate.gameObject.SetActive(active);
        Log($"Set {aggregate} active: {active}");
    }

    public void SetActiveByType(AggregateType type, bool active)
    {
        foreach (var aggregate in GetByType(type))
        {
            SetActive(aggregate, active);
        }
        Log($"Set type {type} active: {active}");
    }

    public void InitializeAll()
    {
        foreach (var aggregate in registeredAggregates.Values.Where(a => a.EnableInitialization && !a.IsInitialized))
        {
            aggregate.PerformInitialization();
        }
        Log("Initialized all aggregates");
    }

    public void DeinitializeAll()
    {
        foreach (var aggregate in registeredAggregates.Values.Where(a => a.IsInitialized))
        {
            aggregate.PerformDeinitialization();
        }
        Log("Deinitialized all aggregates");
    }

    public void ClearPool()
    {
        foreach (var pooled in pooledObjects.Values.ToArray())
        {
            if (pooled != null)
            {
                Destroy(pooled.gameObject);
            }
        }
        pooledObjects.Clear();
        Log("Cleared object pool");
    }

    protected override void OnManagerDestroy()
    {
        base.OnManagerDestroy();
        DeinitializeAll();
        ClearPool();
        registeredAggregates.Clear();
    }

    public override void OnSceneUnloaded(Scene scene)
    {
        base.OnSceneUnloaded(scene);
        CleanupSceneAggregates(scene);
    }

    private void CleanupSceneAggregates(Scene scene)
    {
        var toRemove = registeredAggregates.Values
            .Where(a => a != null && a.gameObject.scene == scene && a.IsSceneCreated)
            .ToArray();

        foreach (var aggregate in toRemove)
        {
            UnregisterAggregate(aggregate);
        }

        if (toRemove.Length > 0)
        {
            Log($"Cleaned up {toRemove.Length} scene aggregates");
        }
    }

    public Dictionary<AggregateType, int> GetTypeStatistics()
    {
        var stats = new Dictionary<AggregateType, int>();
        foreach (var type in typeGroups.Keys)
        {
            stats[type] = typeGroups[type].Count;
        }
        return stats;
    }

    private void Log(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[{GetType().Name}] {message}");
        }
    }

    private void LogWarning(string message)
    {
        if (enableDebugLogs)
        {
            Debug.LogWarning($"[{GetType().Name}] {message}");
        }
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        if (Application.isPlaying)
        {
            var stats = GetTypeStatistics();
            var statsText = stats.Count > 0
                ? string.Join("\n", stats.Select(kvp => $"  {kvp.Key}: {kvp.Value}"))
                : "  No aggregates";

            debugInfo = $"Registered: {RegisteredCount}\n" +
                       $"Pooled: {PooledCount}\n" +
                       $"Type Distribution:\n{statsText}";
        }
    }
#endif
}