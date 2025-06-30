using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class Presenter : AggregateRoot
{
    protected override void OnInitialize()
    {
        base.OnInitialize();
        OnPresenterInitialize();
    }

    protected override void OnDeinitialize()
    {
        OnPresenterDeinitialize();
        base.OnDeinitialize();
    }

    protected virtual void OnPresenterInitialize() { }
    protected virtual void OnPresenterDeinitialize() { }
}

public abstract class BasePresenter : Presenter, IRxOwner, IRxCaller
{
    [Header("Presenter Settings")]
    [SerializeField] protected bool enableDebugLogs = false;

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

    protected void BindModelToView(BaseView view, BaseModel model)
    {
        if (view == null || model == null) return;

        var bindMethod = view.GetType().GetMethod("BindToModel", new[] { model.GetType() });
        if (bindMethod == null)
        {
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

    protected override void OnPresenterInitialize()
    {
        base.OnPresenterInitialize();
        LogDebug($"Presenter {GetType().Name} initialized");
    }

    protected override void OnPresenterDeinitialize()
    {
        foreach (var view in ownedViews)
        {
            if (view != null)
                view.Cleanup();
        }
        ownedViews.Clear();

        Unload();
        LogDebug($"Presenter {GetType().Name} deinitialized");
        base.OnPresenterDeinitialize();
    }

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