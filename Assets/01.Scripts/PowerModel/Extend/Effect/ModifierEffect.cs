using System;
using System.Collections.Generic;
using UnityEngine;

public class ModifierEffect : BaseEffect
{
    public EffectApplyMode Mode { get; private set; }

    public float Duration { get; private set; }

    public IReadOnlyList<FieldModifier> Modifiers => modifiers;

    public bool HasSignFlip => hasSignFlip;

    public bool Stackable { get; private set; } = false;

    public bool RefreshOnDuplicate { get; private set; } = false;

    public Func<bool> RemoveTrigger { get; private set; } = null;

    private readonly List<FieldModifier> modifiers = new();

    private bool hasSignFlip = false;

    public Func<float, object> Interpolator { get; private set; }

    public bool IsInterpolated { get; private set; } = false;

    public ModifierEffect(Enum id, EffectApplyMode mode = EffectApplyMode.Manual, float duration = 0f)
        : base(id)
    {
        Mode = mode;
        Duration = duration;
    }

    public ModifierEffect Add<T>(string fieldName, ModifierType type, T value)
    {
        if (value is not int and not long and not float and not double)
            throw new InvalidOperationException($"Unsupported modifier type: {typeof(T)}");

        modifiers.Add(new FieldModifier(fieldName, type, value!));
        return this;
    }

    public ModifierEffect AddSignFlip()
    {
        hasSignFlip = true;
        return this;
    }


    public new ModifierEffect When(Func<bool> condition)
    {
        base.When(condition);
        return this;
    }

    public ModifierEffect Until(Func<bool> trigger)
    {
        RemoveTrigger = trigger;
        return this;
    }

    public ModifierEffect SetDuration(float seconds)
    {
        if (Mode != EffectApplyMode.Timed)
            throw new InvalidOperationException("SetDuration is only valid for Timed effects.");
        Duration = seconds;
        return this;
    }

    public ModifierEffect AllowStacking(bool value = true)
    {
        Stackable = value;
        return this;
    }

    public ModifierEffect RefreshOnRepeat(bool value = true)
    {
        RefreshOnDuplicate = value;
        return this;
    }

    public ModifierEffect SetInterpolated(float duration, Func<float, object> interpolator)
    {
        this.Duration = duration;
        this.Interpolator = interpolator;
        this.IsInterpolated = true;
        return this;
    }

    public override void ApplyTo(IModelOwner target)
    {
        var modifiableTarget = target.GetBaseModel() as IModifiableTarget;
        if (modifiableTarget == null)
        {
            Debug.LogError($"[ModifierEffect] Target {target} does not implement IModifiableTarget");
            return;
        }

        foreach (var modifiable in modifiableTarget.GetModifiables())
        {
            if (modifiable == null)
                continue;

            foreach (var modifier in Modifiers)
            {
                if (modifiable is IRxField field &&
                    field.FieldName.Equals(modifier.FieldName, StringComparison.OrdinalIgnoreCase))
                {
                    modifiable.ApplyModifier(Key, modifier.FieldName, modifier.Type, modifier.Value);
                }
            }

            if (HasSignFlip)
            {
                try
                {
                    modifiable.ApplySignFlip(Key);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[ModifierEffect] Failed to apply sign flip: {e.Message}");
                }
            }
        }

        if (Mode == EffectApplyMode.Timed)
        {
            if (IsInterpolated)
            {
                EffectRunner.Instance.RegisterInterpolatedEffect(this, target);
            }
            else
            {
                EffectRunner.Instance.RegisterTimedEffect(this, target);
            }
        }
    }

    public override void RemoveFrom(IModelOwner target)
    {
        var modifiableTarget = target.GetBaseModel() as IModifiableTarget;
        if (modifiableTarget == null)
            return;

        foreach (var modifiable in modifiableTarget.GetModifiables())
        {
            if (modifiable == null)
                continue;

            try
            {
                modifiable.RemoveModifier(Key);
            }
            catch (Exception e)
            {
                Debug.LogError($"[ModifierEffect] Failed to remove modifier: {e.Message}");
            }
        }
    }
}

public enum EffectApplyMode { Passive, Manual, Timed } // modifier 적용 방식 (수동, 자동, 시간제한)

public readonly struct FieldModifier // 필드에 적용할 modifier 정보를 담는 구조체
{
    public readonly string FieldName;
    public readonly ModifierType Type;
    public readonly object Value;

    public FieldModifier(string fieldName, ModifierType type, object value)
    {
        FieldName = fieldName;
        Type = type;
        Value = value;
    }
}
