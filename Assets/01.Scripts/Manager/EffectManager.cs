using System;
using System.Collections.Generic;
using UnityEngine;

public class EffectManager : Singleton<EffectManager>
{
    private readonly Dictionary<ModifierKey, BaseEffect> effects = new();

    public void Register(BaseEffect effect)
    {
        if (effect == null)
        {
            Debug.LogError("[EffectManager] Cannot register null effect");
            return;
        }

        effects[effect.Key] = effect;
    }

    public bool TryGetEffect(ModifierKey key, out BaseEffect effect)
    {
        return effects.TryGetValue(key, out effect);
    }


    public BaseEffect GetEffect(ModifierKey key)
    {
        if (!effects.TryGetValue(key, out var effect))
            throw new KeyNotFoundException($"Effect not found: {key}");
        return effect;
    }

    public T GetEffect<T>(ModifierKey key) where T : BaseEffect
    {
        BaseEffect effect = GetEffect(key);

        if (effect is T typedEffect)
            return typedEffect;
        else
            throw new InvalidCastException($"Effect {key} is not of type {typeof(T).Name}");
    }

    public ModifierEffect GetModifierEffect(ModifierKey key)
    {
        return GetEffect<ModifierEffect>(key);
    }
    public DirectEffect GetDirectEffect(ModifierKey key)
    {
        return GetEffect<DirectEffect>(key);
    }
    protected override void Awake()
    {
        base.Awake();

        Register(EffectBuilder.DefineModifier(EffectId.Exhaust, EffectApplyMode.Timed)
                .Add("MoveSpeed", ModifierType.Multiplier, 0.7f)
                .Duration(2f)
                .Build());

        Register(EffectBuilder.DefineModifier(EffectId.Ghost, EffectApplyMode.Timed)
                .Add("MoveSpeed", ModifierType.Multiplier, 1.8f)
                .Duration(10f)
                .Build());

        Register(EffectBuilder.DefineModifier(EffectId.Slow, EffectApplyMode.Timed)
                .Add("MoveSpeed", ModifierType.Multiplier,0.2f)
                .Duration(2f)
                .Build());
    }
}

public enum EffectId
{
    Ghost,
    Exhaust,
    Slow
}