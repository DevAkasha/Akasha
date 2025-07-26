using System.Collections.Generic;
using UnityEngine;

namespace Akasha
{
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
        protected bool isLifecycleInitialized = false;

        public bool IsLifecycleInitialized => isLifecycleInitialized;

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
            CallEnable();
        }

        protected virtual void OnDisable()
        {
            CallDisable();
        }

        protected override void OnDestroyed()
        {
            CallDestroy();
            base.OnDestroyed();
        }

        protected virtual void CallEnable()
        {
            AtEnable();
            if (!isLifecycleInitialized && IsInitialized)
            {
                CallInit();
            }
        }

        protected virtual void CallDisable()
        {
            if (EnablePooling && isLifecycleInitialized)
            {
                CallDeinit();
            }
            AtDisable();
        }

        protected virtual void CallInit()
        {
            if (isLifecycleInitialized) return;

            AtInit();
            isLifecycleInitialized = true;
            LogDebug($"Controller {GetType().Name} lifecycle initialized");
        }

        protected virtual void CallDeinit()
        {
            if (!isLifecycleInitialized) return;

            AtDeinit();
            isLifecycleInitialized = false;
            LogDebug($"Controller {GetType().Name} lifecycle deinitialized");
        }

        protected virtual void CallDestroy()
        {
            if (isLifecycleInitialized)
            {
                CallDeinit();
            }
            AtDestroy();
            Unload();
            LogDebug($"Controller {GetType().Name} destroyed");
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

        protected virtual void AtEnable() { }
        protected virtual void AtAwake() { }
        protected virtual void AtStart() { }
        protected virtual void AtInit() { }
        protected virtual void AtDeinit() { }
        protected virtual void AtDisable() { }
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


    public abstract class MController : BaseController, IModelOwner
    {
        protected virtual bool EnableSaveLoad => false;

        [SerializeField] protected bool isModelInitialized = false;
        public bool isDirty = false;

        public override AggregateType GetAggregateType() => AggregateType.MController;

        public new bool IsRxAllOwner => true;

        public bool IsModelInitialized => isModelInitialized;
        public bool IsSaveLoadEnabled => EnableSaveLoad;

        public abstract BaseModel GetBaseModel();

        protected override void OnAwake()
        {
            if (!isModelInitialized)
            {
                InitializeModel();
            }
            base.OnAwake();
        }

        protected override void CallInit()
        {
            if (isLifecycleInitialized) return;

            base.CallInit();

            if (EnableSaveLoad)
            {
                CallLoad();
            }

            CallReadyModel();
        }

        protected override void CallDeinit()
        {
            if (!isLifecycleInitialized) return;

            if (EnableSaveLoad && isDirty)
            {
                CallSave();
            }

            base.CallDeinit();
        }

        protected override void CallDestroy()
        {
            base.CallDestroy();

            if (isModelInitialized)
            {
                DeinitializeModel();
            }
        }

        public virtual void CallLoad()
        {
            PerformLoad();
            AtLoad();
        }

        public virtual void CallReadyModel()
        {
            AtReadyModel();
        }

        public virtual void CallSave()
        {
            AtSave();
            PerformSave();
        }

        protected virtual void InitializeModel()
        {
            SetupModel();
            isModelInitialized = true;
            OnModelInitialized();
        }

        protected virtual void DeinitializeModel()
        {
            OnModelDeinitializing();
            CleanupModel();
            isModelInitialized = false;
        }

        protected abstract void SetupModel();
        protected virtual void CleanupModel() { }
        protected virtual void OnModelInitialized() { }
        protected virtual void OnModelDeinitializing() { }

        protected virtual void AtLoad() { }
        protected virtual void AtReadyModel() { }
        protected virtual void AtSave() { }

        public void MarkDirty()
        {
            isDirty = true;
            OnMarkDirty();
        }

        protected virtual void OnMarkDirty() { }

        public virtual void Save()
        {
            if (!EnableSaveLoad) return;

            CallSave();
            isDirty = false;

            if (GameManager.SaveLoad != null)
            {
                GameManager.SaveLoad.SaveGame();
            }
        }

        public virtual void Load()
        {
            if (!EnableSaveLoad) return;

            if (GameManager.SaveLoad != null)
            {
                GameManager.SaveLoad.LoadGame();
            }

            CallLoad();
            isDirty = false;
        }

        protected virtual void PerformSave() { }
        protected virtual void PerformLoad() { }
    }

    public abstract class MController<M> : MController, IModelOwner<M> where M : BaseModel
    {
        public M Model { get; set; }

        public override BaseModel GetBaseModel() => Model;
        public M GetModel() => Model;

        protected override void SetupModel()
        {
            CreateModel();
            SetModel();
        }

        protected override void CleanupModel()
        {
            Model?.Unload();
            Model = null;
        }

        protected abstract void CreateModel();
        protected virtual void SetModel() { }
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

        protected override void CallEnable()
        {
            entity?.CallEnable();
            base.CallEnable();
        }

        protected override void CallDisable()
        {
            entity?.CallDisable();
            base.CallDisable();
        }

        protected override void CallInit()
        {
            if (isLifecycleInitialized) return;

            entity?.CallInit();
            base.CallInit();
        }

        protected override void CallDeinit()
        {
            if (!isLifecycleInitialized) return;

            entity?.CallDeinit();
            base.CallDeinit();
        }

        protected override void SetupModel()
        {
            entity?.CallAwake();
            entity?.CallStart();
            SetModel();
        }

        public override void CallLoad()
        {
            entity?.CallLoad();
            base.CallLoad();
        }

        public override void CallReadyModel()
        {
            entity?.CallReadyModel();
            base.CallReadyModel();
        }

        public override void CallSave()
        {
            entity?.CallSave();
            base.CallSave();
        }

        protected override void CleanupModel()
        {
            entity?.CallDestroy();
            base.CleanupModel();
        }

        protected virtual void SetModel() { }
    }
}