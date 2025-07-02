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
    private bool isLifecycleInitialized = false;

    public M Model { get; set; }

    public override BaseModel GetBaseModel() => Model;
    public M GetModel() => Model;

    public void CallAwake()
    {
        AtSetModel();
        SetupModel();

        InitializeParts();

        foreach (var part in allParts)
        {
            part.CallAwake();
        }

        AtAwake();
    }

    public void CallStart()
    {
        foreach (var part in allParts)
        {
            part.CallStart();
        }

        AtStart();
    }

    public void CallEnable()
    {
        foreach (var part in allParts)
        {
            part.CallEnable();
        }

        AtEnable();
    }

    public void CallDisable()
    {
        foreach (var part in allParts)
        {
            part.CallDisable();
        }

        AtDisable();
    }

    public void CallDestroy()
    {
        foreach (var part in allParts)
        {
            part.CallDestroy();
        }
        AtDestroy();
        Model.Unload();
    }

    public void CallInit()
    {
        if (isLifecycleInitialized) return;

        foreach (var part in allParts)
        {
            part.CallInit();
        }

        AtInit();
        isLifecycleInitialized = true;
    }

    public void CallDeinit()
    {
        if (!isLifecycleInitialized) return;

        foreach (var part in allParts)
        {
            part.CallDeinit();
        }
        AtDeinit();
        Model.Unload();
        isLifecycleInitialized = false;
    }

    private void InitializeParts()
    {
        var parts = GetComponentsInChildren<BasePart>();
        foreach (BasePart part in parts)
        {
            allParts.Add(part);
            partsByType[part.GetType()] = part;

            part.RegistEntity(this);
            part.RegistModel(Model);
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

    protected virtual void AtSetModel() { }
    protected virtual void AtAwake() { }
    protected virtual void AtStart() { }
    protected virtual void AtInit() { }
    protected virtual void AtEnable() { }
    protected virtual void AtDisable() { }
    protected virtual void AtDeinit() { }
    protected virtual void AtDestroy() { }

    protected override void OnEntityInitialize()
    {
        base.OnEntityInitialize();
    }

    protected override void OnEntityDeinitialize()
    {
        Model?.Unload();
        base.OnEntityDeinitialize();
    }

    protected abstract void SetupModel();
}