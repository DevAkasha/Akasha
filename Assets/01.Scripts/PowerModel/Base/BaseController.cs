using System;
using System.Collections.Generic;
using UnityEngine;

namespace Akasha
{
    public abstract class BaseController : AggregateRoot
    {
        public override AggregateType GetAggregateType()
        {
            return AggregateType.Controller;
        }
    }

    public abstract class Controller : BaseController, IRxOwner, IRxCaller
    {
        private bool isInit = false;
        private bool isPoolInit = false;
        private readonly HashSet<RxBase> trackedRxVars = new();

        public bool IsRxVarOwner => true;
        public bool IsRxAllOwner => false;
        public bool IsLogicalCaller => true;
        public bool IsMultiRolesCaller => true;
        public bool IsFunctionalCaller => false;

        protected override void Awake()
        {
            base.Awake();
            AtAwake();
        }

        private void OnEnable()
        {
            if (!isInit) return;

            if (IsPoolObject && IsSpawningFromPool)
            {
                OnPoolInit();
                AtPoolInit();
            }
            else if (!IsPoolObject || !IsInPool)
            {
                AtEnable();
            }
        }

        private void OnPoolInit()
        {
            isPoolInit = true;
        }

        protected void Start()
        {
            OnInit();
            AtInit();
            AtStart();
            AtLateStart();
        }

        private void OnInit()
        {
            isInit = true;
        }

        protected void OnDisable()
        {
            if (!IsPoolObject || !IsReturningToPool)
            {
                AtDisable();
            }
            else if (isPoolInit && IsReturningToPool)
            {
                OnPoolDeinit();
                AtPoolDeinit();
            }
        }

        private void OnPoolDeinit()
        {
            isPoolInit = false;
        }

        protected override void OnDestroy()
        {
            OnDeinit();
            AtDeinit();
            AtDestroy();
            base.OnDestroy();
        }

        private void OnDeinit()
        {
            isInit = false;
            Unload();
        }

        public override void OnSpawnFromPool()
        {
            base.OnSpawnFromPool();
            if (isInit)
            {
                OnPoolInit();
                AtPoolInit();
            }
        }

        public override void OnReturnToPool()
        {
            if (isPoolInit)
            {
                OnPoolDeinit();
                AtPoolDeinit();
            }
            base.OnReturnToPool();
        }

        public override void ResetPoolableState()
        {
            base.ResetPoolableState();
            isPoolInit = false;
        }

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

        protected virtual void AtAwake() { }
        protected virtual void AtInit() { }
        protected virtual void AtDeinit() { }
        protected virtual void AtStart() { }
        protected virtual void AtLateStart() { }
        protected virtual void AtEnable() { }
        protected virtual void AtDisable() { }
        protected virtual void AtPoolInit() { }
        protected virtual void AtPoolDeinit() { }
        protected virtual void AtDestroy() { }
    }

    public abstract class MController : BaseController, IModelOwner
    {
        public abstract BaseModel GetBaseModel();

        public override AggregateType GetAggregateType()
        {
            return AggregateType.MController;
        }
    }

    public abstract class MController<M> : MController, IModelOwner<M>, IRxCaller where M : BaseModel
    {
        public M Model { get; set; }

        public override BaseModel GetBaseModel() => Model;
        public M GetModel() => Model;

        private bool isInit = false;
        private bool isPoolInit = false;

        public bool IsLogicalCaller => true;
        public bool IsMultiRolesCaller => false;
        public bool IsFunctionalCaller => true;

        protected override void Awake()
        {
            base.Awake();
            AtAwake();
            Model = SetModel();
            AtModelReady();
        }

        private void OnEnable()
        {
            if (!isInit) return;

            if (IsPoolObject && IsSpawningFromPool)
            {
                OnPoolInit();
                AtPoolInit();
            }
            else if (!IsPoolObject || !IsInPool)
            {
                AtEnable();
            }
        }

        protected void Start()
        {
            OnInit();
            AtInit();
            AtStart();
            AtLateStart();
        }

        private void OnPoolInit()
        {
            isPoolInit = true;
        }

        private void OnInit()
        {
            isInit = true;
        }

        protected void OnDisable()
        {
            if (!IsPoolObject)
            {
                AtDisable();
            }
            else if (isPoolInit && IsInPool)
            {
                OnPoolDeinit();
                AtPoolDeinit();
            }
        }

        private void OnPoolDeinit()
        {
            isPoolInit = false;
        }

        protected override void OnDestroy()
        {
            OnDeinit();
            AtDeinit();
            AtDestroy();
            base.OnDestroy();
        }

        private void OnDeinit()
        {
            isInit = false;
        }

        public override void OnSpawnFromPool()
        {
            base.OnSpawnFromPool();
            if (isInit)
            {
                OnPoolInit();
                AtPoolInit();
            }
        }

        public override void OnReturnToPool()
        {
            if (isPoolInit)
            {
                OnPoolDeinit();
                AtPoolDeinit();
            }
            AtSave();
            base.OnReturnToPool();
        }

        public override void ResetPoolableState()
        {
            base.ResetPoolableState();
            isPoolInit = false;
            Model?.Unload();
            Model = SetModel();
        }

        public void CallSave()
        {
            AtSave();
        }

        public void CallLoad()
        {
            AtLoad();
        }

        protected virtual void AtAwake() { }
        protected virtual void AtInit() { }
        protected virtual void AtDeinit() { }
        protected virtual void AtStart() { }
        protected virtual void AtLateStart() { }
        protected virtual void AtEnable() { }
        protected virtual void AtDisable() { }
        protected virtual void AtDestroy() { }
        protected virtual void AtPoolInit() { }
        protected virtual void AtPoolDeinit() { }
        protected abstract M SetModel();
        protected virtual void AtModelReady() { }
        protected virtual void AtSave() { }
        protected virtual void AtLoad() { }
    }

    public abstract class EMController : BaseController, IModelOwner
    {
        public abstract BaseModel GetBaseModel();
        public abstract void RegistEntity(BaseEntity baseEntity);
        public abstract void CallModelReady();

        public override AggregateType GetAggregateType()
        {
            return AggregateType.EMController;
        }
    }

    public abstract class EMController<E, M> : EMController, IRxCaller
        where E : BaseEntity<M> where M : BaseModel
    {
        public E Entity { get; set; }
        public M Model { get; set; }

        private bool isInit = false;
        private bool isPoolInit = false;

        bool IRxCaller.IsLogicalCaller => true;
        bool IRxCaller.IsMultiRolesCaller => false;
        bool IRxCaller.IsFunctionalCaller => false;

        public override BaseModel GetBaseModel() => Model;
        public M GetModel() => Model;

        public override void RegistEntity(BaseEntity entity)
        {
            try
            {
                Entity = (E)entity;
            }
            catch (InvalidCastException ex)
            {
                Debug.LogError($"EntityRegist failed: Cannot cast {entity.GetType().Name} to {typeof(E).Name}. " +
                              $"Entity: {entity}, Expected: {typeof(E)}");
            }
        }

        public override void CallModelReady()
        {
            Model = Entity.Model;
            AtModelReady();
        }

        protected override void Awake()
        {
            base.Awake();
            AtAwake();
        }

        private void OnEnable()
        {
            if (!isInit) return;

            if (IsPoolObject && IsSpawningFromPool)
            {
                Entity?.CallPoolInit();
                OnPoolInit();
                AtPoolInit();
            }
            else if (!IsPoolObject || !IsInPool)
            {
                Entity?.CallEnable();
                AtEnable();
            }
        }

        private void OnPoolInit()
        {
            isPoolInit = true;
        }

        protected void Start()
        {
            Entity.SetEnable();
            Entity.CallInit();
            OnInit();
            AtInit();
            Entity.CallStart();
            AtStart();
            Entity.CallLateStart();
            AtLateStart();
        }

        private void OnInit()
        {
            isInit = true;
        }

        protected void OnDisable()
        {
            if (!isInit) return;

            if (!IsPoolObject || !IsReturningToPool)
            {
                Entity?.CallDisable();
                AtDisable();
            }
            else if (isPoolInit && IsReturningToPool)
            {
                Entity?.CallPoolDeinit();
                OnPoolDeinit();
                AtPoolDeinit();
            }
        }

        private void OnPoolDeinit()
        {
            isPoolInit = false;
        }

        protected override void OnDestroy()
        {
            Entity?.CallDeinit();
            OnDeinit();
            AtDeinit();
            Entity?.CallDestroy();
            AtDestroy();
            base.OnDestroy();
        }

        private void OnDeinit()
        {
            isInit = false;
        }

        public override void OnSpawnFromPool()
        {
            base.OnSpawnFromPool();
            if (isInit)
            {
                Entity?.CallPoolInit();
                OnPoolInit();
                AtPoolInit();
            }
        }

        public override void OnReturnToPool()
        {
            if (isPoolInit)
            {
                Entity?.CallPoolDeinit();
                OnPoolDeinit();
                AtPoolDeinit();
            }
            AtSave();
            base.OnReturnToPool();
        }

        public override void ResetPoolableState()
        {
            base.ResetPoolableState();
            isPoolInit = false;
        }

        public void OnSave()
        {
            AtSave();
        }

        public void OnLoad()
        {
            AtLoad();
        }

        protected virtual void AtAwake() { }
        protected virtual void AtInit() { }
        protected virtual void AtStart() { }
        protected virtual void AtLateStart() { }
        protected virtual void AtEnable() { }
        protected virtual void AtDisable() { }
        protected virtual void AtDeinit() { }
        protected virtual void AtDestroy() { }
        protected virtual void AtPoolInit() { }
        protected virtual void AtPoolDeinit() { }
        protected virtual void AtModelReady() { }
        protected virtual void AtSave() { }
        protected virtual void AtLoad() { }
    }
}