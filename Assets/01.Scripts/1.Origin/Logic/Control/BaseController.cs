using UnityEngine;

namespace Akasha
{
    /// <summary>
    /// Entity와 Interactor를 함께 제어하는 게임 로직 중심 컨트롤러입니다.
    /// 상향식 인터랙션을 받아 해석하고, Entity에 명령을 전달합니다.
    /// </summary>
    public abstract class BaseController : RxContextBehaviour { }
    public abstract class BaseController<TEntity> : BaseController
        where TEntity : BaseEntity
    {
        [SerializeField] private TEntity? entity;
        [SerializeField] private BaseInteractor? interactor;

        public TEntity? Entity => entity;
        public BaseInteractor? Interactor => interactor;

        #region --- Unity Hook ---

        protected override void OnInit()
        {

            foreach (var child in GetComponentsInChildren<RxContextBehaviour>())
            {
                child.InjectControllerContext(this);
            }

            if (entity == null) entity = GetComponent<TEntity>();
            if (interactor == null) interactor = GetComponent<BaseInteractor>();

            if (entity != null) OnEntityInjected();
            if (interactor != null) OnInteractorInjected();

            SetupLogic();
        }

        protected virtual void OnEnable() => OnControllerActivated();
        protected virtual void OnDisable() => OnControllerDeactivated();

        #endregion

        #region --- Injection API ---

        public void InjectEntity(TEntity entity)
        {
            this.entity = entity;
            OnEntityInjected();
        }

        public void InjectInteractor(BaseInteractor interactor)
        {
            this.interactor = interactor;
            OnInteractorInjected();
        }

        #endregion

        #region --- Overridable Hooks ---

        protected virtual void OnEntityInjected() { }
        protected virtual void OnInteractorInjected() { }
        protected virtual void SetupLogic() { }
        protected virtual void OnControllerActivated() { }
        protected virtual void OnControllerDeactivated() { }

        #endregion
    }
}