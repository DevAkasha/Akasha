using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Akasha
{
    public abstract class BaseView : MonoBehaviour
    {
        [Header("View Settings")]
        [SerializeField] protected bool enableDebugLogs = false;

        private BasePresenter owner;
        private readonly List<IUIComponent> ownedComponents = new();
        private bool isInitialized = false;

        public void SetOwner(BasePresenter presenter)
        {
            owner = presenter;
        }

        public BasePresenter Owner => owner;


        protected T CreateComponent<T>() where T : class, IUIComponent, new()
        {
            var component = new T();
            component.SetOwner(this);
            ownedComponents.Add(component);

            LogDebug($"Created component: {typeof(T).Name}");
            return component;
        }
        protected T CreateViewModel<T>() where T : class, IBindable<BaseModel>, new()
        {
            var viewModel = new T();
            viewModel.SetOwner(this);
            ownedComponents.Add(viewModel);

            LogDebug($"Created ViewModel: {typeof(T).Name}");
            return viewModel;
        }

        protected T CreateViewField<T>(string fieldName) where T : class, IBindable<RxBase>, new()
        {
            var viewField = new T();
            viewField.SetOwner(this);
            ownedComponents.Add(viewField);

            LogDebug($"Created ViewSlot: {typeof(T).Name} for field '{fieldName}'");
            return viewField;
        }

        protected void DestroyComponent(IUIComponent component)
        {
            if (component != null && ownedComponents.Contains(component))
            {
                LogDebug($"Destroying component: {component.GetType().Name}");

                component.Cleanup();
                ownedComponents.Remove(component);
            }
        }
        protected T GetUIComponent<T>() where T : class, IUIComponent
        {
            foreach (var component in ownedComponents)
            {
                if (component is T targetComponent)
                    return targetComponent;
            }
            return null;
        }

        // 편의 메서드들
        protected T GetViewModel<T>() where T : class, IBindable<BaseModel>
        {
            return GetUIComponent<T>();
        }

        protected T GetViewField<T>() where T : class, IBindable<RxBase>
        {
            return GetUIComponent<T>();
        }

        protected virtual void Start()
        {
            Initialize();
        }

        protected virtual void OnDestroy()
        {
            Cleanup();
        }

        protected virtual void Initialize()
        {
            if (isInitialized) return;

            LogDebug($"Initializing view: {GetType().Name}");

            SetupComponents();
            AtInit();

            isInitialized = true;
            LogDebug($"View {GetType().Name} initialized");
        }

        public virtual void Cleanup()
        {
            if (!isInitialized) return;

            LogDebug($"Cleaning up view: {GetType().Name}");

            AtDestroy();

            foreach (var component in ownedComponents)
                component?.Cleanup();
            ownedComponents.Clear();

            isInitialized = false;
            LogDebug($"View {GetType().Name} cleaned up");
        }

        // 하위 클래스에서 구현할 메서드들
        protected abstract void SetupComponents();
        protected virtual void AtInit() { }
        protected virtual void AtDestroy() { }

        public virtual void Show()
        {
            if (!isInitialized)
            {
                Initialize();
            }

            gameObject.SetActive(true);
            OnShow();
            LogDebug($"View {GetType().Name} shown");
        }

        public virtual void Hide()
        {
            gameObject.SetActive(false);
            OnHide();
            LogDebug($"View {GetType().Name} hidden");
        }

        protected virtual void OnShow() { }
        protected virtual void OnHide() { }

        // 컴포넌트에서 발생한 이벤트 처리
        public virtual void OnComponentEvent(IUIComponent component, string eventType, object eventData)
        {
            LogDebug($"Component event from {component.GetType().Name}: {eventType}");
            HandleComponentEvent(component, eventType, eventData);
        }

        protected virtual void HandleComponentEvent(IUIComponent component, string eventType, object eventData)
        {
            // 하위 클래스에서 오버라이드하여 컴포넌트별 이벤트 처리
        }

        protected void LogDebug(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[{GetType().Name}] {message}");
            }
        }

        protected void LogWarning(string message)
        {
            Debug.LogWarning($"[{GetType().Name}] {message}");
        }

        protected void LogError(string message)
        {
            Debug.LogError($"[{GetType().Name}] {message}");
        }

        public bool IsInitialized => isInitialized;
        public IReadOnlyList<IUIComponent> Components => ownedComponents.AsReadOnly();

        // 타입별 컴포넌트 조회를 위한 편의 프로퍼티
        public IEnumerable<IBindable<BaseModel>> ViewModels =>
            ownedComponents.OfType<IBindable<BaseModel>>();

        public IEnumerable<IUIComponent> ViewSlots =>
            ownedComponents.Where(c => !(c is IBindable<BaseModel>));
    }
}