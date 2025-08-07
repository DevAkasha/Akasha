using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Akasha
{
    public abstract class ContainerManager<T> : ManagerBase, IAggregateManager where T : AggregateRoot
    {
        [Header("Container Settings")]
        [SerializeField] private bool enableDebugLogs = true;

        private readonly Dictionary<int, T> registeredAggregates = new();
        private readonly Dictionary<int, T> pooledObjects = new();
        private int nextIndex = 0;

        public int RegisteredCount => registeredAggregates.Count;
        public int PooledCount => pooledObjects.Count;

        protected override void OnManagerAwake()
        {
            Debug.Log($"[{GetType().Name}] Initialized - Managing {typeof(T).Name} aggregates");
        }

        protected override void OnManagerDestroy()
        {
            ClearPool();
            registeredAggregates.Clear();
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
            var pooled = pooledObjects.Values.OfType<TSpecific>().FirstOrDefault();
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
            if (aggregate.isInPool)
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

        public void SetActiveAll(bool active)
        {
            foreach (var aggregate in registeredAggregates.Values)
            {
                SetActive(aggregate, active);
            }
            Log($"Set all aggregates active: {active}");
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

        public override void OnSceneUnloaded(Scene scene)
        {
            base.OnSceneUnloaded(scene);
            CleanupSceneAggregates(scene);
        }

        private void CleanupSceneAggregates(Scene scene)
        {
            var toRemove = registeredAggregates.Values
                .Where(a => a != null && a.gameObject.scene == scene && a.isSceneCreated)
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
                debugInfo = $"Registered: {RegisteredCount}\n" +
                           $"Pooled: {PooledCount}\n" +
                           $"Managing: {typeof(T).Name}";
            }
        }
#endif
    }
}



