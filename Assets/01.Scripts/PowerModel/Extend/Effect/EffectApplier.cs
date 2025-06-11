using System.Collections.Generic;

public class EffectApplier
{
    private readonly BaseEffect effect;
    private readonly List<IModelOwner> targets = new();

    public EffectApplier(BaseEffect effect)
    {
        this.effect = effect;
    }

    public EffectApplier AddTarget(IModelOwner target)
    {
        if (target != null)
            targets.Add(target);
        return this;
    }

    public void Apply()
    {
        if (!effect.Condition())
            return;

        foreach (var target in targets)
            effect.ApplyTo(target);
    }

    public void Remove()
    {
        foreach (var target in targets)
            effect.RemoveFrom(target);
    }
}