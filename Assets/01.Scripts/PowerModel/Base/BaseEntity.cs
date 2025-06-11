using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public abstract class BaseEntity : WorldObject, IModelOwner, IRxCaller
{


    public bool IsLogicalCaller => true;

    public bool IsMultiRolesCaller => true;

    public bool IsFunctionalCaller => true;

    public abstract BaseModel GetBaseModel();
}

public abstract class BaseEntity<M> : BaseEntity, IModelOwner<M> where M : BaseModel
{
    private readonly Dictionary<Type, BasePart> partsByType = new();
    private readonly List<BasePart> allParts = new();

    public void CallInit()
    {
        SetupModel();
        AtInit();

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
    public M Model { get; set; }

    public override BaseModel GetBaseModel() => Model;

    public M GetModel() => Model;

    
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
        AtDisable();
    }

    public void CallDestroy()
    {
        foreach (var part in allParts)
        {
            part.CallDestroy();
        }
        AtDestroy();
    }

    public void CallDeinit()
    {
        foreach (var part in allParts)
        {
            part.CallDeinit();
        }
        AtDeinit();
    }

    protected abstract void SetupModel();
    protected virtual void AtInit() { }
    public virtual void AtDisable() { }
    public virtual void AtDestroy() { }
    public virtual void AtDeinit() { }
}