using UnityEngine;

namespace Akasha
{
    public abstract class BasePart : MonoBehaviour
    {
        private bool isLifecycleInitialized = false;

        public void CallAwake()
        {
            AtAwake();
        }

        public void CallStart()
        {
            AtStart();
        }

        public void CallEnable()
        {
            AtEnable();
        }

        public void CallDisable()
        {
            AtDisable();
        }

        public void CallInit()
        {
            if (isLifecycleInitialized) return;

            AtInit();
            isLifecycleInitialized = true;
        }

        public void CallLoad()
        {
            AtLoad();
        }

        public void CallReadyModel()
        {
            AtReadyModel();
        }

        public void CallSave()
        {
            AtSave();
        }

        public void CallDeinit()
        {
            if (!isLifecycleInitialized) return;

            AtDeinit();
            isLifecycleInitialized = false;
        }

        public void CallDestroy()
        {
            AtDestroy();
        }

        protected virtual void AtEnable() { }
        protected virtual void AtAwake() { }
        protected virtual void AtStart() { }
        protected virtual void AtInit() { }
        protected virtual void AtLoad() { }
        protected virtual void AtReadyModel() { }
        protected virtual void AtSave() { }
        protected virtual void AtDeinit() { }
        protected virtual void AtDisable() { }
        protected virtual void AtDestroy() { }

        public abstract void RegistEntity(object entity);
        public abstract void RegistModel(object model);
    }

    public abstract class BasePart<E, M> : BasePart where E : BaseEntity<M> where M : BaseModel
    {
        protected E Entity { get; set; }
        protected M Model { get; set; }

        public override void RegistEntity(object entity) => RegisterEntity(entity as E);
        public override void RegistModel(object model) => RegisterModel(model as M);

        protected T GetSiblingPart<T>() where T : BasePart
        {
            return Entity?.GetPart<T>();
        }

        protected void CallPartMethod<T>(string methodName, params object[] parameters)
            where T : BasePart
        {
            var part = GetSiblingPart<T>();
            if (part != null)
            {
                var method = part.GetType().GetMethod(methodName);
                method?.Invoke(part, parameters);
            }
        }

        private void RegisterEntity(E entity)
        {
            Entity = entity;
        }

        private void RegisterModel(M model)
        {
            Model = model;
        }
    }
}