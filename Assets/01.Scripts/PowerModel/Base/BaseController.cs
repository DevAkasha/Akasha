using System;
using System.Collections.Generic;
using UnityEngine;

namespace Akasha
{
    public abstract class BaseController : AggregateRoot
    {


    }

    public abstract class Controller : BaseController, IRxOwner, IRxCaller
    {
        public bool isInit = false;
        public bool isPoolObject = false;
        public bool isPoolInit = false;

        private readonly HashSet<RxBase> trackedRxVars = new();

        public bool IsRxVarOwner => true;

        public bool IsRxAllOwner => false;

        public bool IsLogicalCaller => true;

        public bool IsMultiRolesCaller => true;

        public bool IsFunctionalCaller => false;
        public override AggregateType GetAggregateType()
        {
            return AggregateType.Controller;
        }
        protected override void Awake()
        {
            base.Awake();
            AtAwake();
        }

        private void OnEnable()
        {
            if (!isInit) return;
            if (isPoolObject)
            {
                OnPoolInit();
                AtPoolInit();
            }
            else
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
            OnEnable();
            AtLateStart();
        }

        private void OnInit()
        {
            isInit = true;           
        }     

        protected void OnDisable()
        {
            if (!isPoolObject)
            {
                AtDisable();
            }
            else if(isPoolInit)
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
        }

        private void OnDeinit()
        {
            isInit = false;
            Unload();
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
    }

    public abstract class MController<M> : MController, IModelOwner<M>, IRxCaller where M : BaseModel
    {
        public M Model { get; set; }

        public override BaseModel GetBaseModel() => Model;

        public M GetModel() => Model;

        public bool isInit = false;
        public bool isPoolObject = false;
        public bool isPoolInit = false;

        public bool IsLogicalCaller => true;

        public bool IsMultiRolesCaller => false;

        public bool IsFunctionalCaller => true;
        public override AggregateType GetAggregateType()
        {
            return AggregateType.MController;
        }
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
            if (isPoolObject)
            {
                OnPoolInit();
                AtPoolInit();
            }
            else
            {
                AtEnable();
            }
        }

        protected void Start()
        {
            OnInit();
            AtInit();
            AtStart();
            OnEnable();
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
            if (!isPoolObject)
            {
                AtDisable();
            }
            else if(isPoolInit)
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
        }

        private void OnDeinit()
        {
            isInit = false;
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
    }

    public abstract class EMController<E, M> : EMController, IRxCaller
        where E : BaseEntity<M> where M : BaseModel
    {
        public E Entity { get; set; }
        public M Model { get; set; }

        public bool isInit = false;

        protected bool isPoolObject = false;

        public bool isPoolInit = false;

        bool IRxCaller.IsLogicalCaller => true;
        bool IRxCaller.IsMultiRolesCaller => false;
        bool IRxCaller.IsFunctionalCaller => false;

        public override BaseModel GetBaseModel() => Model;
        public M GetModel() => Model;
        public override AggregateType GetAggregateType()
        {
            return AggregateType.EMController;
        }
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
            if (isPoolObject)
            {
                Entity.CallPoolInit();
                OnPoolInit();
                AtPoolInit();
            }
            else
            {
                Entity.CallEnable();
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
            OnEnable();
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
            if (!isPoolObject)
            {
                Entity.CallDisable();
                AtDisable();
            }
            else if (isPoolInit)
            {
                Entity.CallPoolDeinit();
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
            Entity.CallDeinit();
            OnDeinit();
            AtDeinit();
            Entity.CallDestroy();
            AtDestroy();
        }

        private void OnDeinit()
        {
            isInit = false;   
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