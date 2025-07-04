using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Akasha.Modifier
{
    public interface IModifiable
    {
        void SetModifier(ModifierKey key, ModifierType type, float value);
        void SetModifier(ModifierKey key, ModifierType type, float value, StackBehavior behavior);
        void RemoveModifier(ModifierKey key, int stackId);
        bool HasModifier(ModifierKey key);
        int GetStackCount(ModifierKey key);
    }


    public sealed class RxMod<T> : RxBase, IModifiable, IRxField<T>
    {
        private T origin;
        private T cachedValue;
        private readonly List<Action<T>> listeners;
        private readonly int instanceId;
        private readonly TypeCalculator<T> calculator;

        public string FieldName { get; set; }
        public T Value => cachedValue;

        private static ModifierManager ModifierManager => GameManager.Instance?.GetManager<ModifierManager>();

        public RxMod(T origin = default, string fieldName = null, IRxOwner owner = null)
        {
            if (owner != null && !owner.IsRxAllOwner)
                throw new InvalidOperationException($"An invalid owner({owner}) has accessed.");

            this.origin = origin;
            this.cachedValue = origin;
            this.listeners = new List<Action<T>>();
            this.calculator = TypeCalculator<T>.Instance;

            if (!string.IsNullOrEmpty(fieldName))
                FieldName = fieldName;

            if (ModifierManager != null)
            {
                instanceId = ModifierManager.RegisterInstance();
            }
            else
            {
                instanceId = GetHashCode();
                UnityEngine.Debug.LogWarning("[RxMod] ModifierManager not found - using fallback instance ID");
            }

            owner?.RegisterRx(this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddListener(Action<T> listener)
        {
            if (listener != null)
            {
                listeners.Add(listener);
                listener(Value);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveListener(Action<T> listener)
        {
            listeners.Remove(listener);
        }

        public void SetModifier(ModifierKey key, ModifierType type, float value)
        {
            ModifierManager?.SetModifier(instanceId, type, key, value);
            Recalculate();
        }

        public void SetModifier(ModifierKey key, ModifierType type, float value, StackBehavior behavior)
        {
            ModifierManager?.SetModifier(instanceId, type, key, value, behavior);
            Recalculate();
        }

        public void RemoveModifier(ModifierKey key)
        {
            if (ModifierManager?.RemoveByBaseKey(instanceId, key) == true)
                Recalculate();
        }

        public bool HasModifier(ModifierKey key)
        {
            return ModifierManager?.ContainsBaseKey(instanceId, key) ?? false;
        }

        public int GetStackCount(ModifierKey key)
        {
            return ModifierManager?.GetStackCount(instanceId, key) ?? 0;
        }

        // 기존 고급 기능들 유지 (하위 호환성)
        public void SetStackModifier(ModifierKey key, ModifierType type, float value, int stackId = -1)
        {
            ModifierManager?.SetStackModifier(instanceId, type, key, value, stackId);
            Recalculate();
        }

        public void RemoveModifier(ModifierKey key, int stackId)
        {
            if (ModifierManager?.RemoveModifier(instanceId, key, stackId) == true)
                Recalculate();
        }

        public void RemoveModifiersByPredicate(Func<ModifierData, bool> predicate)
        {
            if (ModifierManager?.RemoveModifiersByPredicate(instanceId, predicate) > 0)
                Recalculate();
        }

        public void ClearAll()
        {
            ModifierManager?.Clear(instanceId);
            Recalculate();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(T value)
        {
            if (!calculator.AreEqual(origin, value))
            {
                origin = value;
                Recalculate();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetValue(T value, IRxCaller caller)
        {
            if (!caller.IsFunctionalCaller)
                throw new InvalidOperationException($"An invalid caller({caller}) has accessed.");

            Set(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Recalculate()
        {
            T oldValue = cachedValue;

            float baseValue = calculator.ToFloat(origin);
            float calculatedValue = ModifierManager?.CalculateValue(instanceId, baseValue) ?? baseValue;
            cachedValue = calculator.FromFloat(calculatedValue);

            if (!calculator.AreEqual(oldValue, cachedValue))
            {
                NotifyAll(cachedValue);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void NotifyAll(T value)
        {
            for (int i = 0; i < listeners.Count; i++)
            {
                listeners[i](value);
            }
        }

        public override bool Satisfies(Func<object, bool> predicate)
            => predicate?.Invoke(Value) ?? false;

        public override void ClearRelation()
        {
            ModifierManager?.UnregisterInstance(instanceId);
            listeners.Clear();
        }
    }

    public abstract class TypeCalculator<T>
    {
        public static readonly TypeCalculator<T> Instance = CreateInstance();

        private static TypeCalculator<T> CreateInstance()
        {
            return typeof(T) switch
            {
                Type t when t == typeof(int) => (TypeCalculator<T>)(object)new IntCalculator(),
                Type t when t == typeof(float) => (TypeCalculator<T>)(object)new FloatCalculator(),
                Type t when t == typeof(long) => (TypeCalculator<T>)(object)new LongCalculator(),
                Type t when t == typeof(double) => (TypeCalculator<T>)(object)new DoubleCalculator(),
                _ => throw new NotSupportedException($"Type {typeof(T)} is not supported for RxMod")
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public abstract float ToFloat(T value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public abstract T FromFloat(float value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public abstract bool AreEqual(T a, T b);
    }

    public class IntCalculator : TypeCalculator<int>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override float ToFloat(int value) => value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int FromFloat(float value) => (int)Math.Round(Math.Clamp(value, int.MinValue, int.MaxValue));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool AreEqual(int a, int b) => a == b;
    }

    public class FloatCalculator : TypeCalculator<float>
    {
        private const float Epsilon = 0.0001f;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override float ToFloat(float value) => value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override float FromFloat(float value) => value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool AreEqual(float a, float b) => Math.Abs(a - b) < Epsilon;
    }

    public class LongCalculator : TypeCalculator<long>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override float ToFloat(long value) => value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override long FromFloat(float value) => (long)Math.Round(Math.Clamp(value, long.MinValue, long.MaxValue));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool AreEqual(long a, long b) => a == b;
    }

    public class DoubleCalculator : TypeCalculator<double>
    {
        private const double Epsilon = 0.0001;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override float ToFloat(double value) => (float)value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override double FromFloat(float value) => value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool AreEqual(double a, double b) => Math.Abs(a - b) < Epsilon;
    }
}