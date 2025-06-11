using UnityEngine;

namespace Akasha
{
    public abstract class BaseInteractor : RxContextBehaviour, IInteractor, IInteractLogicalSubscriber
    {
        protected override void OnInit()
        {
            SetupBinding();
        }

        protected virtual void SetupBinding() { }

        public abstract void RefreshView();

        protected virtual void OnEnable() => OnInteractorActivated();
        protected virtual void OnDisable() => OnInteractorDeactivated();

        protected virtual void OnInteractorActivated() { }
        protected virtual void OnInteractorDeactivated() { }
    }

    public abstract class BaseInteractor<TEntity> : BaseInteractor
        where TEntity : BaseEntity
    {
        [SerializeField, Tooltip("이 Interactor가 제어할 Entity")]
        private TEntity? entity;

        public TEntity? Entity => entity;

        public void InjectEntity(TEntity entity)
        {
            this.entity = entity;
            OnEntityInjected();
        }

        protected override void OnInit()
        {
            if (entity == null)
                entity = GetComponent<TEntity>();

            base.OnInit();
        }

        protected virtual void OnEntityInjected() { }
    }
}