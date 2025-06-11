using System;

public static class EffectBuilder
{
    public static ModifierEffectBuilder DefineModifier(Enum effectId, EffectApplyMode mode = EffectApplyMode.Manual, float duration = 0f)
        => new ModifierEffectBuilder(effectId, mode, duration);

    public static DirectEffectBuilder DefineDirect(Enum effectId)
        => new DirectEffectBuilder(effectId);

    public class ModifierEffectBuilder
    {
        private readonly ModifierEffect effect;

        public ModifierEffectBuilder(Enum effectId, EffectApplyMode mode = EffectApplyMode.Manual, float duration = 0f)
        {
            effect = new ModifierEffect(effectId, mode, duration);
        }

        public ModifierEffectBuilder Add<T>(string field, ModifierType type, T value)
        {
            effect.Add(field, type, value);
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
        public ModifierEffectBuilder RefreshOnDuplicate(bool value = true)
        {
            effect.RefreshOnRepeat(value);
            return this;
        }
        public ModifierEffectBuilder Interpolated(float duration, Func<float, object> interpolator)
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

    public static ComplexEffectBuilder DefineComplex(Enum effectId)
    => new ComplexEffectBuilder(effectId);

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