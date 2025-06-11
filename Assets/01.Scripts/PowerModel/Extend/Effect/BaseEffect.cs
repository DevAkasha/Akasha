using System;
using System.Collections.Generic;



public abstract class BaseEffect
{
    public ModifierKey Key { get; }

    protected readonly List<Func<bool>> conditions = new();

    protected BaseEffect(Enum id)
    {
        Key = id;
    }

    public BaseEffect When(Func<bool> condition)
    {
        if (condition != null)
            conditions.Add(condition);
        return this;
    }

    public Func<bool> Condition => () => conditions.TrueForAll(cond => cond());

    public abstract void ApplyTo(IModelOwner target);

    public abstract void RemoveFrom(IModelOwner target);
}