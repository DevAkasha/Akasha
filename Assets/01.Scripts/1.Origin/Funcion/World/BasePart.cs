using UnityEngine;

namespace Akasha
{
    public abstract class BasePart : RxContextBehaviour, IFunctionalSubscriber
    {
        public BaseEntity? Entity { get; private set; }

        internal void SetParent(BaseEntity? entity)
        {
            Entity = entity;

            if (entity != null)
                OnAttachedToEntity(entity);
            else
                OnDetachedFromEntity();
        }

        protected override void OnInit()
        {
            base.OnInit();
            OnInitialize();
        }

        protected override void OnDispose()
        {
            OnTerminate();
            base.OnDispose();
        }

        protected virtual void OnInitialize() { }

        protected virtual void OnTerminate() { }

        protected virtual void OnAttachedToEntity(BaseEntity entity) { }

        protected virtual void OnDetachedFromEntity() { }
    }
}