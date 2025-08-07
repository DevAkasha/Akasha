using System;
using UnityEngine;

namespace Akasha
{
    public abstract class BasePart : MonoBehaviour
    {
        public abstract void RegistEntity(BaseEntity entity);
        public abstract void CallInit();
        public abstract void CallStart();
        public abstract void CallEnable();
        public abstract void CallPoolInit(); 
        public abstract void CallPoolDeinit();
        public abstract void CallLateStart();
        public abstract void CallDisable();
        public abstract void CallDeinit();
        public abstract void CallDestroy();
        public abstract void CallSave();
        public abstract void CallLoad();
    }

    public abstract class BasePart<E, M> : BasePart where E : BaseEntity<M> where M : BaseModel
    {
        protected E Entity { get; set; }
        protected M Model { get; set; }

        private void Awake()
        {
            enabled = false;
            AtAwake();
        }
        public override void CallSave()
        {
            AtSave();
        }
        public override void CallLoad()
        {
            AtLoad();
        }
        public override void CallDestroy()
        {
            AtDestroy();
        }
        public override void CallDeinit()
        {
            AtDeinit();
        }
        public override void CallDisable()
        {
            AtDisable();
        }
        public override void CallLateStart()
        {
            AtLateStart();
        }
        public override void CallStart()
        {
            AtStart();
        }
        public override void CallInit()
        {
            AtInit();
        }
        public override void CallEnable()
        {
            AtEnable();
        }

        public override void CallPoolInit()
        {
            AtPoolInit();
        }

        public override void CallPoolDeinit()
        {
            AtPoolDeinit();
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
            Model = Entity.Model;
            AtModelReady();
        }

        protected virtual void AtEnable() { }
        protected virtual void AtAwake() { }
        protected virtual void AtStart() { }
        protected virtual void AtInit() { }
        protected virtual void AtLateStart() { }

        protected virtual void AtModelReady() { }

        protected virtual void AtSave() { }
        protected virtual void AtLoad() { }

        protected virtual void AtDeinit() { }
        protected virtual void AtDisable() { }
        protected virtual void AtDestroy() { }

        protected virtual void AtPoolInit() { }
        protected virtual void AtPoolDeinit() { }
    }
}