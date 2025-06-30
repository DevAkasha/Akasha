using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class BaseEntity : AggregateRoot, IModelOwner, IRxCaller
{
    public bool IsLogicalCaller => true;
    public bool IsMultiRolesCaller => true;
    public bool IsFunctionalCaller => true;

    public abstract BaseModel GetBaseModel();

    protected override void OnInitialize()
    {
        base.OnInitialize();
        OnEntityInitialize();
    }

    protected override void OnDeinitialize()
    {
        OnEntityDeinitialize();
        base.OnDeinitialize();
    }

    protected virtual void OnEntityInitialize() { }
    protected virtual void OnEntityDeinitialize() { }
}

public abstract class BaseEntity<M> : BaseEntity, IModelOwner<M> where M : BaseModel
{
    private readonly Dictionary<Type, BasePart> partsByType = new();
    private readonly List<BasePart> allParts = new();

    public M Model { get; set; }

    public override BaseModel GetBaseModel() => Model;
    public M GetModel() => Model;

    public void CallInit()
    {
        SetupModel();

        if (EnableInitialization)
        {
            PerformInitialization();
        }

        var parts = GetComponentsInChildren<BasePart>();
        foreach (BasePart part in parts)
        {
            allParts.Add(part);
            partsByType[part.GetType()] = part;

            part.RegistEntity(this);
            part.RegistModel(Model);
            part.CallInit();
        }

        foreach (var part in allParts)
        {
            part.CallInitAfter();
        }
    }

    public T GetPart<T>() where T : BasePart
    {
        partsByType.TryGetValue(typeof(T), out var part);
        return part as T;
    }

    public IEnumerable<T> GetParts<T>() where T : BasePart
    {
        return allParts.OfType<T>();
    }

    public void NotifyAllParts(string methodName, params object[] parameters)
    {
        foreach (var part in allParts)
        {
            var method = part.GetType().GetMethod(methodName);
            method?.Invoke(part, parameters);
        }
    }

    public void CallDisable()
    {
        foreach (var part in allParts)
        {
            part.CallDisable();
        }
        OnEntityDisable();
    }

    public void CallDestroy()
    {
        foreach (var part in allParts)
        {
            part.CallDestroy();
        }
        OnEntityDestroy();
    }

    public void CallDeinit()
    {
        foreach (var part in allParts)
        {
            part.CallDeinit();
        }

        if (EnableInitialization && IsInitialized)
        {
            PerformDeinitialization();
        }
    }

    protected override void OnEntityInitialize()
    {
        base.OnEntityInitialize();
    }

    protected override void OnEntityDeinitialize()
    {
        Model?.Unload();
        base.OnEntityDeinitialize();
    }

    protected virtual void OnEntityDisable() { }
    protected virtual void OnEntityDestroy() { }

    protected abstract void SetupModel();
}