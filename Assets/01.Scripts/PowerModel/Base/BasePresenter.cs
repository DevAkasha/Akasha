using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class Presenter : AggregateRoot
{
    // AggregateRoot 상속으로 InstanceID 소유
    // UIManager에 의해 관리됨
}

#region BasePresenter
/// <summary>
/// 개선된 ViewModel 지원을 위한 BasePresenter 수정
/// </summary>
public abstract class BasePresenter : Presenter, IRxOwner, IRxCaller
{
    [Header("Presenter Settings")]
    [SerializeField] protected bool enableDebugLogs = false;
    [SerializeField] protected bool autoRegisterToUIManager = true;

    #region IRxOwner, IRxCaller Implementation
    public bool IsRxVarOwner => true;
    public bool IsRxAllOwner => false;
    public bool IsLogicalCaller => true;
    public bool IsMultiRolesCaller => true;
    public bool IsFunctionalCaller => true;

    private readonly HashSet<RxBase> trackedRxVars = new();

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
    #endregion

    #region View Management
    private readonly List<BaseView> ownedViews = new();

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

    protected T CreateView<T>(GameObject prefab) where T : BaseView
    {
        if (prefab == null)
        {
            LogError("Cannot create view from null prefab");
            return null;
        }

        var viewObject = Instantiate(prefab);
        var view = viewObject.GetComponent<T>();

        if (view == null)
        {
            LogError($"Prefab does not contain component {typeof(T).Name}");
            Destroy(viewObject);
            return null;
        }

        view.SetOwner(this);
        ownedViews.Add(view);

        LogDebug($"Created view from prefab: {typeof(T).Name}");
        return view;
    }

    protected T CreateView<T>(Transform parent) where T : BaseView
    {
        var viewPrefab = Resources.Load<T>(typeof(T).Name);
        if (viewPrefab == null)
        {
            LogError($"Could not find prefab for {typeof(T).Name} in Resources folder");
            return null;
        }

        var view = Instantiate(viewPrefab, parent);
        view.SetOwner(this);
        ownedViews.Add(view);

        LogDebug($"Created view under parent: {typeof(T).Name}");
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

    protected T GetView<T>() where T : BaseView
    {
        foreach (var view in ownedViews)
        {
            if (view is T targetView)
                return targetView;
        }
        return null;
    }

    protected IReadOnlyList<BaseView> GetAllViews()
    {
        return ownedViews.AsReadOnly();
    }

    protected void ShowAllViews()
    {
        foreach (var view in ownedViews)
        {
            view?.Show();
        }
        LogDebug("All views shown");
    }

    protected void HideAllViews()
    {
        foreach (var view in ownedViews)
        {
            view?.Hide();
        }
        LogDebug("All views hidden");
    }
    #endregion

    #region Model Binding - 개선된 바인딩 지원
    /// <summary>
    /// 모든 뷰에 모델 바인딩 (ViewModel과 ViewSlot 모두 지원)
    /// </summary>
    protected void BindModelToAllViews(BaseModel model)
    {
        if (model == null)
        {
            LogWarning("Cannot bind null model to views");
            return;
        }

        foreach (var view in ownedViews)
        {
            BindModelToView(view, model);
        }
    }

    /// <summary>
    /// 특정 뷰에 모델 바인딩
    /// </summary>
    protected void BindModelToView(BaseView view, BaseModel model)
    {
        if (view == null || model == null) return;

        // 리플렉션을 통해 BindToModel 메서드 찾기
        var bindMethod = view.GetType().GetMethod("BindToModel", new[] { model.GetType() });
        if (bindMethod == null)
        {
            // BaseModel 타입으로 재시도
            bindMethod = view.GetType().GetMethod("BindToModel", new[] { typeof(BaseModel) });
        }

        if (bindMethod != null)
        {
            try
            {
                bindMethod.Invoke(view, new object[] { model });
                LogDebug($"Bound model {model.GetType().Name} to view {view.GetType().Name}");
            }
            catch (Exception ex)
            {
                LogError($"Failed to bind model to view {view.GetType().Name}: {ex.Message}");
            }
        }
        else
        {
            LogWarning($"View {view.GetType().Name} does not have BindToModel method");
        }
    }

    /// <summary>
    /// 타입별 모델 바인딩 헬퍼
    /// </summary>
    protected void BindModelToViews<TModel>(TModel model) where TModel : BaseModel
    {
        if (model == null) return;

        foreach (var view in ownedViews)
        {
            // 해당 모델 타입을 받는 BindToModel 메서드가 있는지 확인
            var bindMethod = view.GetType().GetMethod("BindToModel", new[] { typeof(TModel) });
            if (bindMethod != null)
            {
                try
                {
                    bindMethod.Invoke(view, new object[] { model });
                    LogDebug($"Bound {typeof(TModel).Name} to view {view.GetType().Name}");
                }
                catch (Exception ex)
                {
                    LogError($"Failed to bind {typeof(TModel).Name} to view {view.GetType().Name}: {ex.Message}");
                }
            }
        }
    }
    #endregion

    #region Unity Lifecycle
    protected virtual void Awake()
    {
        AtInit();

        if (autoRegisterToUIManager && GameManager.UI != null)
        {
            GameManager.UI.Register(this);
            LogDebug("Auto-registered to UIManager");
        }
    }

    protected virtual void OnDestroy()
    {
        AtDestroy();

        foreach (var view in ownedViews)
        {
            if (view != null)
                view.Cleanup();
        }
        ownedViews.Clear();

        if (GameManager.UI != null)
        {
            GameManager.UI.Unregister(this);
            LogDebug("Unregistered from UIManager");
        }

        Unload();
    }
    #endregion

    #region Presenter Control
    public virtual void Show()
    {
        ShowAllViews();
        OnShow();
    }

    public virtual void Hide()
    {
        HideAllViews();
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
    #endregion

    #region Lifecycle Hooks
    protected virtual void AtInit()
    {
        LogDebug($"Presenter {GetType().Name} initialized");
    }

    protected virtual void AtDestroy()
    {
        LogDebug($"Presenter {GetType().Name} destroyed");
    }
    #endregion

    #region Utility Methods
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
    #endregion

    #region Properties
    public int ViewCount => ownedViews.Count;
    public bool HasViews => ownedViews.Count > 0;
    #endregion
}
#endregion