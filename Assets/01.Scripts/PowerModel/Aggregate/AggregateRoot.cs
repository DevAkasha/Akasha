using System;
using UnityEngine;

namespace Akasha
{
    public enum AggregateType
    {
        Controller,
        MController,
        EMController,
        Presenter,
        Unknown
    }

    [System.Serializable]
    public struct TransformData
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 localScale;

        public static TransformData FromTransform(Transform t)
        {
            return new TransformData
            {
                position = t.position,
                rotation = t.rotation,
                localScale = t.localScale
            };
        }

        public void ApplyTo(Transform t)
        {
            t.position = position;
            t.rotation = rotation;
            t.localScale = localScale;
        }
    }

    [System.Serializable]
    public class ObjectMetadata
    {
        public AggregateType aggregateType = AggregateType.Unknown;
        public string className = "";
        public TransformData transformData;
        public string prefabPath = "";
        public bool hasParent = false;
        public string parentPath = "";

        public void CaptureFromObject(AggregateRoot aggregate)
        {
            aggregateType = aggregate.GetAggregateType();
            className = aggregate.GetType().Name;
            transformData = TransformData.FromTransform(aggregate.CachedTransform);

            var parent = aggregate.CachedTransform.parent;
            if (parent != null)
            {
                hasParent = true;
                parentPath = GetTransformPath(parent);
            }
        }

        private string GetTransformPath(Transform t)
        {
            return t.parent == null ? t.name : GetTransformPath(t.parent) + "/" + t.name;
        }
    }

    public interface IPoolable
    {
        void OnSpawnFromPool();
        void OnReturnToPool();
        void ResetPoolableState();
    }

    public abstract class AggregateRoot : MonoBehaviour, IPoolable
    {
        public ObjectMetadata objectMetadata = new ObjectMetadata();
        [SerializeField] private int instanceId = -1;
        [SerializeField] private bool isInPool = false;
        [SerializeField] private bool isSceneCreated = true;
        [SerializeField] protected bool isPoolObject = false;

        private Transform cachedTransform;
        private bool isInitialized = false;
        private TransformData originalTransformData;
        private bool isSpawningFromPool = false;
        private bool isReturningToPool = false;

        public int InstanceId => instanceId;
        public Transform CachedTransform => cachedTransform ?? (cachedTransform = transform);
        public bool IsInitialized => isInitialized;
        public bool IsInPool => isInPool;
        public bool IsPoolObject => isPoolObject;
        public bool IsSceneCreated => isSceneCreated;
        public bool IsSpawningFromPool => isSpawningFromPool;
        public bool IsReturningToPool => isReturningToPool;

        public abstract AggregateType GetAggregateType();

        protected virtual void Awake()
        {
            InitializeIdentity();
            DeterminePoolStatus();
            RegisterToManager();
        }

        protected virtual void OnDestroy()
        {
            if (!isInPool)
            {
                UnregisterFromManager();
            }
        }

        private void InitializeIdentity()
        {
            if (instanceId == -1)
            {
                instanceId = GetInstanceID();
            }

            cachedTransform = transform;
            objectMetadata.CaptureFromObject(this);
            originalTransformData = TransformData.FromTransform(cachedTransform);
            isInitialized = true;
        }

        private void DeterminePoolStatus()
        {
            var containerManager = GetComponentInParent<ContainerManager<AggregateRoot>>();
            if (containerManager != null)
            {
                isPoolObject = true;
                isSceneCreated = false;
                DontDestroyOnLoad(gameObject);
            }
        }

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

        private IAggregateManager GetResponsibleManager()
        {
            return GetAggregateType() switch
            {
                AggregateType.Controller or AggregateType.MController or AggregateType.EMController => GameManager.Controllers,
                AggregateType.Presenter => GameManager.Presenters,
                _ => null
            };
        }

        public void SetInPool(bool inPool)
        {
            if (isInPool == inPool) return;

            isInPool = inPool;
            OnPoolStateChanged(inPool);
        }

        protected virtual void OnPoolStateChanged(bool inPool)
        {
            if (inPool)
                OnReturnToPool();
            else
                OnSpawnFromPool();
        }

        public virtual void OnSpawnFromPool()
        {
            isSpawningFromPool = true;
            gameObject.SetActive(true);
            OnExitPool();
            isSpawningFromPool = false;
        }

        public virtual void OnReturnToPool()
        {
            isReturningToPool = true;
            ResetPoolableState();
            gameObject.SetActive(false);
            OnEnterPool();
            isReturningToPool = false;
        }

        public virtual void ResetPoolableState()
        {
            originalTransformData.ApplyTo(cachedTransform);
        }

        protected virtual void OnEnterPool() { }
        protected virtual void OnExitPool() { }

        public void ForceReturnToPool()
        {
            var manager = GetResponsibleManager();
            if (manager is ContainerManager<AggregateRoot> containerManager)
            {
                containerManager.ReturnToPool(this);
            }
        }

        public string GetAggregateId()
        {
            return $"{GetAggregateType()}_{instanceId}";
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
}