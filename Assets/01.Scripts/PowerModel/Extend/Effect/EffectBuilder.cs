using System;

public static class EffectBuilder
{
    public static ModifierEffectBuilder DefineModifier(Enum effectId, EffectApplyMode mode = EffectApplyMode.Manual, float duration = 0f)
        => new ModifierEffectBuilder(effectId, mode, duration);

    public static DirectEffectBuilder DefineDirect(Enum effectId)
        => new DirectEffectBuilder(effectId);

    public static ComplexEffectBuilder DefineComplex(Enum effectId)
        => new ComplexEffectBuilder(effectId);

    public class ModifierEffectBuilder
    {
        private readonly ModifierEffect effect;

        public ModifierEffectBuilder(Enum effectId, EffectApplyMode mode = EffectApplyMode.Manual, float duration = 0f)
        {
            effect = new ModifierEffect(effectId, mode, duration);
        }

        public ModifierEffectBuilder Add<T>(string field, ModifierType type, float value)
        {
            effect.Add<T>(field, type, value);
            return this;
        }

        public ModifierEffectBuilder AddSignFlip()
        {
            effect.AddSignFlip();
            return this;
        }

        public ModifierEffectBuilder When(Func<bool> condition)
        {
            effect.When(condition);
            return this;
        }

        public ModifierEffectBuilder Until(Func<bool> trigger)
        {
            effect.Until(trigger);
            return this;
        }

        public ModifierEffectBuilder Duration(float seconds)
        {
            effect.SetDuration(seconds);
            return this;
        }

        public ModifierEffectBuilder Stackable(bool value = true)
        {
            effect.AllowStacking(value);
            return this;
        }

        public ModifierEffectBuilder SetStackBehavior(StackBehavior behavior)
        {
            effect.SetStackBehavior(behavior);
            return this;
        }

        public ModifierEffectBuilder RefreshOnDuplicate(bool value = true)
        {
            effect.RefreshOnRepeat(value);
            return this;
        }

        public ModifierEffectBuilder Interpolated(float duration, Func<float, float> interpolator)
        {
            effect.SetInterpolated(duration, interpolator);
            return this;
        }

        public ModifierEffect Build() => effect;
    }

    public class DirectEffectBuilder
    {
        private readonly DirectEffect effect;

        public DirectEffectBuilder(Enum effectId)
        {
            effect = new DirectEffect(effectId);
        }

        public DirectEffectBuilder Change<T>(string field, T value)
        {
            effect.AddChange(field, value);
            return this;
        }

        public DirectEffectBuilder AsPercentage(bool value = true)
        {
            effect.AsPercentage(value);
            return this;
        }

        public DirectEffectBuilder When(Func<bool> condition)
        {
            effect.When(condition);
            return this;
        }

        public DirectEffect Build() => effect;
    }

    public class ComplexEffectBuilder
    {
        private readonly ComplexEffect effect;

        public ComplexEffectBuilder(Enum effectId)
        {
            effect = new ComplexEffect(effectId);
        }

        public ComplexEffectBuilder AddDirectEffect(DirectEffect directEffect)
        {
            effect.AddEffect(directEffect);
            return this;
        }

        public ComplexEffectBuilder AddModifierEffect(ModifierEffect modifierEffect)
        {
            effect.AddEffect(modifierEffect);
            return this;
        }

        public ComplexEffect Build() => effect;
    }
}

public static class StackEffectHelper
{
    public static ModifierEffect CreateStackablePoison(float damagePerStack, float duration = 5f)
    {
        return EffectBuilder.DefineModifier(TestEffectId.Poison, EffectApplyMode.Timed, duration)
            .Add<int>("Health", ModifierType.OriginAdd, -damagePerStack)
            .Stackable()
            .Build();
    }

    public static ModifierEffect CreateNonStackableShield(float shieldAmount, float duration = 10f)
    {
        return EffectBuilder.DefineModifier(TestEffectId.DefenseBuff, EffectApplyMode.Timed, duration)
            .Add<int>("Defense", ModifierType.FinalAdd, shieldAmount)
            .SetStackBehavior(StackBehavior.TakeMaximum)
            .Build();
    }

    public static ModifierEffect CreateReplacingBuff(float buffAmount, float duration = 15f)
    {
        return EffectBuilder.DefineModifier(TestEffectId.StrengthBuff, EffectApplyMode.Timed, duration)
            .Add<int>("Attack", ModifierType.Multiplier, buffAmount)
            .SetStackBehavior(StackBehavior.ReplaceLatest)
            .RefreshOnDuplicate()
            .Build();
    }

    public static ModifierEffect CreatePersistentBuff(float buffAmount)
    {
        return EffectBuilder.DefineModifier(TestEffectId.LegendaryWeapon, EffectApplyMode.Manual)
            .Add<int>("Attack", ModifierType.OriginAdd, buffAmount)
            .SetStackBehavior(StackBehavior.KeepFirst)
            .Build();
    }
}