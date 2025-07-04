using System.Collections.Generic;
using UnityEngine;

namespace Akasha
{
    public abstract class BasePresenter : AggregateRoot, IRxOwner, IRxCaller
    {
        [Header("Presenter Settings")]
        [SerializeField] protected bool enableDebugLogs = false;

        public override AggregateType GetAggregateType() => AggregateType.Presenter;

        public bool IsRxVarOwner => true;
        public bool IsRxAllOwner => false;
        public bool IsLogicalCaller => true;
        public bool IsMultiRolesCaller => true;
        public bool IsFunctionalCaller => true;

        private readonly HashSet<RxBase> trackedRxVars = new();
        private readonly List<BaseView> ownedViews = new();

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

        protected override void OnInitialize()
        {
            base.OnInitialize();
            OnPresenterInitialize();
        }

        protected override void OnDeinitialize()
        {
            foreach (var view in ownedViews)
            {
                if (view != null)
                    view.Cleanup();
            }
            ownedViews.Clear();

            Unload();
            OnPresenterDeinitialize();
            base.OnDeinitialize();
        }

        protected virtual void OnPresenterInitialize() { }
        protected virtual void OnPresenterDeinitialize() { }

        protected T CreateView<T>() where T : BaseView
        {
            var viewPrefab = Resources.Load<T>(typeof(T).Name);
            if (viewPrefab == null)
            {
                LogError($"Could not find prefab for {typeof(T).Name} in Resources folder");
                return null;
            }

            var view = Instantiate(viewPrefab);
            view.SetOwner(this);
            ownedViews.Add(view);

            LogDebug($"Created view: {typeof(T).Name}");
            return view;
        }

        protected void DestroyView(BaseView view)
        {
            if (view != null && ownedViews.Contains(view))
            {
                LogDebug($"Destroying view: {view.GetType().Name}");

                view.Cleanup();
                ownedViews.Remove(view);

                if (view.gameObject != null)
                {
                    Destroy(view.gameObject);
                }
            }
        }

        public virtual void Show()
        {
            foreach (var view in ownedViews)
                view?.Show();
            OnShow();
        }

        public virtual void Hide()
        {
            foreach (var view in ownedViews)
                view?.Hide();
            OnHide();
        }

        protected virtual void OnShow()
        {
            LogDebug($"Presenter {GetType().Name} shown");
        }

        protected virtual void OnHide()
        {
            LogDebug($"Presenter {GetType().Name} hidden");
        }

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

        public int ViewCount => ownedViews.Count;
        public bool HasViews => ownedViews.Count > 0;
    }
}