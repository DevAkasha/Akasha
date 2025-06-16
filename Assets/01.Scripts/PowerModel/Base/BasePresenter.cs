using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class Presenter : AggregateRoot
{
    // AggregateRoot 상속으로 InstanceID 소유
    // UIManager에 의해 관리됨
}

/// <summary>
/// 개선된 ViewModel 지원을 위한 BasePresenter 수정
/// </summary>
public abstract class BasePresenter : Presenter, IRxOwner, IRxCaller
{
    [Header("Presenter Settings")]
    [SerializeField] protected bool enableDebugLogs = false;
    [SerializeField] protected bool manualManagerRegistration = false; // 수동 등록 모드

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

    #region Model Binding
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
    #endregion

    #region Unity Lifecycle - AggregateRoot 통합
    protected override void Awake()
    {
        base.Awake(); // AggregateRoot.Awake() 호출
        AtInit();
    }

    protected override void Start()
    {
        base.Start(); // AggregateRoot.Start() 호출 (자동 등록 포함)

        // 수동 등록 모드인 경우에만 수동 등록
        if (manualManagerRegistration && GameManager.UI != null)
        {
            GameManager.UI.RegisterWithAutoMetadata(this);
            LogDebug("Manual registration to UIManager completed");
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy(); // AggregateRoot.OnDestroy() 호출 (자동 해제 포함)

        AtDestroy();

        foreach (var view in ownedViews)
        {
            if (view != null)
                view.Cleanup();
        }
        ownedViews.Clear();

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

    #region AggregateRoot Integration
    protected void SetupPresenterTags()
    {
        AddTag(ObjectTag.UI);

        // 메뉴 타입 자동 감지
        if (GetType().Name.ToLower().Contains("menu"))
        {
            AddTag(ObjectTag.Menu);
        }

        // HUD 타입 자동 감지
        if (GetType().Name.ToLower().Contains("hud"))
        {
            AddTag(ObjectTag.HUD);
        }

        // 다이얼로그 타입 자동 감지
        if (GetType().Name.ToLower().Contains("dialog") || GetType().Name.ToLower().Contains("popup"))
        {
            AddTag(ObjectTag.Dialog);
        }
    }

    protected void UpdateViewMetadata()
    {
        SetMetadata("ViewCount", ownedViews.Count);
        SetMetadata("LastViewUpdate", DateTime.Now);

        if (ownedViews.Count > 0)
        {
            SetMetadata("ViewTypes", ownedViews.Select(v => v.GetType().Name).ToArray());
        }
    }
    #endregion

    #region Lifecycle Hooks
    protected virtual void AtInit()
    {
        SetupPresenterTags();
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
            Debug.Log($"[{AggregateId}] {message}");
        }
    }

    protected void LogWarning(string message)
    {
        Debug.LogWarning($"[{AggregateId}] {message}");
    }

    protected void LogError(string message)
    {
        Debug.LogError($"[{AggregateId}] {message}");
    }
    #endregion

    #region Properties
    public int ViewCount => ownedViews.Count;
    public bool HasViews => ownedViews.Count > 0;
    #endregion
}