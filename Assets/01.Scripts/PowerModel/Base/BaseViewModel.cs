using System;
using System.Collections.Generic;
using UnityEngine;

#region 기본 ViewModel 클래스
/// <summary>
/// Model 단위의 구독 전용 래퍼
/// 개별 RxField가 아닌 Model 레벨의 변화를 감지하고 처리
/// </summary>
public abstract class BaseViewModel : IViewModel
{
    protected BaseView owner;
    protected BaseModel boundModel;
    private readonly List<Action> cleanupActions = new();

    public abstract Type ModelType { get; }

    public void SetOwner(BaseView view)
    {
        owner = view;
    }

    public virtual void BindToModel(BaseModel model)
    {
        if (model == null)
        {
            Debug.LogWarning($"[{GetType().Name}] Cannot bind to null model");
            return;
        }

        if (!ModelType.IsAssignableFrom(model.GetType()))
        {
            Debug.LogError($"[{GetType().Name}] Model type mismatch. Expected: {ModelType.Name}, Got: {model.GetType().Name}");
            return;
        }

        boundModel = model;

        // Model 레벨의 구독 설정
        SetupModelSubscription(model);

        Debug.Log($"[{GetType().Name}] Bound to model: {model.GetType().Name}");
    }

    /// <summary>
    /// Model 레벨의 구독을 설정 - 하위 클래스에서 구현
    /// </summary>
    protected abstract void SetupModelSubscription(BaseModel model);

    /// <summary>
    /// Model의 전반적인 변화가 있을 때 호출
    /// </summary>
    protected virtual void OnModelChanged()
    {
        NotifyViewUpdate();
    }

    /// <summary>
    /// 특정 도메인 이벤트 발생 시 호출
    /// </summary>
    protected virtual void OnModelEvent(string eventType, object eventData)
    {
        NotifyViewUpdate();
    }

    protected virtual void NotifyViewUpdate()
    {
        owner?.OnModelDataChanged(this);
    }

    /// <summary>
    /// 정리 액션 등록 헬퍼
    /// </summary>
    protected void RegisterCleanupAction(Action cleanupAction)
    {
        if (cleanupAction != null)
        {
            cleanupActions.Add(cleanupAction);
        }
    }

    public virtual void Cleanup()
    {
        foreach (var cleanup in cleanupActions)
        {
            try
            {
                cleanup?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] Error during cleanup: {ex.Message}");
            }
        }

        cleanupActions.Clear();
        boundModel = null;

        Debug.Log($"[{GetType().Name}] ViewModel cleaned up");
    }

    /// <summary>
    /// 바인딩된 모델 반환
    /// </summary>
    public BaseModel GetBoundModel() => boundModel;
}
#endregion

#region 제네릭 ViewModel
/// <summary>
/// 특정 Model 타입을 위한 제네릭 ViewModel
/// </summary>
public abstract class BaseViewModel<T> : BaseViewModel where T : BaseModel
{
    protected T model;

    public override Type ModelType => typeof(T);
    public T Model => model;

    public override void BindToModel(BaseModel model)
    {
        this.model = model as T;
        if (this.model == null)
        {
            Debug.LogError($"[{GetType().Name}] Model is not of expected type {typeof(T).Name}");
            return;
        }

        base.BindToModel(model);
        OnModelBound(this.model);
    }

    protected override void SetupModelSubscription(BaseModel model)
    {
        if (model is T typedModel)
        {
            SetupModelSubscription(typedModel);
        }
    }

    /// <summary>
    /// 타입 안전한 Model 구독 설정
    /// </summary>
    protected abstract void SetupModelSubscription(T model);

    /// <summary>
    /// 타입 안전한 Model 바인딩 완료 콜백
    /// </summary>
    protected abstract void OnModelBound(T model);
}
#endregion