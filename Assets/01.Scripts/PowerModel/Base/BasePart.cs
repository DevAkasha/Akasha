using UnityEngine;

public abstract class BasePart : AggregateRoot
{
    protected override void OnInitialize()
    {
        base.OnInitialize();
        OnPartInitialize();
    }

    protected override void OnDeinitialize()
    {
        OnPartDeinitialize();
        base.OnDeinitialize();
    }

    protected virtual void OnPartInitialize() { }
    protected virtual void OnPartDeinitialize() { }

    public void CallInit()
    {
        if (EnableInitialization)
        {
            PerformInitialization();
        }
    }

    public void CallInitAfter() => OnInitAfter();
    public void CallDisable() => OnDisable();
    public void CallDestroy() => OnDestroy();

    public void CallDeinit()
    {
        if (EnableInitialization && IsInitialized)
        {
            PerformDeinitialization();
        }
    }

    protected virtual void OnInitAfter() { }
    protected virtual void OnDisable() { }
    protected virtual void OnDestroy() { }

    public abstract void RegistEntity(object entity);
    public abstract void RegistModel(object model);
}

public abstract class BasePart<E, M> : BasePart where E : BaseEntity<M> where M : BaseModel
{
    protected E Entity { get; set; }
    protected M Model { get; set; }

    public override void RegistEntity(object entity) => RegisterEntity(entity as E);
    public override void RegistModel(object model) => RegisterModel(model as M);

    protected T GetSiblingPart<T>() where T : BasePart
    {
        return Entity?.GetPart<T>();
    }

    protected void CallPartMethod<T>(string methodName, params object[] parameters)
    where T : BasePart
    {
        var part = GetSiblingPart<T>();
        if (part != null)
        {
            var method = part.GetType().GetMethod(methodName);
            method?.Invoke(part, parameters);
        }
    }

    private void RegisterEntity(E entity)
    {
        Entity = entity;
    }

    private void RegisterModel(M model)
    {
        Model = model;
    }

    protected override void OnPartInitialize()
    {
        base.OnPartInitialize();
    }

    protected override void OnPartDeinitialize()
    {
        base.OnPartDeinitialize();
    }
}