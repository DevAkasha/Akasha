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

    public abstract class AggregateRoot : MonoBehaviour
    {
        public ObjectMetadata objectMetadata = new ObjectMetadata();
        [SerializeField] private int instanceId = -1;
        public bool isInPool = false;
        public bool isSceneCreated = true;

        private Transform cachedTransform;
        private bool isInitialized = false;
        private static int nextInstanceId = 1;

        public int InstanceId => instanceId;
        public Transform CachedTransform => cachedTransform ?? (cachedTransform = transform);
        public bool IsInitialized => isInitialized;

        public abstract AggregateType GetAggregateType();

        protected virtual void Awake()
        {
            InitializeIdentity();
            RegisterToManager();
            OnAwake();
        }

        protected virtual void Start()
        {
            if (!isInitialized)
            {
                PerformInitialization();
            }
            OnStart();
        }

        protected virtual void OnDestroy()
        {
            if (isInitialized)
            {
                PerformDeinitialization();
            }
            UnregisterFromManager();
            OnDestroyed();
        }

        private void InitializeIdentity()
        {
            if (instanceId == -1)
            {
                instanceId = nextInstanceId++;
            }

            cachedTransform = transform;
            objectMetadata.CaptureFromObject(this);
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
                AggregateType.Controller => GameManager.Controllers,
                AggregateType.MController or AggregateType.EMController => GameManager.ModelControllers,
                AggregateType.Presenter => GameManager.Presenters,
                _ => null
            };
        }

        protected virtual void OnAwake() { }
        protected virtual void OnStart() { }
        protected virtual void OnBeforeInitialize() { }
        protected virtual void OnInitialize() { }
        protected virtual void OnAfterInitialize() { }
        protected virtual void OnBeforeDeinitialize() { }
        protected virtual void OnDeinitialize() { }
        protected virtual void OnAfterDeinitialize() { }
        protected virtual void OnDestroyed() { }

        public void SetInPool(bool inPool)
        {
            isInPool = inPool;
            OnPoolStateChanged(inPool);
        }

        protected virtual void OnPoolStateChanged(bool inPool)
        {
            if (inPool)
                OnEnterPool();
            else
                OnExitPool();
        }

        protected virtual void OnEnterPool() { }
        protected virtual void OnExitPool() { }

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