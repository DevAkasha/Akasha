using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Akasha
{
    [System.Serializable]
    public class PoolConfig<T> where T : AggregateRoot
    {
        public T prefab;
        public int preloadCount = 5;
        public int maxPoolSize = 20;
        public bool autoReturn = true;

        [Header("Debug")]
        public string prefabName;

        private void OnValidate()
        {
            if (prefab != null)
                prefabName = prefab.name;

            if (preloadCount < 0)
                preloadCount = 0;
            if (maxPoolSize < preloadCount)
                maxPoolSize = preloadCount;
            if (maxPoolSize < 1)
                maxPoolSize = 1;
        }
    }

    public abstract class ContainerManager<T> : ManagerBase, IAggregateManager where T : AggregateRoot
    {
        [Header("Container Settings")]
        [SerializeField] private bool enableDebugLogs = true;
        [SerializeField] private bool autoReturnToPool = true;

        [Header("Pool Configuration")]
        [SerializeField] private List<PoolConfig<T>> poolConfigs = new();

        private readonly Dictionary<int, T> registeredAggregates = new();
        private readonly Dictionary<Type, Queue<T>> poolsByType = new();
        private readonly Dictionary<int, T> activeObjects = new();
        private readonly Dictionary<Type, PoolConfig<T>> configMap = new();
        private int nextIndex = 0;

        public int RegisteredCount => registeredAggregates.Count;
        public int PooledCount => poolsByType.Values.Sum(queue => queue.Count);
        public int ActiveCount => activeObjects.Count;

        protected override void OnManagerAwake()
        {
            InitializePools();
            Debug.Log($"[{GetType().Name}] Initialized - Managing {typeof(T).Name} aggregates with pooling");
        }

        protected override void OnManagerDestroy()
        {
            ClearAllPools();
            registeredAggregates.Clear();
            activeObjects.Clear();
        }

        private void InitializePools()
        {
            foreach (var config in poolConfigs)
            {
                if (config.prefab == null) continue;

                var type = config.prefab.GetType();
                configMap[type] = config;
                poolsByType[type] = new Queue<T>();

                PreloadPool(type, config);
                Log($"Initialized pool for {type.Name} with {config.preloadCount} preloaded objects");
            }
        }

        private void PreloadPool(Type type, PoolConfig<T> config)
        {
            var pool = poolsByType[type];
            for (int i = 0; i < config.preloadCount; i++)
            {
                var instance = CreatePoolInstance(config.prefab);
                if (instance != null)
                {
                    instance.SetInPool(true);
                    instance.gameObject.SetActive(false);
                    pool.Enqueue(instance);
                }
            }
        }

        private T CreatePoolInstance(T prefab)
        {
            var instance = Instantiate(prefab, transform);
            if (instance != null)
            {
                DontDestroyOnLoad(instance.gameObject);
                instance.name = $"{prefab.name}_Pooled_{instance.GetInstanceID()}";
            }
            return instance;
        }

        public int GetNextIndex(AggregateType type)
        {
            return nextIndex++;
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
            if (!aggregate.IsInPool)
            {
                activeObjects[instanceId] = typedAggregate;
            }

            OnAggregateRegistered(typedAggregate);
            Log($"Registered: {aggregate}");
        }

        public void UnregisterAggregate(AggregateRoot aggregate)
        {
            if (!(aggregate is T typedAggregate))
                return;

            int instanceId = aggregate.InstanceId;
            if (!registeredAggregates.Remove(instanceId))
            {
                LogWarning($"Aggregate not found for unregister: {aggregate}");
                return;
            }

            activeObjects.Remove(instanceId);
            RemoveFromAllPools(typedAggregate);
            OnAggregateUnregistered(typedAggregate);
            Log($"Unregistered: {aggregate}");
        }

        protected virtual void OnAggregateRegistered(T aggregate) { }
        protected virtual void OnAggregateUnregistered(T aggregate) { }

        public TSpecific Spawn<TSpecific>() where TSpecific : T
        {
            return SpawnFromPool<TSpecific>();
        }

        public TSpecific Spawn<TSpecific>(Vector3 position, Quaternion rotation) where TSpecific : T
        {
            var instance = SpawnFromPool<TSpecific>();
            if (instance != null)
            {
                instance.transform.position = position;
                instance.transform.rotation = rotation;
            }
            return instance;
        }

        public TSpecific Spawn<TSpecific>(Transform parent) where TSpecific : T
        {
            var instance = SpawnFromPool<TSpecific>();
            if (instance != null)
            {
                instance.transform.SetParent(parent);
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.identity;
            }
            return instance;
        }

        public TSpecific SpawnFromPool<TSpecific>() where TSpecific : T
        {
            var type = typeof(TSpecific);
            if (!poolsByType.TryGetValue(type, out var pool) || pool.Count == 0)
            {
                LogWarning($"No pooled objects available for type {type.Name}");
                return null;
            }

            var pooled = (TSpecific)pool.Dequeue();
            pooled.SetInPool(false);
            activeObjects[pooled.InstanceId] = pooled;
            pooled.OnSpawnFromPool();

            Log($"Spawned from pool: {pooled} (remaining: {pool.Count})");
            return pooled;
        }

        public TSpecific SpawnFromPrefab<TSpecific>(TSpecific prefab) where TSpecific : T
        {
            if (prefab == null)
            {
                LogWarning("Cannot spawn from null prefab");
                return null;
            }

            var instance = Instantiate(prefab);
            Log($"Spawned from prefab: {instance}");
            return instance;
        }

        public TSpecific SpawnOrCreate<TSpecific>(TSpecific prefab) where TSpecific : T
        {
            var pooled = SpawnFromPool<TSpecific>();
            if (pooled != null)
            {
                return pooled;
            }

            var newInstance = SpawnFromPrefab(prefab);
            Log($"Created new instance (pool empty): {newInstance}");
            return newInstance;
        }

        public bool ReturnToPool(T aggregate)
        {
            if (aggregate == null)
            {
                LogWarning("Trying to return null instance");
                return false;
            }

            if (aggregate.IsInPool)
            {
                LogWarning($"Instance {aggregate} is already in pool");
                return false;
            }

            var type = aggregate.GetType();
            if (!poolsByType.TryGetValue(type, out var pool))
            {
                pool = new Queue<T>();
                poolsByType[type] = pool;
            }

            if (!configMap.TryGetValue(type, out var config))
            {
                LogWarning($"No config for type {type.Name}, using default max pool size");
                if (pool.Count >= 20) // 기본 최대 풀 크기
                {
                    DestroyAggregate(aggregate);
                    return false;
                }
            }
            else if (pool.Count >= config.maxPoolSize)
            {
                Log($"Pool for {type.Name} is full. Destroying {aggregate}");
                DestroyAggregate(aggregate);
                return false;
            }

            activeObjects.Remove(aggregate.InstanceId);
            aggregate.transform.SetParent(transform);
            aggregate.SetInPool(true);
            aggregate.OnReturnToPool();
            pool.Enqueue(aggregate);

            Log($"Returned to pool: {aggregate} (pool size: {pool.Count})");
            return true;
        }

        public void ReturnAllToPool()
        {
            var activeList = activeObjects.Values.ToArray();
            int returnedCount = 0;

            foreach (var aggregate in activeList)
            {
                if (aggregate != null && aggregate.IsPoolObject && ReturnToPool(aggregate))
                {
                    returnedCount++;
                }
            }

            Log($"Returned {returnedCount} objects to pool");
        }

        public void ReturnAllOfType<TSpecific>() where TSpecific : T
        {
            var type = typeof(TSpecific);
            var toReturn = new List<T>();

            foreach (var instance in activeObjects.Values)
            {
                if (instance != null && instance.GetType() == type)
                {
                    toReturn.Add(instance);
                }
            }

            int returnedCount = 0;
            foreach (var instance in toReturn)
            {
                if (ReturnToPool(instance))
                {
                    returnedCount++;
                }
            }

            Log($"Returned {returnedCount} objects of type {type.Name} to pool");
        }

        public void PrewarmPool<TSpecific>(TSpecific prefab, int additionalCount) where TSpecific : T
        {
            if (prefab == null)
            {
                LogWarning("Cannot prewarm pool with null prefab");
                return;
            }

            var type = typeof(TSpecific);
            if (!poolsByType.TryGetValue(type, out var pool))
            {
                pool = new Queue<T>();
                poolsByType[type] = pool;
            }

            for (int i = 0; i < additionalCount; i++)
            {
                var instance = CreatePoolInstance(prefab);
                if (instance != null)
                {
                    instance.SetInPool(true);
                    instance.gameObject.SetActive(false);
                    pool.Enqueue(instance);
                }
            }

            Log($"Prewarmed pool {type.Name} with {additionalCount} additional instances");
        }

        public void ClearPool<TSpecific>() where TSpecific : T
        {
            var type = typeof(TSpecific);
            if (!poolsByType.TryGetValue(type, out var pool))
            {
                LogWarning($"No pool for type {type.Name}");
                return;
            }

            int count = pool.Count;
            while (pool.Count > 0)
            {
                var instance = pool.Dequeue();
                if (instance != null && instance.gameObject != null)
                {
                    DestroyAggregate(instance);
                }
            }

            Log($"Cleared pool for {type.Name} ({count} objects destroyed)");
        }

        public void ClearAllPools()
        {
            int totalDestroyed = 0;

            foreach (var kvp in poolsByType)
            {
                var pool = kvp.Value;
                while (pool.Count > 0)
                {
                    var instance = pool.Dequeue();
                    if (instance != null && instance.gameObject != null)
                    {
                        DestroyAggregate(instance);
                        totalDestroyed++;
                    }
                }
            }

            poolsByType.Clear();
            Log($"Cleared all pools ({totalDestroyed} objects destroyed)");
        }

        private void RemoveFromAllPools(T aggregate)
        {
            foreach (var pool in poolsByType.Values)
            {
                var tempList = new List<T>();
                while (pool.Count > 0)
                {
                    var pooled = pool.Dequeue();
                    if (!ReferenceEquals(pooled, aggregate))
                    {
                        tempList.Add(pooled);
                    }
                }
                foreach (var item in tempList)
                {
                    pool.Enqueue(item);
                }
            }
        }

        private void DestroyAggregate(T aggregate)
        {
            if (aggregate != null && aggregate.gameObject != null)
            {
                UnregisterAggregate(aggregate);
                Destroy(aggregate.gameObject);
            }
        }

        public T GetById(int instanceId)
        {
            registeredAggregates.TryGetValue(instanceId, out var aggregate);
            return aggregate;
        }

        public IEnumerable<T> GetAll()
        {
            return registeredAggregates.Values;
        }

        public IEnumerable<T> GetActive()
        {
            return activeObjects.Values;
        }

        public IEnumerable<T> GetPooled()
        {
            return poolsByType.Values.SelectMany(pool => pool);
        }

        public bool HasPool<TSpecific>() where TSpecific : T
        {
            return poolsByType.ContainsKey(typeof(TSpecific));
        }

        public int GetPoolCount<TSpecific>() where TSpecific : T
        {
            var type = typeof(TSpecific);
            return poolsByType.TryGetValue(type, out var pool) ? pool.Count : 0;
        }

        public int GetActiveCount<TSpecific>() where TSpecific : T
        {
            var type = typeof(TSpecific);
            return activeObjects.Values.Count(instance => instance != null && instance.GetType() == type);
        }

        public void SetActive(T aggregate, bool active)
        {
            if (aggregate == null) return;

            if (active && aggregate.IsInPool)
            {
                LogWarning($"Cannot activate pooled object {aggregate}. Spawn from pool first.");
                return;
            }

            aggregate.gameObject.SetActive(active);
            Log($"Set {aggregate} active: {active}");
        }

        public void SetActiveAll(bool active)
        {
            foreach (var aggregate in activeObjects.Values)
            {
                SetActive(aggregate, active);
            }
            Log($"Set all active aggregates active: {active}");
        }

        public override void OnSceneUnloaded(Scene scene)
        {
            base.OnSceneUnloaded(scene);
            CleanupSceneAggregates(scene);
        }

        private void CleanupSceneAggregates(Scene scene)
        {
            var toRemove = registeredAggregates.Values
                .Where(a => a != null && a.gameObject.scene == scene && a.IsSceneCreated && !a.IsInPool)
                .ToArray();

            foreach (var aggregate in toRemove)
            {
                if (autoReturnToPool && aggregate.IsPoolObject)
                {
                    ReturnToPool(aggregate);
                }
                else
                {
                    UnregisterAggregate(aggregate);
                }
            }

            if (toRemove.Length > 0)
            {
                Log($"Cleaned up {toRemove.Length} scene aggregates");
            }
        }

        public Dictionary<Type, int> GetAllPoolCounts()
        {
            return poolsByType.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Count);
        }

        public Dictionary<Type, int> GetAllActiveCounts()
        {
            var result = new Dictionary<Type, int>();

            foreach (var instance in activeObjects.Values)
            {
                if (instance != null)
                {
                    var type = instance.GetType();
                    result[type] = result.GetValueOrDefault(type) + 1;
                }
            }

            return result;
        }

        public Dictionary<Type, int> GetPoolStatistics()
        {
            return GetAllPoolCounts();
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

            foreach (var config in poolConfigs)
            {
                if (config.preloadCount < 0)
                    config.preloadCount = 0;
                if (config.maxPoolSize < config.preloadCount)
                    config.maxPoolSize = config.preloadCount;
                if (config.maxPoolSize < 1)
                    config.maxPoolSize = 1;
            }

            if (Application.isPlaying)
            {
                UpdateDebugInfo();
            }
        }

        private void UpdateDebugInfo()
        {
            var poolCounts = GetAllPoolCounts();
            var activeCounts = GetAllActiveCounts();

            var info = $"Registered: {RegisteredCount}\n";
            info += $"Active: {ActiveCount}\n";
            info += $"Pooled: {PooledCount}\n\n";
            info += "Pool Details:\n";

            foreach (var config in poolConfigs)
            {
                if (config.prefab != null)
                {
                    var type = config.prefab.GetType();
                    var pooled = poolCounts.GetValueOrDefault(type, 0);
                    var active = activeCounts.GetValueOrDefault(type, 0);

                    info += $"{type.Name}: P={pooled}, A={active}, Max={config.maxPoolSize}\n";
                }
            }

            debugInfo = info;
        }
#endif
    }
}