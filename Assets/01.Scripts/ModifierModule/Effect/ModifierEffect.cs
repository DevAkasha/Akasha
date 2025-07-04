using System;
using System.Collections.Generic;
using UnityEngine;

namespace Akasha.Modifier
{
    public class ModifierEffect : BaseEffect
    {
        public EffectApplyMode Mode { get; private set; }
        public float Duration { get; private set; }
        public IReadOnlyList<FieldModifier> Modifiers => modifiers;
        public bool HasSignFlip => hasSignFlip;
        public bool Stackable { get; private set; } = false;
        public bool RefreshOnDuplicate { get; private set; } = false;
        public Func<bool> RemoveTrigger { get; private set; } = null;
        public StackBehavior StackBehavior { get; private set; } = StackBehavior.ReplaceLatest;

        private readonly List<FieldModifier> modifiers = new();
        private bool hasSignFlip = false;

        public Func<float, float> Interpolator { get; private set; }
        public bool IsInterpolated { get; private set; } = false;

        public ModifierEffect(Enum id, EffectApplyMode mode = EffectApplyMode.Manual, float duration = 0f)
            : base(id)
        {
            Mode = mode;
            Duration = duration;
        }

        public ModifierEffect Add<T>(string fieldName, ModifierType type, float value)
        {
            modifiers.Add(new FieldModifier(fieldName, type, value));
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
            if (value)
            {
                StackBehavior = StackBehavior.Stack;
            }
            return this;
        }

        public ModifierEffect SetStackBehavior(StackBehavior behavior)
        {
            StackBehavior = behavior;
            if (behavior == StackBehavior.Stack)
            {
                Stackable = true;
            }
            return this;
        }

        public ModifierEffect RefreshOnRepeat(bool value = true)
        {
            RefreshOnDuplicate = value;
            return this;
        }

        public ModifierEffect SetInterpolated(float duration, Func<float, float> interpolator)
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
                        modifiable.SetModifier(Key, modifier.Type, modifier.Value, StackBehavior);
                    }
                }
            }

            if (Mode == EffectApplyMode.Timed)
            {
                if (IsInterpolated)
                {
                    GameManager.EffectRunner.RegisterInterpolatedEffect(this, target);
                }
                else
                {
                    GameManager.EffectRunner.RegisterTimedEffect(this, target);
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
                    modifiable.RemoveModifier(Key, 0);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[ModifierEffect] Failed to remove modifier: {e.Message}");
                }
            }
        }

        public void ApplyStack(IModelOwner target, int stackId)
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
                        modifiable.SetModifier(Key, modifier.Type, modifier.Value, StackBehavior.Stack);
                    }
                }
            }
        }

        public void RemoveStack(IModelOwner target, int stackId)
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
                    modifiable.RemoveModifier(Key, stackId);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[ModifierEffect] Failed to remove stack modifier: {e.Message}");
                }
            }
        }

        public int GetStackCount(IModelOwner target)
        {
            var modifiableTarget = target.GetBaseModel() as IModifiableTarget;
            if (modifiableTarget == null)
                return 0;

            foreach (var modifiable in modifiableTarget.GetModifiables())
            {
                if (modifiable == null)
                    continue;

                foreach (var modifier in Modifiers)
                {
                    if (modifiable is IRxField field &&
                        field.FieldName.Equals(modifier.FieldName, StringComparison.OrdinalIgnoreCase))
                    {
                        return modifiable.GetStackCount(Key);
                    }
                }
            }

            return 0;
        }
    }

    public readonly struct FieldModifier
    {
        public readonly string FieldName;
        public readonly ModifierType Type;
        public readonly float Value;

        public FieldModifier(string fieldName, ModifierType type, float value)
        {
            FieldName = fieldName;
            Type = type;
            Value = value;
        }
    }
}