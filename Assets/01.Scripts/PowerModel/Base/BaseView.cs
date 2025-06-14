using System;
using System.Collections.Generic;
using UnityEngine;
#region 수정된 BaseView
/// <summary>
/// 개선된 ViewModel을 지원하는 BaseView
/// </summary>
public abstract class BaseView : MonoBehaviour
{
    [Header("View Settings")]
    [SerializeField] protected bool enableDebugLogs = false;

    private BasePresenter owner;
    private readonly List<IViewModel> ownedViewModels = new();
    private readonly List<IViewSlot> ownedViewSlots = new();
    private bool isInitialized = false;

    #region Ownership
    public void SetOwner(BasePresenter presenter)
    {
        owner = presenter;
    }

    public BasePresenter Owner => owner;
    #endregion

    #region ViewModel Management
    protected T CreateViewModel<T>() where T : class, IViewModel, new()
    {
        var viewModel = new T();
        viewModel.SetOwner(this);
        ownedViewModels.Add(viewModel);

        LogDebug($"Created ViewModel: {typeof(T).Name}");
        return viewModel;
    }

    protected void DestroyViewModel(IViewModel viewModel)
    {
        if (viewModel != null && ownedViewModels.Contains(viewModel))
        {
            LogDebug($"Destroying ViewModel: {viewModel.GetType().Name}");

            viewModel.Cleanup();
            ownedViewModels.Remove(viewModel);
        }
    }

    protected T GetViewModel<T>() where T : class, IViewModel
    {
        foreach (var vm in ownedViewModels)
        {
            if (vm is T targetVm)
                return targetVm;
        }
        return null;
    }
    #endregion

    #region ViewSlot Management
    protected T CreateViewSlot<T>(string fieldName) where T : class, IViewSlot, new()
    {
        var viewSlot = new T();
        viewSlot.SetOwner(this);
        viewSlot.SetFieldName(fieldName);
        ownedViewSlots.Add(viewSlot);

        LogDebug($"Created ViewSlot: {typeof(T).Name} for field '{fieldName}'");
        return viewSlot;
    }

    protected void DestroyViewSlot(IViewSlot viewSlot)
    {
        if (viewSlot != null && ownedViewSlots.Contains(viewSlot))
        {
            LogDebug($"Destroying ViewSlot: {viewSlot.GetType().Name}");

            viewSlot.Cleanup();
            ownedViewSlots.Remove(viewSlot);
        }
    }

    protected T GetViewSlot<T>() where T : class, IViewSlot
    {
        foreach (var vs in ownedViewSlots)
        {
            if (vs is T targetVs)
                return targetVs;
        }
        return null;
    }
    #endregion

    #region Unity Lifecycle
    protected virtual void Start()
    {
        Initialize();
    }

    protected virtual void OnDestroy()
    {
        Cleanup();
    }
    #endregion

    #region View Lifecycle
    protected virtual void Initialize()
    {
        if (isInitialized) return;

        LogDebug($"Initializing view: {GetType().Name}");

        SetupViewModels();
        SetupViewSlots();
        AtInit();

        isInitialized = true;
        LogDebug($"View {GetType().Name} initialized");
    }

    public virtual void Cleanup()
    {
        if (!isInitialized) return;

        LogDebug($"Cleaning up view: {GetType().Name}");

        AtDestroy();

        // 모든 ViewModel 정리
        foreach (var vm in ownedViewModels)
            vm?.Cleanup();
        ownedViewModels.Clear();

        // 모든 ViewSlot 정리
        foreach (var vs in ownedViewSlots)
            vs?.Cleanup();
        ownedViewSlots.Clear();

        isInitialized = false;
        LogDebug($"View {GetType().Name} cleaned up");
    }

    protected abstract void SetupViewModels();
    protected abstract void SetupViewSlots();

    protected virtual void AtInit() { }
    protected virtual void AtDestroy() { }
    #endregion

    #region UI Control
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
    #endregion

    #region Model Data Change Notification - 개선된 이벤트 처리
    /// <summary>
    /// ViewModel에서 Model 레벨 이벤트 발생 시 호출
    /// </summary>
    public virtual void OnModelDataChanged(IViewModel viewModel)
    {
        LogDebug($"Model event from ViewModel: {viewModel.GetType().Name}");

        // 하위 클래스에서 ViewModel별 이벤트 처리
        HandleViewModelEvent(viewModel);
    }

    /// <summary>
    /// 하위 클래스에서 오버라이드하여 ViewModel별 이벤트 처리
    /// </summary>
    protected virtual void HandleViewModelEvent(IViewModel viewModel)
    {
        // 기본 구현 - 하위 클래스에서 필요시 오버라이드
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
    public bool IsInitialized => isInitialized;
    public IReadOnlyList<IViewModel> ViewModels => ownedViewModels.AsReadOnly();
    public IReadOnlyList<IViewSlot> ViewSlots => ownedViewSlots.AsReadOnly();
    #endregion
}
#endregion