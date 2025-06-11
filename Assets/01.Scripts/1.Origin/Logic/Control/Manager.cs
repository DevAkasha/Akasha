using UnityEngine;

namespace Akasha
{
    public abstract class Manager<T> : RxContextBehaviour, IManager, IGlobalLogicalSubscriber, IRxGlobalEventSubscriber
        where T : Manager<T>
    {
        private static T? instance;

        public static T Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<T>();

                    if (instance == null)
                    {
                        GameObject singleton = new GameObject(typeof(T).Name);
                        instance = singleton.AddComponent<T>();
                    }
                }
                return instance!;
            }
        }

        public static bool IsInstance => instance != null;
        protected virtual bool IsPersistent => true;

        protected virtual void Awake()
        {
            if (instance == null)
            {
                instance = (T)this;

                if (IsPersistent)
                    DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            // RxContext 초기화 전에 수행됨
            OnSetup();
        }

        protected override void OnInit()
        {
            RegisterGlobalEvents();
            SetupGlobalBindings();
            HandleGlobalLogic();
            OnManagerInitialized();
        }

        protected virtual void OnEnable() => OnActivate();
        protected virtual void OnDisable() => OnDeactivate();

        protected override void OnDispose()
        {
            base.OnDispose();
            OnTeardown();
        }

        protected virtual void OnSetup() { }
        
        protected virtual void OnManagerInitialized() { }

        protected virtual void RegisterGlobalEvents() { }

        protected virtual void SetupGlobalBindings() { }

        protected virtual void HandleGlobalLogic() { }

        protected virtual void OnTeardown() { }

        protected abstract void OnActivate();

        protected abstract void OnDeactivate();
    }
}