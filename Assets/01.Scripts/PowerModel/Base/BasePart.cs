using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BasePart : WorldObject 
{
    public void CallInit() => AtInit();
    public void CallInitAfter() => AtInitAfter();
    public void CallDisable() => AtDisable(); 
    public void CallDestroy() => AtDestroy();
    public void CallDeinit() => AtDeinit();

    protected virtual void AtInit() { }
    protected virtual void AtInitAfter() { }
    protected virtual void AtDisable() { }
    protected virtual void AtDestroy() { }
    protected virtual void AtDeinit() { }
    public abstract void RegistEntity(object entity);
    public abstract void RegistModel(object model);
}

public abstract class BasePart<E, M> : BasePart where E : BaseEntity<M> where M : BaseModel
{
    protected E Entity { get; set; }
    protected M Model { get; set; }

    public override void RegistEntity(object entity) => RegisterEntity(entity as E);
    public override void RegistModel(object entity) => RegisterModel(entity as M);

    protected T GetSiblingPart<T>() where T : BasePart
    {
        return Entity.GetPart<T>();
    }

    protected void CallPartMethod<T>(string methodName, params object[] parameters)
    where T : BasePart
    {
        var part = GetSiblingPart<T>();
        if (part != null)
        {
            var method = part.GetType().GetMethod(methodName);
            method?.Invoke(part, parameters);
            // TODO.동작확인 후  파라미터 불일치에 대해 방어코드작성.
        }
    }

    private void RegisterEntity(E entity)
    {
        Entity = entity;
    }

    public void RegisterModel(M model)
    {
        Model = model;
    }
}
