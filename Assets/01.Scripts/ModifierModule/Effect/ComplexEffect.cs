using System;
using System.Collections.Generic;

public class ComplexEffect : BaseEffect
{
    private readonly List<BaseEffect> effects = new();

    public ComplexEffect(Enum id) : base(id) { }

    public ComplexEffect AddEffect(BaseEffect effect)
    {
        effects.Add(effect);
        return this;
    }

    public override void ApplyTo(IModelOwner target)
    {
        foreach (var effect in effects)
        {
            effect.ApplyTo(target);
        }
    }

    public override void RemoveFrom(IModelOwner target)
    {
        // 역순으로 제거 (마지막에 추가된 효과부터)
        for (int i = effects.Count - 1; i >= 0; i--)
        {
            effects[i].RemoveFrom(target);
        }
    }
}